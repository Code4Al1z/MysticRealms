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

    [Header("Wwise")]
    [SerializeField] private AK.Wwise.Event wispAmbientEvent;

    [Header("Components")]
    [SerializeField] private WispEchoPulseHandler echoPulseHandler;
    [SerializeField] private WispChargeHandler chargeHandler;

    public UnityEngine.AI.NavMeshAgent Agent => agent;
    public float BaseMoveSpeed => baseMoveSpeed;

    private float hoverTimer = 0f;
    private float hoverBaseY;

    // Store the playing ID so we can stop this exact voice instance
    private uint ambientPlayingID = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;

    protected override void Awake()
    {
        RegisterPatrolPoints(wispPatrolPoints);
        base.Awake();
    }

    protected override void Start()
    {
        hoverBaseY = transform.position.y;

        base.Start();

        agent.updateUpAxis = false;
        agent.updateRotation = false;

        Vector3 pos = transform.position;
        pos.y = hoverBaseY;
        transform.position = pos;

        echoPulseHandler.Initialise(this);
        chargeHandler.Initialise(this, agent);

        StartAmbientLoop();
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
        StopAmbientLoop();
        chargeHandler.Cancel();
        echoPulseHandler.StopBodyParticles();

        if (deathBurstParticles != null)
            deathBurstParticles.Play();

        Invoke(nameof(CleanupBody), 10f);
    }

    public void NotifyStatusEffectPublic(string effect, bool active)
        => NotifyStatusEffect(effect, active);

    private void StartAmbientLoop()
    {
        if (wispAmbientEvent == null) return;
        if (ambientPlayingID != AkUnitySoundEngine.AK_INVALID_PLAYING_ID) return;

        // Store the playing ID so StopAmbientLoop can kill this exact voice.
        ambientPlayingID = wispAmbientEvent.Post(gameObject);
        Debug.LogWarning($"Wisp started ambient loop with playing ID {ambientPlayingID}");
    }

    private void StopAmbientLoop()
    {
        if (ambientPlayingID == AkUnitySoundEngine.AK_INVALID_PLAYING_ID) return;

        // Stop this specific playing instance with a short fade
        AkUnitySoundEngine.StopPlayingID(ambientPlayingID, 300);
        ambientPlayingID = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
    }

    private void UpdateHover()
    {
        hoverTimer += Time.deltaTime * hoverFrequency * Mathf.PI * 2f;
        Vector3 pos = transform.position;
        pos.y = hoverBaseY + Mathf.Sin(hoverTimer) * hoverAmplitude;
        transform.position = pos;
    }

    private void CleanupBody()
    {
        if (this == null || gameObject == null) return;

        // Belt-and-braces: stop the loop by playing ID first, then kill all
        // remaining voices on this object before Unity destroys it.
        StopAmbientLoop();
        AkUnitySoundEngine.StopAll(gameObject);

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