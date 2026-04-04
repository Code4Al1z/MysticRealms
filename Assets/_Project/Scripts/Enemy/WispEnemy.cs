using UnityEngine;

/// <summary>
/// WispEnemy — Floating energy orb. Affected only by Echo Pulse.
///
/// ── ATTACK ─────────────────────────────────────────────────────────────────
///   The wisp uses a TRIGGER COLLIDER attack — no separate prefab needed.
///   When the attack fires, the wisp charges through the player in a straight
///   line (forward + back). A Trigger Collider on the wisp (set IsTrigger=true
///   in the Inspector) calls OnTriggerEnter each time the player enters it.
///
///   ALIVE:   OnTriggerEnter → deal damage to PlayerHealth
///   DEAD:    OnTriggerEnter → award collectable point to PlayerHealth
///            (the wisp body lingers briefly as a pickup — no separate prefab)
///
/// ── ECHO PULSE RESPONSE ────────────────────────────────────────────────────
///   • Slows the wisp (stressLevel → speed multiplier)
///   • Shakes at high stress
///   • Tints colour from healthyColor → stressedColor
///   • Damage-over-time when frequency matches requiredFrequency ± tolerance
///   • Stress/speed recover when pulse stops or player is out of range
///
/// ── WWISE RTPCs REQUIRED ───────────────────────────────────────────────────
///   Wisp_Speed        0–100  ambient hum pitch/rate
///   Wisp_PulseStress  0–100  distortion intensity
///   healthPercentRTPC 0–100  (from BaseEnemy inspector field)
///
/// ── WWISE EVENTS REQUIRED ──────────────────────────────────────────────────
///   spawnEvent / deathEvent  (BaseEnemy inspector fields)
///   Wisp_PulseHit            one-shot on first pulse contact
///   Wisp_Recover             one-shot when pulse is released
///   Wisp_ChargeAttack        one-shot when charge begins
/// </summary>
public class WispEnemy : BaseEnemy, IEchoResponsive
{
    // ─────────────────────────────────────────────────────────── Inspector ───

    [Header("Wisp - Patrol Points")]
    [SerializeField] private Transform[] wispPatrolPoints;

    [Header("Wisp - Hover")]
    [SerializeField] private float hoverAmplitude = 0.35f;
    [SerializeField] private float hoverFrequency = 0.9f;

    [Header("Wisp - Echo Pulse Response")]
    [SerializeField] private float maxPulseRange = 12f;
    [SerializeField] private float damagePerSecond = 8f;
    [SerializeField] private float requiredFrequency = 300f;
    [SerializeField] private float frequencyTolerance = 30f;
    [SerializeField] private float pulseSlowMultiplier = 0.35f;
    [SerializeField] private float freqMatchExtraSlow = 0.6f;
    [SerializeField] private float stressRecoveryRate = 0.4f;
    [SerializeField] private float maxShakeAmplitude = 0.08f;
    [SerializeField] private float shakeFrequency = 18f;
    [SerializeField] private float shakeThreshold = 0.5f;

    [Header("Wisp - Visuals")]
    [SerializeField] private Renderer wispRenderer;
    [SerializeField] private Color healthyColor = new Color(0.4f, 0.8f, 1f);
    [SerializeField] private Color stressedColor = new Color(1f, 0.2f, 0.1f);
    [SerializeField] private ParticleSystem wispBodyParticles;
    [SerializeField] private ParticleSystem deathBurstParticles;

    [Header("Wisp - Attack (Charge)")]
    [Tooltip("Damage dealt each time the player enters the wisp trigger while alive.")]
    [SerializeField] private float contactDamage = 10f;
    [Tooltip("Speed of the charge dash.")]
    [SerializeField] private float chargeSpeed = 14f;
    [Tooltip("Distance past the player the wisp flies before returning.")]
    [SerializeField] private float chargeDistance = 4f;
    [Tooltip("Collectable points awarded when a dead wisp is collected.")]
    [SerializeField] private int collectableValue = 1;

    [Header("Wwise - Wisp Specific")]
    [SerializeField] private AK.Wwise.Event wispPulseHitEvent;
    [SerializeField] private AK.Wwise.Event wispRecoverEvent;
    [SerializeField] private AK.Wwise.Event wispChargeEvent;
    [SerializeField] private AK.Wwise.RTPC wispSpeedRTPC;
    [SerializeField] private AK.Wwise.RTPC wispPulseStressRTPC;

