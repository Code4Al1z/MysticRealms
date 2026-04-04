using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// WispEnemy — Floating energy orb. Affected only by Echo Pulse.
///
/// BEHAVIOUR:
///   • Floats and patrols (no footsteps).
///   • Echo Pulse in range: slows it, shakes at high frequency, tints colour toward death colour.
///   • When pulse stops or player leaves range: gradually recovers speed and colour.
///   • Only killable via Echo Pulse damage-over-time while in range.
///   • On death: spawns a collectable pickup and plays death VFX.
///
/// WWISE SETUP REQUIRED:
///   • RTPC "Wisp_Speed"       — float 0–100, drives pitch/speed of ambient hum loop.
///   • RTPC "Wisp_PulseStress" — float 0–100, drives distortion/intensity while being pulsed.
///   • RTPC "Wisp_HealthPct"   — float 0–100 (inherited from BaseEnemy's healthPercentRTPC).
///   • Event "Wisp_Spawn"      — ambient loop (looping event, stopped on death).
///   • Event "Wisp_Death"      — one-shot death burst.
///   • Event "Wisp_PulseHit"   — short accent when pulse first connects.
///   • Event "Wisp_Recover"    — subtle exhale when pulse is released.
///
/// UI READINESS:
///   Inherits OnHealthChanged, OnDied, OnStatusEffectChanged events from BaseEnemy.
///   Subscribe from a future EnemyHealthBarUI component — no changes needed here.
/// </summary>
public class WispEnemy : BaseEnemy, IEchoResponsive
{
    // ─── Inspector ─────────────────────────────────────────────────────────────

    [Header("Wisp – Patrol")]
    [Tooltip("World-space positions the wisp floats between when idle.")]
    [SerializeField] private Transform[] patrolPoints;

    [Tooltip("Hover amplitude (metres) added as a sine wave to Y position.")]
    [SerializeField] private float hoverAmplitude = 0.3f;

    [Tooltip("Hover frequency (cycles per second).")]
    [SerializeField] private float hoverFrequency = 0.8f;

    [Header("Wisp – Echo Pulse Response")]
    [Tooltip("Maximum distance at which Echo Pulse affects this wisp.")]
    [SerializeField] private float maxPulseRange = 12f;

    [Tooltip("Damage per second dealt while pulse frequency is within tolerance.")]
    [SerializeField] private float damagePerSecond = 8f;

    [Tooltip("Frequency this wisp is tuned to. Pulse must be within tolerance to deal damage.")]
    [SerializeField] private float requiredFrequency = 300f;

    [Tooltip("How far from requiredFrequency the pulse can be and still deal damage.")]
    [SerializeField] private float frequencyTolerance = 30f;

    [Tooltip("Speed multiplier applied instantly when pulse first hits (0–1).")]
    [SerializeField] private float pulseSlowMultiplier = 0.35f;

    [Tooltip("Additional slow applied when frequency matches (stacks with pulseSlowMultiplier).")]
    [SerializeField] private float frequencyMatchExtraSlowMultiplier = 0.6f;

    [Tooltip("Speed recovery rate when not being pulsed (0–1 per second).")]
    [SerializeField] private float recoveryRate = 0.4f;

    [Tooltip("Shake magnitude at full frequency match stress.")]
    [SerializeField] private float maxShakeAmplitude = 0.08f;

    [Tooltip("Shake frequency (Hz).")]
    [SerializeField] private float shakeFrequency = 18f;

    [Tooltip("Frequency stress threshold (0–1) above which shaking starts.")]
    [SerializeField] private float shakeThreshold = 0.5f;

    [Header("Wisp – Visuals")]
    [Tooltip("Main renderer whose material tint changes as the wisp is stressed.")]
    [SerializeField] private Renderer wispRenderer;

    [Tooltip("Resting colour (healthy, unstressed).")]
    [SerializeField] private Color healthyColor = new Color(0.4f, 0.8f, 1f);

    [Tooltip("Colour at maximum stress / near death.")]
    [SerializeField] private Color stressedColor = new Color(1f, 0.2f, 0.1f);

