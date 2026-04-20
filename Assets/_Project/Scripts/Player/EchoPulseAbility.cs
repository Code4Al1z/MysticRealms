using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Echo Pulse: Hold E to emit continuous sound wave that charges crystals with frequency
/// </summary>
public class EchoPulseAbility : MonoBehaviour
{
    [Header("Echo Pulse Settings")]
    [Tooltip("Radius of the echo pulse effect")]
    [SerializeField] private float echoPulseRadius = 10f;

    [Tooltip("Time in seconds to grow from 0 to full radius after activating")]
    [SerializeField] private float radiusRampDuration = 1f;

    [Tooltip("Layer mask for objects that can be activated by echo pulse")]
    [SerializeField] private LayerMask echoTargetLayer;

    [Header("Visual Effects")]
    [Tooltip("Prefab for echo pulse visual effect (blue ripple)")]
    [SerializeField] private GameObject echoPulseVFXPrefab;

    [Header("Wwise Events")]
    [SerializeField] private AK.Wwise.Event echoPulseStartEvent;
    [SerializeField] private AK.Wwise.Event echoPulseStopEvent;

    [Header("Wwise RTPCs")]
    [Tooltip("RTPC for echo pulse frequency (Hz)")]
    [SerializeField] private AK.Wwise.RTPC echoPulseFrequencyRTPC;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool enableDebugLog = false;

    private bool isLocked = true;
    private bool isActive = false;
    private float currentFrequency = 100f;
    private float frequencyRampSpeed = 50f;
    private GameObject activeVFX;
    private uint playingID = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;

    private float radiusRampTimer = 0f;
    private float currentRadius = 0f;

    private void Update()
    {
        HandleInput();
        if (isActive)
        {
            UpdateRadiusRamp();
            UpdateEchoPulse();
        }
    }

    private void UpdateRadiusRamp()
    {
        if (radiusRampTimer >= radiusRampDuration) return;

        radiusRampTimer = Mathf.Min(radiusRampTimer + Time.deltaTime, radiusRampDuration);
        float t = (radiusRampDuration > 0f) ? radiusRampTimer / radiusRampDuration : 1f;
        currentRadius = Mathf.Lerp(0f, echoPulseRadius, t);
    }

    private void HandleInput()
    {
        if (isLocked) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.eKey != null)
        {
            if (kb.eKey.isPressed && !isActive)
            {
                StartEchoPulse();
            }
            else if (!kb.eKey.isPressed && isActive)
            {
                StopEchoPulse();
            }
        }
    }

    private void StartEchoPulse()
    {
        isActive = true;
        currentFrequency = 100f;
        radiusRampTimer = 0f;
        currentRadius = 0f;

        if (echoPulseStartEvent != null)
        {
            playingID = echoPulseStartEvent.Post(gameObject);
        }

        if (echoPulseVFXPrefab != null && activeVFX == null)
        {
            activeVFX = Instantiate(echoPulseVFXPrefab, transform);
        }

        if (enableDebugLog)
            Debug.Log("[EchoPulse] Started - frequency ramping up");
    }

    private void UpdateEchoPulse()
    {
        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            currentRadius,
            echoTargetLayer);

        IEchoResponsive closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (Collider col in hitColliders)
        {
            IEchoResponsive target = col.GetComponent<IEchoResponsive>();
            if (target == null) continue;

            float distance = Vector3.Distance(transform.position, col.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }

        if (closestTarget != null)
        {
            float targetFreq = closestTarget.GetRequiredFrequency();
            currentFrequency = Mathf.MoveTowards(
                currentFrequency,
                targetFreq,
                frequencyRampSpeed * Time.deltaTime);
        }

        echoPulseFrequencyRTPC?.SetValue(gameObject, currentFrequency);

        foreach (Collider col in hitColliders)
        {
            IEchoResponsive target = col.GetComponent<IEchoResponsive>();
            if (target == null) continue;

            float distance = Vector3.Distance(transform.position, col.transform.position);
            target.OnEchoPulseActive(transform.position, distance, currentFrequency);
        }
    }

    private void StopEchoPulse()
    {
        if (!isActive) return;

        isActive = false;

        if (playingID != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkUnitySoundEngine.StopPlayingID(playingID, 300);
            playingID = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
        }

        if (echoPulseStopEvent != null)
        {
            echoPulseStopEvent.Post(gameObject);
        }

        if (activeVFX != null)
        {
            Destroy(activeVFX);
            activeVFX = null;
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, currentRadius, echoTargetLayer);
        foreach (Collider col in hitColliders)
        {
            IEchoResponsive echoTarget = col.GetComponent<IEchoResponsive>();
            if (echoTarget != null)
            {
                echoTarget.OnEchoPulseStopped();
            }
        }

        if (enableDebugLog)
            Debug.Log($"[EchoPulse] Stopped at frequency {currentFrequency:F0}Hz");
    }

    public float GetFrequency()
    {
        return isActive ? currentFrequency : 0f;
    }

    public bool IsActive()
    {
        return isActive;
    }

    public bool IsLocked => isLocked;

    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = isActive ? new Color(0f, 1f, 1f, 0.5f) : new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, isActive ? currentRadius : echoPulseRadius);
    }
}