    // ───────────────────────────────────────────────────────── Runtime State ─

    private float stressLevel = 0f;
    private bool isBeingPulsed = false;
    private bool wasPulsedLastFrame = false;

    private float hoverTimer = 0f;
    private float hoverBaseY;            // float — only the Y value matters

    // Charge is XZ only. Y is solely owned by hover and never written by charge.
    private bool isCharging = false;
    private bool isReturning = false;
    private Vector2 chargeTargetXZ;
    private Vector2 returnTargetXZ;

    private static readonly int ShaderBaseColor = Shader.PropertyToID("_BaseColor");
    private Material wispMat;

    // ─────────────────────────────────────────────────────── BaseEnemy Setup ─

    protected override void Awake()
    {
        base.Awake();
        RegisterPatrolPoints(wispPatrolPoints);

        if (wispRenderer != null)
            wispMat = wispRenderer.material;
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
    }

    public override string GetEnemyTypeID() => "Wisp";

    // ──────────────────────────────────────────────────── Override: Patrol ───

    protected override void AdvancePatrol()
    {
        if (wispPatrolPoints == null || wispPatrolPoints.Length == 0) return;
        if (!agent.enabled || !agent.isOnNavMesh) return;

        // Force destination Y to match current Y so the agent only steers XZ.
        Vector3 target = wispPatrolPoints[currentPatrolIndex].position;
        target.y = transform.position.y;
        agent.SetDestination(target);

        float flatDist = new Vector2(
            transform.position.x - wispPatrolPoints[currentPatrolIndex].position.x,
            transform.position.z - wispPatrolPoints[currentPatrolIndex].position.z).magnitude;

        if (flatDist < 0.5f)
            currentPatrolIndex = (currentPatrolIndex + 1) % wispPatrolPoints.Length;
    }

    // ──────────────────────────────────────────────────── Override: Update ───

    protected override void OnEnemyUpdate()
    {
        // Hover runs first and writes Y. Charge runs second but only touches XZ,
        // so the two never conflict. Y is hover's exclusively.
        UpdateHover();
        UpdateCharge();
        UpdateStressRecovery();
        UpdateVisuals();
        UpdateWwiseRTPCs();

        wasPulsedLastFrame = isBeingPulsed;
        isBeingPulsed = false;
    }

    // ─────────────────────────────────────────────────── Override: Attack ────

    protected override void PerformAttack()
    {
        if (isCharging || isReturning) return;
        if (playerTransform == null) return;
        StartCharge();
    }

    // ─────────────────────────────────────────────────── IEchoResponsive ─────

    public float GetRequiredFrequency() => requiredFrequency;

    public void OnEchoPulseActive(Vector3 sourcePosition, float distance, float frequency)
    {
        if (isDead) return;
        if (distance > maxPulseRange) return;

        isBeingPulsed = true;

        if (!wasPulsedLastFrame)
        {
            wispPulseHitEvent?.Post(gameObject);
            NotifyStatusEffect("EchoPulse", true);
        }

        bool freqMatch = Mathf.Abs(frequency - requiredFrequency) <= frequencyTolerance;
        float stressSpd = freqMatch ? 1.5f : 0.6f;
        stressLevel = Mathf.MoveTowards(stressLevel, 1f, stressSpd * Time.deltaTime);

        float slow = pulseSlowMultiplier * (freqMatch ? freqMatchExtraSlow : 1f);
        SetSpeedMultiplier("echo_pulse", slow);

        if (freqMatch)
            TakeDamage(damagePerSecond * Time.deltaTime, "EchoPulse");

        if (stressLevel >= shakeThreshold)
        {
            float mag = Mathf.InverseLerp(shakeThreshold, 1f, stressLevel) * maxShakeAmplitude;
            transform.position += new Vector3(
                Mathf.Sin(Time.time * shakeFrequency * 1.3f) * mag,
                Mathf.Sin(Time.time * shakeFrequency) * mag * 0.5f,
                Mathf.Sin(Time.time * shakeFrequency * 0.7f) * mag);
        }
    }

