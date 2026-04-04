using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// RockGolemEnemy — Heavy stone creature. Affected only by Resonance Hum.
///
/// BEHAVIOUR:
///   • Patrols on ground, has surface-aware footsteps via SurfaceAudioManager.
///   • On first Resonance Hum contact: knocked back (impulse + large damage hit).
///   • While hum sustains and golem is in range: slowed + damage-over-time drain.
///   • When hum stops or golem leaves range: returns to normal speed, no drain.
///   • Only killable via Resonance Hum.
///
/// FOOTSTEP SETUP:
///   Assign the same SurfaceAudioManager instance used by the player, OR a second
///   instance referencing heavier Wwise Switch variants for the golem.
///   The golem's footstep interval and Wwise events are separate Inspector fields
///   so the sounds can be tuned independently (slower, heavier).
///
/// WWISE SETUP REQUIRED:
///   • RTPC "Golem_Speed"           — float 0–100, drives footstep tempo / voice pitch.
///   • RTPC "Golem_ResonanceStress" — float 0–100, drives tremor / crumble layering.
///   • RTPC "Golem_HealthPct"       — float 0–100 (use BaseEnemy's healthPercentRTPC).
///   • Event "Golem_Spawn"          — stone rumble on spawn.
///   • Event "Golem_Death"          — full collapse sound.
///   • Event "Golem_KnockbackHit"   — one-shot impact when first knocked back.
///   • Event "Golem_Footstep"       — used via SurfaceAudioManager switch container.
///   • Event "Golem_Land"           — used via SurfaceAudioManager switch container.
///   Switch Group "SurfaceType" (shared with player) — golem Switch Container reads same group.
///
/// UI READINESS:
///   Inherits OnHealthChanged, OnDied, OnStatusEffectChanged events from BaseEnemy.
///   Subscribe from a future EnemyHealthBarUI component — no changes needed here.
/// </summary>
public class RockGolemEnemy : BaseEnemy, IResonanceResponsive
{
    // ─── Inspector ─────────────────────────────────────────────────────────────

    [Header("Golem – Patrol")]
    [SerializeField] private Transform[] golemPatrolPoints;
    [SerializeField] private float patrolStoppingDistance = 0.6f;

    [Header("Golem – Resonance Hum Response")]
    [Tooltip("Maximum distance at which Resonance Hum affects this golem.")]
    [SerializeField] private float maxResonanceRange = 12f;

    [Tooltip("Knockback force applied as an impulse on first Resonance Hum contact.")]
    [SerializeField] private float knockbackForce = 14f;

    [Tooltip("Duration (seconds) the golem's NavMesh agent is disabled after knockback.")]
    [SerializeField] private float knockbackStunDuration = 1.2f;

    [Tooltip("Damage dealt immediately on the knockback hit.")]
    [SerializeField] private float knockbackDamage = 25f;

    [Tooltip("Damage per second while Resonance Hum sustains and golem is in range.")]
    [SerializeField] private float drainDamagePerSecond = 6f;

    [Tooltip("Speed multiplier while being drained (0–1).")]
    [SerializeField] private float resonanceSlowMultiplier = 0.3f;

    [Tooltip("Cooldown (seconds) before the golem can be knocked back again.")]
    [SerializeField] private float knockbackCooldown = 4f;

    [Header("Golem – Footsteps")]
    [Tooltip("Reference to the SurfaceAudioManager (can share with player or use a separate golem instance).")]
    [SerializeField] private SurfaceAudioManager surfaceAudioManager;

    [Tooltip("Seconds between footstep events at normal speed.")]
    [SerializeField] private float footstepInterval = 0.75f;

    [Tooltip("Minimum agent speed to trigger footsteps.")]
    [SerializeField] private float minSpeedForFootsteps = 0.3f;

    [Tooltip("Ground check origin (assign a child Transform at the golem's feet).")]
    [SerializeField] private Transform groundCheck;

    [Tooltip("Ground check radius.")]
    [SerializeField] private float groundCheckRadius = 0.4f;

