using UnityEngine;
using UnityEngine.AI;

public class GolemFootstepHandler : MonoBehaviour
{
    [Header("Footsteps")]
    [SerializeField] private SurfaceAudioManager surfaceAudioManager;
    [SerializeField] private float footstepInterval = 0.75f;
    [SerializeField] private float minSpeedForFootstep = 0.3f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.4f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Wwise")]
    [SerializeField] private AK.Wwise.RTPC speedRTPC;

    private NavMeshAgent agent;
    private float baseMoveSpeed;
    private float footstepTimer = 0f;
    private Collider lastSurfaceCollider = null;
    private float lastSpeedRTPC = -1f;

    public void Initialise(NavMeshAgent agent, float baseMoveSpeed)
    {
        this.agent = agent;
        this.baseMoveSpeed = baseMoveSpeed;

        if (surfaceAudioManager == null)
            Debug.LogWarning("[GolemFootstepHandler] SurfaceAudioManager not assigned.");
    }

    public void Tick(bool isKnockedBack)
    {
        float speed = agent.enabled ? agent.velocity.magnitude : 0f;

        UpdateSpeedRTPC(speed);

        if (surfaceAudioManager == null || isKnockedBack) return;

        bool isMoving = speed > minSpeedForFootstep;

        if (isMoving)
        {
            float scaledInterval = baseMoveSpeed > 0f
                ? Mathf.Clamp(
                    footstepInterval * (baseMoveSpeed / Mathf.Max(speed, 0.01f)),
                    footstepInterval * 0.5f,
                    footstepInterval * 3f)
                : footstepInterval;

            footstepTimer += Time.deltaTime;

            if (footstepTimer >= scaledInterval)
            {
                surfaceAudioManager.OnFootstep(gameObject);
                footstepTimer = 0f;
            }

            TryUpdateSurface();
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    private void TryUpdateSurface()
    {
        if (groundCheck == null) return;

        Collider[] overlaps = Physics.OverlapSphere(groundCheck.position, groundCheckRadius, groundLayer);
        if (overlaps == null || overlaps.Length == 0) return;

        Collider hit = overlaps[0];
        if (hit == lastSurfaceCollider) return;

        lastSurfaceCollider = hit;
        surfaceAudioManager.UpdateCurrentSurface(hit);
    }

    private void UpdateSpeedRTPC(float speed)
    {
        if (speedRTPC == null || baseMoveSpeed <= 0f) return;
        float value = (speed / baseMoveSpeed) * 100f;
        if (Mathf.Approximately(value, lastSpeedRTPC)) return;
        lastSpeedRTPC = value;
        speedRTPC.SetValue(gameObject, value);
    }
}