    [Tooltip("Particle system representing the wisp's ambient 'body'.")]
    [SerializeField] private ParticleSystem wispBodyParticles;

    [Tooltip("Particle system played on death.")]
    [SerializeField] private ParticleSystem deathBurstParticles;

    [Header("Wisp – Collectable")]
    [Tooltip("Prefab spawned at wisp position when it dies.")]
    [SerializeField] private GameObject collectablePrefab;

    [Header("Wwise – Wisp Specific")]
    [SerializeField] private AK.Wwise.Event wispPulseHitEvent;
    [SerializeField] private AK.Wwise.Event wispRecoverEvent;
    [SerializeField] private AK.Wwise.RTPC wispSpeedRTPC;
    [SerializeField] private AK.Wwise.RTPC wispPulseStressRTPC;

    // ─── Runtime State ──────────────────────────────────────────────────────────

    // 0 = no stress, 1 = max stress
    private float stressLevel = 0f;
    // tracks whether pulse is currently hitting us
    private bool isBeingPulsed = false;
    // tracks whether it was a fresh connection this frame (for one-shot sound)
    private bool pulseJustConnected = false;

    private float currentSpeedRecovery = 1f; // 0–1, actual multiplier toward base
    private Vector3 basePosition;
    private int currentPatrolIndex = 0;
    private float hoverTimer = 0f;

    // Shader colour property (standard URP lit uses "_BaseColor")
    private static readonly int ShaderColorID = Shader.PropertyToID("_BaseColor");
    private Material wispMaterialInstance;

    // ─── BaseEnemy Overrides ────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();

        if (wispRenderer != null)
            wispMaterialInstance = wispRenderer.material; // instanced copy