    public void OnEchoPulseStopped()
    {
        isBeingPulsed = false;
        wispRecoverEvent?.Post(gameObject);
        NotifyStatusEffect("EchoPulse", false);
    }

    // ──────────────────────────────────────────────────────── Trigger Attack ─

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null) return;

        if (isDead)
        {
            ph.AddCollectable(collectableValue);
            Destroy(gameObject);
        }
        else
        {
            ph.TakeDamage(contactDamage);
        }
    }

    // ───────────────────────────────────────────────────────── Charge Logic ──

    private void StartCharge()
    {
        if (playerTransform == null) return;

        isCharging = true;
        isReturning = false;

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

        if (enableDebugLog)
            Debug.Log($"[Wisp] Charge toward XZ {chargeTargetXZ}");
    }

    private void UpdateCharge()
    {
        if (!isCharging && !isReturning) return;

        Vector2 currentXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 targetXZ = isCharging ? chargeTargetXZ : returnTargetXZ;
        float speed = isCharging ? chargeSpeed : chargeSpeed * 0.7f;

        // Move on XZ only. Y stays exactly as hover set it this frame.
        Vector2 nextXZ = Vector2.MoveTowards(currentXZ, targetXZ, speed * Time.deltaTime);
        transform.position = new Vector3(nextXZ.x, transform.position.y, nextXZ.y);

        float dist = Vector2.Distance(nextXZ, targetXZ);

        if (isCharging && dist < 0.15f)
        {
            isCharging = false;
            isReturning = true;
        }
        else if (isReturning && dist < 0.2f)
        {
            isReturning = false;
            agent.enabled = true;
            agent.isStopped = false;
            // No Warp — agent resumes from current transform, updateUpAxis=false
            // means it will never write Y again.
        }
    }

    // ─────────────────────────────────────────────────── Stress / Recovery ───

    private void UpdateStressRecovery()
    {
        if (isBeingPulsed) return;

        stressLevel = Mathf.MoveTowards(stressLevel, 0f, stressRecoveryRate * Time.deltaTime);

        float recovery = Mathf.Lerp(pulseSlowMultiplier, 1f, 1f - stressLevel);
        SetSpeedMultiplier("echo_pulse", recovery);

        if (stressLevel <= 0f)
            ClearSpeedMultiplier("echo_pulse");
    }

    // ──────────────────────────────────────────────────────────── Hover ──────

    private void UpdateHover()
    {
        // Sole owner of transform.position.y. Always runs — charge only touches XZ.
        hoverTimer += Time.deltaTime * hoverFrequency * Mathf.PI * 2f;
        Vector3 pos = transform.position;
        pos.y = hoverBaseY + Mathf.Sin(hoverTimer) * hoverAmplitude;
        transform.position = pos;
    }

    // ─────────────────────────────────────────────────────── Visuals / RTPC ──

    private void UpdateVisuals()
    {
        if (wispMat != null)
            wispMat.SetColor(ShaderBaseColor, Color.Lerp(healthyColor, stressedColor, stressLevel));

        if (wispBodyParticles != null)
        {
            var em = wispBodyParticles.emission;
            em.rateOverTime = Mathf.Lerp(30f, 6f, stressLevel);
        }
    }

    private void UpdateWwiseRTPCs()
    {
        float speedNorm = (baseMoveSpeed > 0f && agent != null)
            ? agent.speed / baseMoveSpeed : 1f;

        wispSpeedRTPC?.SetValue(gameObject, speedNorm * 100f);
        wispPulseStressRTPC?.SetValue(gameObject, stressLevel * 100f);
    }

    // ──────────────────────────────────────────────────── Death Override ──────

    protected override void OnEnemyDeath()
    {
        wispBodyParticles?.Stop();

        if (deathBurstParticles != null)
        {
            //deathBurstParticles.transform.SetParent(null);
            deathBurstParticles.Play();
        }

        isCharging = false;
        isReturning = false;

        Invoke(nameof(CleanupBody), 10f);
    }

    private void CleanupBody()
    {
        if (this != null && gameObject != null)
            Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────── Gizmos ──────

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.18f);
        Gizmos.DrawWireSphere(transform.position, maxPulseRange);

        if (Application.isPlaying && isCharging)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position,
                new Vector3(chargeTargetXZ.x, transform.position.y, chargeTargetXZ.y));
        }
    }
}