    [Tooltip("Layer mask for ground surfaces.")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Golem – Visual Stress")]
    [Tooltip("Particle system emitting dust/rock chips when under resonance stress.")]
    [SerializeField] private ParticleSystem stressParticles;

    [Tooltip("Particle system played on death (collapse cloud).")]
    [SerializeField] private ParticleSystem deathParticles;

    [Header("Golem – Melee Attack")]
    [Tooltip("Optional child Transform at the golem's fist for hit detection. Falls back to transform if null.")]
    [SerializeField] private Transform attackPoint;

    [Tooltip("Radius of the melee hit sphere.")]
    [SerializeField] private float meleeRadius = 1.2f;

    [Tooltip("Damage per melee swing.")]
    [SerializeField] private float meleeDamage = 15f;

    [Tooltip("Layer mask for the player.")]
    [SerializeField] private LayerMask playerLayer;

    [Tooltip("Optional particle burst played on a swing.")]
    [SerializeField] private ParticleSystem swingParticles;

    [Header("Wwise – Golem Specific")]
    [SerializeField] private AK.Wwise.Event knockbackHitEvent;
    [SerializeField] private AK.Wwise.Event meleeSwingEvent;
    [SerializeField] private AK.Wwise.RTPC golemSpeedRTPC;
    [SerializeField] private AK.Wwise.RTPC golemResonanceStressRTPC;

    // ─── Runtime State ──────────────────────────────────────────────────────────

    private Rigidbody rb;

    // Resonance state
    private bool isBeingDrained = false;
    private bool wasBeingDrained = false;
    private float resonanceStress = 0f; // 0–1 for RTPC/VFX
    private float knockbackTimer = -999f;
    private bool isKnockedBack = false;
    private float knockbackStunTimer = 0f;

    // Footstep state
    private float footstepTimer = 0f;
    private Collider lastSurfaceCollider = null;

    // ─── BaseEnemy Overrides ────────────────────────────────────────────────────

    protected override void Awake()
    {
        RegisterPatrolPoints(golemPatrolPoints);
        base.Awake();
        rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.freezeRotation = true;
        rb.isKinematic = false;
    }

    protected override void Start()
    {
        base.Start();

        if (surfaceAudioManager == null)
            Debug.LogWarning("[RockGolem] SurfaceAudioManager not assigned. Footsteps will be silent.");

        SetNextPatrolTarget();
    }

    public override string GetEnemyTypeID() => "RockGolem";

    protected override void OnEnemyUpdate()
    {
        HandleKnockbackRecovery();
        UpdateFootsteps();
        UpdateVisualStress();
        UpdateWwiseRTPCs();

        // Reset drain flag each frame — set back to true by OnResonanceHumActive
        wasBeingDrained = isBeingDrained;
        isBeingDrained = false;
    }

    // ─── BaseEnemy Attack ───────────────────────────────────────────────────────

    protected override void PerformAttack()
    {
        meleeSwingEvent?.Post(gameObject);
        swingParticles?.Play();

        Vector3 hitOrigin = attackPoint != null ? attackPoint.position : transform.position;
        Collider[] hits = Physics.OverlapSphere(hitOrigin, meleeRadius, playerLayer);

        foreach (Collider col in hits)
        {
            PlayerHealth ph = col.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(meleeDamage);

                if (enableDebugLog)
                    Debug.Log($"[RockGolem] Melee hit player for {meleeDamage}");
            }
        }
    }

    // ─── IResonanceResponsive ───────────────────────────────────────────────────

    public void OnResonanceHumActive(Vector3 sourcePosition, float distance)
    {
        if (isDead) return;
        if (distance > maxResonanceRange) return;

        isBeingDrained = true;

        bool canKnockBack = (Time.time > knockbackTimer + knockbackCooldown);
        bool freshContact = !wasBeingDrained;

        if (freshContact && canKnockBack)
        {
            ApplyKnockback(sourcePosition);
        }
        else
        {
            // Sustained drain phase
            SetSpeedMultiplier("resonance_hum", resonanceSlowMultiplier);
            TakeDamage(drainDamagePerSecond * Time.deltaTime, "ResonanceHum");
        }

        resonanceStress = Mathf.MoveTowards(resonanceStress, 1f, 1.5f * Time.deltaTime);

        if (freshContact)
            NotifyStatusEffect("ResonanceHum", true);
    }

    public void OnResonanceHumStopped()
    {
        if (!wasBeingDrained && !isBeingDrained) return;

        isBeingDrained = false;
        ClearSpeedMultiplier("resonance_hum");
        NotifyStatusEffect("ResonanceHum", false);

        if (enableDebugLog)
            Debug.Log("[RockGolem] Resonance hum stopped — recovering.");
    }

    // ─── Knockback ─────────────────────────────────────────────────────────────

    private void ApplyKnockback(Vector3 sourcePosition)
    {
        knockbackTimer = Time.time;
        isKnockedBack = true;
        knockbackStunTimer = knockbackStunDuration;

        // Disable nav agent so physics can drive the body
        agent.isStopped = true;
        agent.enabled = false;

        // Direction: away from player
        Vector3 dir = (transform.position - sourcePosition).normalized;
        dir.y = 0.3f; // slight upward pop
        dir.Normalize();

        rb.AddForce(dir * knockbackForce, ForceMode.Impulse);

        TakeDamage(knockbackDamage, "ResonanceHum_Knockback");
        knockbackHitEvent?.Post(gameObject);

        if (enableDebugLog)
            Debug.Log($"[RockGolem] Knocked back! Dealt {knockbackDamage} damage.");
    }