        basePosition = transform.position;
    }

    protected override void Start()
    {
        base.Start();

        // Wisps float — disable NavMeshAgent gravity/y-control; we'll drive Y manually.
        agent.updateUpAxis    = false;
        agent.updateRotation  = false;
        agent.baseOffset      = 0f;

        SetNextPatrolTarget();
    }

    public override string GetEnemyTypeID() => "Wisp";

    protected override void OnEnemyUpdate()
    {
        UpdateHover();
        UpdatePatrol();
        UpdateStressRecovery();
        UpdateVisuals();
        UpdateWwiseRTPCs();

        isBeingPulsed = false; // reset each frame; set back to true by OnEchoPulseActive if called
    }

    // ─── IEchoResponsive ────────────────────────────────────────────────────────

    public float GetRequiredFrequency() => requiredFrequency;

    public void OnEchoPulseActive(Vector3 sourcePosition, float distance, float frequency)
    {
        if (isDead) return;
        if (distance > maxPulseRange) return;

        bool wasBeingPulsed = isBeingPulsed;
        isBeingPulsed = true;

        // One-shot hit sound on first frame of connection
        if (!wasBeingPulsed)
        {
            wispPulseHitEvent?.Post(gameObject);
            NotifyStatusEffect("EchoPulse", true);
        }

        bool frequencyMatches = Mathf.Abs(frequency - requiredFrequency) <= frequencyTolerance;

        // Stress climbs faster when frequency matches
        float stressSpeed = frequencyMatches ? 1.5f : 0.6f;
        stressLevel = Mathf.MoveTowards(stressLevel, 1f, stressSpeed * Time.deltaTime);

        // Speed: base slow + extra slow on freq match
        float speedMult = pulseSlowMultiplier;
        if (frequencyMatches)
            speedMult *= frequencyMatchExtraSlowMultiplier;

        SetSpeedMultiplier("echo_pulse", speedMult);
        currentSpeedRecovery = speedMult;

        // Damage only on frequency match
        if (frequencyMatches)
        {
            TakeDamage(damagePerSecond * Time.deltaTime, "EchoPulse");
        }

        // Shake the transform when stress is high
        if (stressLevel >= shakeThreshold)
        {
            float shakeMag = Mathf.InverseLerp(shakeThreshold, 1f, stressLevel) * maxShakeAmplitude;
            Vector3 shakeOffset = new Vector3(
                Mathf.Sin(Time.time * shakeFrequency * 1.3f) * shakeMag,
                Mathf.Sin(Time.time * shakeFrequency)        * shakeMag * 0.5f,
                Mathf.Sin(Time.time * shakeFrequency * 0.7f) * shakeMag
            );
            transform.position += shakeOffset;
        }
    }

    public void OnEchoPulseStopped()
    {
        if (!isBeingPulsed) return; // already clean

        isBeingPulsed = false;
        wispRecoverEvent?.Post(gameObject);
        NotifyStatusEffect("EchoPulse", false);
    }

    // ─── Patrol & Hover ────────────────────────────────────────────────────────

    private void UpdatePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        // Set destination on XZ plane only — Y is driven by hover
        Vector3 target = patrolPoints[currentPatrolIndex].position;
        target.y = transform.position.y;
        agent.SetDestination(target);

        float flatDist = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(target.x, target.z));

        if (flatDist < 0.5f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            SetNextPatrolTarget();
        }
    }

    private void SetNextPatrolTarget()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        basePosition = patrolPoints[currentPatrolIndex].position;
    }

    private void UpdateHover()
    {
        hoverTimer += Time.deltaTime * hoverFrequency * Mathf.PI * 2f;
        float yOffset = Mathf.Sin(hoverTimer) * hoverAmplitude;
        Vector3 pos = transform.position;
        pos.y = basePosition.y + yOffset;
        transform.position = pos;
    }

    // ─── Stress / Recovery ─────────────────────────────────────────────────────

    private void UpdateStressRecovery()
    {
        if (isBeingPulsed) return;

        // Gradually relieve stress
        stressLevel = Mathf.MoveTowards(stressLevel, 0f, recoveryRate * Time.deltaTime);

        // Recover speed
        currentSpeedRecovery = Mathf.MoveTowards(currentSpeedRecovery, 1f, recoveryRate * Time.deltaTime);
        SetSpeedMultiplier("echo_pulse", currentSpeedRecovery);

        if (stressLevel <= 0f)
            ClearSpeedMultiplier("echo_pulse");
    }

    // ─── Visuals ───────────────────────────────────────────────────────────────

    private void UpdateVisuals()
    {
        if (wispMaterialInstance == null) return;

        // Interpolate colour by stress level
        Color targetColor = Color.Lerp(healthyColor, stressedColor, stressLevel);
        wispMaterialInstance.SetColor(ShaderColorID, targetColor);

        // Drive particle emission rate down as stress increases
        if (wispBodyParticles != null)
        {
            var emission = wispBodyParticles.emission;
            float baseRate = 30f;
            emission.rateOverTime = Mathf.Lerp(baseRate, baseRate * 0.2f, stressLevel);
        }
    }

    // ─── Wwise ─────────────────────────────────────────────────────────────────

    private void UpdateWwiseRTPCs()
    {
        float speedNorm = (baseMoveSpeed > 0f)
            ? (agent != null ? agent.speed / baseMoveSpeed : 1f)
            : 1f;

        wispSpeedRTPC?.SetValue(gameObject, speedNorm * 100f);
        wispPulseStressRTPC?.SetValue(gameObject, stressLevel * 100f);
    }

    // ─── Death ─────────────────────────────────────────────────────────────────

    protected override void OnEnemyDeath()
    {
        // Stop ambient body particles
        if (wispBodyParticles != null)
            wispBodyParticles.Stop();

        // Play death burst (detached so it outlives the object)
        if (deathBurstParticles != null)
        {
            deathBurstParticles.transform.SetParent(null);
            deathBurstParticles.Play();
        }

        // Spawn collectable
        if (collectablePrefab != null)
            Instantiate(collectablePrefab, transform.position, Quaternion.identity);

        // Destroy self after a short delay (lets Wwise death event finish)
        Destroy(gameObject, 0.5f);
    }

    // ─── Gizmos ────────────────────────────────────────────────────────────────

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, maxPulseRange);

        if (patrolPoints != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                Gizmos.DrawSphere(patrolPoints[i].position, 0.15f);
                if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
            }
        }
    }
}
