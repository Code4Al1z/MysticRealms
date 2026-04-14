using UnityEngine;
using UnityEngine.AI;

public class WispChargeHandler : MonoBehaviour
{
    [Header("Charge")]
    [SerializeField] private float chargeSpeed = 14f;
    [SerializeField] private float chargeDistance = 4f;

    [Header("Contact")]
    [Tooltip("Damage dealt each time the player enters the wisp trigger while alive.")]
    [SerializeField] private float contactDamage = 10f;
    [Tooltip("Collectable points awarded when a dead wisp is collected.")]
    [SerializeField] private int collectableValue = 1;

    [Header("Wwise")]
    [SerializeField] private AK.Wwise.Event wispChargeEvent;

    public bool IsCharging { get; private set; }
    public bool IsReturning { get; private set; }
    public bool IsActive => IsCharging || IsReturning;

    private WispEnemy wisp;
    private NavMeshAgent agent;

    private Vector2 chargeTargetXZ;
    private Vector2 returnTargetXZ;

    public void Initialise(WispEnemy wisp, NavMeshAgent agent)
    {
        this.wisp = wisp;
        this.agent = agent;
    }

    public void BeginCharge(Transform playerTransform)
    {
        if (IsActive || playerTransform == null) return;

        IsCharging = true;
        IsReturning = false;

        // Everything is XZ. Y is not stored, not used, not touched.
        Vector2 selfXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 playerXZ = new Vector2(playerTransform.position.x, playerTransform.position.z);
        Vector2 dirXZ = (playerXZ - selfXZ).normalized;

        chargeTargetXZ = playerXZ + dirXZ * chargeDistance;
        returnTargetXZ = selfXZ;

        if (agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        wispChargeEvent?.Post(gameObject);
    }

    public void Tick()
    {
        if (!IsActive) return;

        Vector2 currentXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 targetXZ = IsCharging ? chargeTargetXZ : returnTargetXZ;
        float speed = IsCharging ? chargeSpeed : chargeSpeed * 0.7f;

        // Move on XZ only. Y stays exactly as hover set it this frame.
        Vector2 nextXZ = Vector2.MoveTowards(currentXZ, targetXZ, speed * Time.deltaTime);
        transform.position = new Vector3(nextXZ.x, transform.position.y, nextXZ.y);

        float dist = Vector2.Distance(nextXZ, targetXZ);

        if (IsCharging && dist < 0.15f)
        {
            IsCharging = false;
            IsReturning = true;
        }
        else if (IsReturning && dist < 0.2f)
        {
            IsReturning = false;
            agent.enabled = true;
            agent.isStopped = false;
            // No Warp — agent resumes from current transform, updateUpAxis=false
            // means it will never write Y again.
        }
    }

    public void Cancel()
    {
        IsCharging = false;
        IsReturning = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null) return;

        if (wisp.IsDead)
        {
            ph.AddCollectable(collectableValue);
            Destroy(wisp.gameObject);
        }
        else
        {
            ph.TakeDamage(contactDamage);
        }
    }
}