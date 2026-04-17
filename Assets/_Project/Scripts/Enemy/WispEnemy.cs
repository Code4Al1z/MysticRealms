using UnityEngine;

public class WispEnemy : BaseEnemy
{
    [Header("Patrol")]
    [SerializeField] private Transform[] wispPatrolPoints;

    [Header("Hover")]
    [SerializeField] private float hoverAmplitude = 0.35f;
    [SerializeField] private float hoverFrequency = 0.9f;

    [Header("Death")]
    [SerializeField] private ParticleSystem deathBurstParticles;

    [Header("Components")]
    [SerializeField] private WispEchoPulseHandler echoPulseHandler;
    [SerializeField] private WispChargeHandler chargeHandler;

    public UnityEngine.AI.NavMeshAgent Agent => agent;
    public float BaseMoveSpeed => baseMoveSpeed;

    private float hoverTimer = 0f;
    private float hoverBaseY;

    protected override void Awake()
    {
        RegisterPatrolPoints(wispPatrolPoints);
        base.Awake();
    }

    protected override void Start()
    {
        // Save the placed Y BEFORE base.Start() runs.
        // base.Start() enables the NavMeshAgent which snaps transform down to the
        // NavMesh surface as a side effect. Saving first means hoverBaseY holds
        // the height you placed the wisp at in the scene, not the ground Y.
        hoverBaseY = transform.position.y;

        base.Start();

        // Tell the agent not to control Y at all.
        // Do NOT assign agent.baseOffset here — leave the Inspector value alone.
        agent.updateUpAxis = false;
        agent.updateRotation = false;

        // Restore our saved Y in case the agent pulled us down.
        Vector3 pos = transform.position;
        pos.y = hoverBaseY;
        transform.position = pos;

        echoPulseHandler.Initialise(this);
        chargeHandler.Initialise(this, agent);
    }

    public override string GetEnemyTypeID() => "Wisp";

    protected override Vector3 AdjustReturnDestination(Vector3 destination)
    {
        destination.y = transform.position.y;
        return destination;
    }

    protected override void AdvancePatrol()
    {
        if (wispPatrolPoints == null || wispPatrolPoints.Length == 0) return;
        if (!agent.enabled || !agent.isOnNavMesh) return;

        // Force destination Y to match current Y so the agent only steers XZ.
        Vector3 target = wispPatrolPoints[currentPatrolIndex].position;
        target.y = transform.position.y;
        agent.SetDestination(target);

        if (agent.pathPending) return;

        float flatDist = new Vector2(
            transform.position.x - wispPatrolPoints[currentPatrolIndex].position.x,
            transform.position.z - wispPatrolPoints[currentPatrolIndex].position.z).magnitude;

        if (flatDist < 0.5f)
            currentPatrolIndex = (currentPatrolIndex + 1) % wispPatrolPoints.Length;
    }

    protected override void OnEnemyUpdate()
    {
        // Hover runs first and writes Y. Charge runs second but only touches XZ,
        // so the two never conflict. Y is hover's exclusively.
        UpdateHover();
        chargeHandler.Tick();
        echoPulseHandler.Tick();
    }

    protected override void OnStateChanged(EnemyState prev, EnemyState next)
    {
        if (next == EnemyState.Patrol)
        {
            float closestDist = float.MaxValue;
            for (int i = 0; i < wispPatrolPoints.Length; i++)
            {
                float d = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(wispPatrolPoints[i].position.x, wispPatrolPoints[i].position.z));
                if (d < closestDist)
                {
                    closestDist = d;
                    currentPatrolIndex = i;
                }
            }
            currentPatrolIndex = (currentPatrolIndex + 1) % wispPatrolPoints.Length;
        }
    }

    protected override void PerformAttack()
    {
        chargeHandler.BeginCharge(playerTransform);
    }

    protected override void OnEnemyDeath()
    {
        chargeHandler.Cancel();
        echoPulseHandler.StopBodyParticles();

        if (deathBurstParticles != null)
            deathBurstParticles.Play();

        Invoke(nameof(CleanupBody), 10f);
    }

    public void NotifyStatusEffectPublic(string effect, bool active)
        => NotifyStatusEffect(effect, active);

    private void UpdateHover()
    {
        // Sole owner of transform.position.y. Always runs — charge only touches XZ.
        hoverTimer += Time.deltaTime * hoverFrequency * Mathf.PI * 2f;
        Vector3 pos = transform.position;
        pos.y = hoverBaseY + Mathf.Sin(hoverTimer) * hoverAmplitude;
        transform.position = pos;
    }

    private void CleanupBody()
    {
        if (this != null && gameObject != null)
            Destroy(gameObject);
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.18f);
        Gizmos.DrawWireSphere(transform.position, 12f);

        if (Application.isPlaying && chargeHandler != null && chargeHandler.IsCharging)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, transform.forward * 4f);
        }
    }
}