    private void HandleKnockbackRecovery()
    {
        if (!isKnockedBack) return;

        knockbackStunTimer -= Time.deltaTime;

        if (knockbackStunTimer <= 0f)
        {
            isKnockedBack = false;

            // Re-enable nav agent on NavMesh
            agent.enabled = true;
            agent.isStopped = false;
            agent.Warp(transform.position); // resync position after physics displacement

            SetNextPatrolTarget();

            if (enableDebugLog)
                Debug.Log("[RockGolem] Recovered from knockback, resuming patrol.");
        }
    }

    // ─── Patrol ────────────────────────────────────────────────────────────────

    protected override void AdvancePatrol()
    {
        if (isKnockedBack) return;
        if (golemPatrolPoints == null || golemPatrolPoints.Length == 0) return;
        if (!agent.enabled || !agent.isOnNavMesh) return;

        if (agent.remainingDistance <= patrolStoppingDistance && !agent.pathPending)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % golemPatrolPoints.Length;
            SetNextPatrolTarget();
        }
    }

    private void SetNextPatrolTarget()
    {
        if (golemPatrolPoints == null || golemPatrolPoints.Length == 0) return;
        if (!agent.enabled || !agent.isOnNavMesh) return;
        agent.SetDestination(golemPatrolPoints[currentPatrolIndex].position);
    }

    // ─── Footsteps ─────────────────────────────────────────────────────────────

    private void UpdateFootsteps()
    {
        if (surfaceAudioManager == null) return;
        if (isKnockedBack) return;

        float speed = agent.enabled ? agent.velocity.magnitude : 0f;
        bool isMoving = speed > minSpeedForFootsteps;

        // Scale interval by current speed: slower movement = longer interval
        float scaledInterval = baseMoveSpeed > 0f
            ? footstepInterval * (baseMoveSpeed / Mathf.Max(speed, 0.01f))
            : footstepInterval;
        scaledInterval = Mathf.Clamp(scaledInterval, footstepInterval * 0.5f, footstepInterval * 3f);

        if (isMoving)
        {
            footstepTimer += Time.deltaTime;

            if (footstepTimer >= scaledInterval)
            {
                PlayFootstep();
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }

        // Surface detection — same pattern as PlayerController
        if (isMoving && groundCheck != null)
        {
            Collider[] overlaps = Physics.OverlapSphere(groundCheck.position, groundCheckRadius, groundLayer);
            if (overlaps != null && overlaps.Length > 0)
                TryUpdateSurface(overlaps[0]);
        }
    }

    private void PlayFootstep()
    {
        surfaceAudioManager?.OnFootstep(gameObject);
    }

    private void TryUpdateSurface(Collider col)
    {
        if (col == lastSurfaceCollider) return;
        lastSurfaceCollider = col;
        surfaceAudioManager?.UpdateCurrentSurface(col);
    }

    // ─── Visual Stress ─────────────────────────────────────────────────────────

    private void UpdateVisualStress()
    {
        // Stress drains when not being affected
        if (!isBeingDrained)
            resonanceStress = Mathf.MoveTowards(resonanceStress, 0f, 0.8f * Time.deltaTime);

        if (stressParticles == null) return;

        var emission = stressParticles.emission;

        if (resonanceStress > 0.05f)
        {
            if (!stressParticles.isPlaying)
                stressParticles.Play();

            emission.rateOverTime = Mathf.Lerp(0f, 40f, resonanceStress);
        }
        else
        {
            if (stressParticles.isPlaying)
                stressParticles.Stop();
        }
    }

    // ─── Wwise ─────────────────────────────────────────────────────────────────

    private void UpdateWwiseRTPCs()
    {
        float speed = agent.enabled ? agent.velocity.magnitude : 0f;
        float speedNorm = baseMoveSpeed > 0f ? speed / baseMoveSpeed : 0f;

        golemSpeedRTPC?.SetValue(gameObject, speedNorm * 100f);
        golemResonanceStressRTPC?.SetValue(gameObject, resonanceStress * 100f);
    }

    // ─── Death ─────────────────────────────────────────────────────────────────

    protected override void OnEnemyDeath()
    {
        if (stressParticles != null)
            stressParticles.Stop();

        if (deathParticles != null)
        {
            deathParticles.transform.SetParent(null);
            deathParticles.Play();
        }

        // Let the deathEvent (set in BaseEnemy inspector) play, then destroy
        Destroy(gameObject, 1.2f);
    }

    // ─── Gizmos ────────────────────────────────────────────────────────────────

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, maxResonanceRange);

        if (golemPatrolPoints != null)
        {
            Gizmos.color = new Color(0.8f, 0.4f, 0.1f);
            for (int i = 0; i < golemPatrolPoints.Length; i++)
            {
                if (golemPatrolPoints[i] == null) continue;
                Gizmos.DrawSphere(golemPatrolPoints[i].position, 0.2f);
                if (i < golemPatrolPoints.Length - 1 && golemPatrolPoints[i + 1] != null)
                    Gizmos.DrawLine(golemPatrolPoints[i].position, golemPatrolPoints[i + 1].position);
            }
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}