using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Base class for all Mystic Realms enemies.
/// ── LEASH CALCULATION ──────────────────────────────────────────────────────
///   At Start(), BakePatrolPath() walks the patrol point array and sums the
///   NavMesh path length of each segment (falling back to straight-line if the
///   path is incomplete). leashRadius = total perimeter × leashRadiusMultiplier.
///   Designers tune only the multiplier. The leash origin updates each frame
///   while patrolling to the nearest patrol point, so a long L-shaped route
///   doesn't produce an oversized bubble at one end.
///
/// ── INHERITANCE CONTRACT ───────────────────────────────────────────────────
///   • Call RegisterPatrolPoints(points) from subclass Awake BEFORE base.Start().
///   • Override OnEnemyUpdate()  — per-frame logic (after AI tick).
///   • Override PerformAttack()  — actual attack implementation.
///   • Override OnEnemyDeath()   — VFX / loot.
///   • Override OnStateChanged() — optional, for audio/VFX reactions to state.
///   • Override AdvancePatrol()  — optional, if patrol movement is non-standard (wisp).
///   • Override GetEnemyTypeID() — short string for Wwise state routing.
///
/// ── UI READINESS ───────────────────────────────────────────────────────────
///   Events: OnHealthChanged(current,max)  OnDied(go)  OnStatusEffectChanged(name,active)
///   Subscribe from EnemyHealthBarUI when built — no changes needed here.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public abstract class BaseEnemy : MonoBehaviour, IEnemyDamageable
{
    // ─── Inspector ─────────────────────────────────────────────────────────────

    [Header("Enemy Identity")]
    [Tooltip("Unique display name used in debug logs and future UI.")]
    [SerializeField] protected string enemyDisplayName = "Enemy";

    [Header("Health")]
    [SerializeField] protected float maxHealth = 100f;

    [Header("Movement")]
    [SerializeField] protected float baseMoveSpeed = 3f;
    [SerializeField] protected float chaseSpeedBoost = 1.4f; // multiplier while chasing

    [Header("AI – Detection")]
    [Tooltip("Player must be within this radius for the enemy to begin chasing.")]
    [SerializeField] protected float viewRange = 10f;

    [Tooltip("Enemy enters Attack state when the player is within this radius.")]
    [SerializeField] protected float attackRange = 1.8f;

    [Tooltip("Seconds between attack attempts.")]
    [SerializeField] protected float attackCooldown = 1.5f;

    [Tooltip("How long the enemy idles at a patrol point after returning from a chase.")]
    [SerializeField] protected float idleDuration = 3f;

    [Header("AI – Leash")]
    [Tooltip("Multiplies the baked patrol perimeter to produce the leash radius.\n" +
             "1.0 = can chase as far as the full patrol loop length.\n" +
             "0.5 = half that. Falls back to viewRange × 1.5 if no patrol points exist.")]
    [SerializeField] protected float leashRadiusMultiplier = 0.6f;

    [SerializeField] protected bool showLeashGizmo = true;

    [Header("Wwise – Shared")]
    [Tooltip("Posted when the enemy is first spawned / becomes active.")]
    [SerializeField] protected AK.Wwise.Event spawnEvent;

    [Tooltip("Posted on death, before the object is destroyed.")]
    [SerializeField] protected AK.Wwise.Event deathEvent;

    [Tooltip("RTPC reflecting current health percentage (0–100).")]
    [SerializeField] protected AK.Wwise.RTPC healthPercentRTPC;

    [Header("Debug")]
    [SerializeField] protected bool enableDebugLog = false;

    // ─── Runtime State ──────────────────────────────────────────────────────────

    public enum EnemyState { Patrol, Chase, Attack, Return, Idle }

    private EnemyState _state = EnemyState.Patrol;
    public EnemyState CurrentState => _state;

    // ──── Runtime Data ───────────────────────────────────────────────────────

    protected NavMeshAgent agent;
    protected Transform playerTransform;
    protected float currentHealth;
    protected bool isDead = false;
    private float attackTimer = 0f;
    protected bool isAttackLocked = false;
    private float idleTimer = 0f;

    // Patrol
    protected Transform[] patrolPoints = System.Array.Empty<Transform>();
    protected int currentPatrolIndex = 0;

    // Leash
    private float bakedPathLength = 0f;
    protected float leashRadius = 0f;
    private Vector3 leashOrigin;

    // Speed stack
    private Dictionary<string, float> speedMultipliers = new Dictionary<string, float>();

    // ─── IEnemyDamageable ──────────────────────────────────────────────────────

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public string DisplayName => enemyDisplayName;

    public event System.Action<float, float> OnHealthChanged;
    public event System.Action<GameObject> OnDied;
    public event System.Action<string, bool> OnStatusEffectChanged;

    // ─── Unity Lifecycle ────────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
    }

    protected virtual void Start()
    {
        agent.speed = baseMoveSpeed;

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            playerTransform = playerGO.transform;
        else
            Debug.LogWarning($"[{enemyDisplayName}] No GameObject tagged 'Player' found.");

        BakePatrolPath();
        leashOrigin = transform.position;

        if (spawnEvent != null) spawnEvent.Post(gameObject);
        UpdateHealthRTPC();

        if (enableDebugLog)
            Debug.Log($"[{enemyDisplayName}] Ready. Patrol perimeter: {bakedPathLength:F1}m  Leash: {leashRadius:F1}m");
    }

    private void Update()
    {
        if (isDead) return;
        attackTimer = Mathf.Max(0f, attackTimer - Time.deltaTime);
        TickStateMachine();
        OnEnemyUpdate();
    }

    // ─── Abstract / Virtual Interface for Subclasses ───────────────────────────

    /// <summary>Called every frame while alive. Override instead of Update().</summary>
    protected abstract void OnEnemyUpdate();

    /// <summary>Implement the actual attack — called by the state machine when cooldown clears and player is in range.</summary>
    protected abstract void PerformAttack();

    /// <summary>Returns a short string ID for Wwise state routing, e.g. "Wisp" or "RockGolem".</summary>
    public abstract string GetEnemyTypeID();

    /// <summary>Override to handle per-type death logic (VFX, collectables, etc.).</summary>
    protected virtual void OnEnemyDeath() { }
    protected virtual void OnStateChanged(EnemyState prev, EnemyState next) { }

    /// <summary>
    /// Default NavMesh patrol. Wisp overrides this because it controls Y manually.
    /// </summary>
    protected virtual void AdvancePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        if (!agent.enabled || !agent.isOnNavMesh) return;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.15f)
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    // ─── Patrol Point Registration ────────────────────────────────────────────

    /// <summary>
    /// Call from subclass Awake(), BEFORE base.Start(), so BakePatrolPath() has data.
    /// </summary>
    protected void RegisterPatrolPoints(Transform[] points)
    {
        patrolPoints = points ?? System.Array.Empty<Transform>();
    }

    protected void ResetPatrolToStart()
    {
        currentPatrolIndex = 0;
    }

    // ──── Leash / Path Bake ───────────────────────────────────────────────────

    private void BakePatrolPath()
    {
        bakedPathLength = 0f;

        if (patrolPoints == null || patrolPoints.Length < 2)
        {
            leashRadius = viewRange * 1.5f;
            if (enableDebugLog)
                Debug.Log($"[{enemyDisplayName}] No patrol path — leash fallback: {leashRadius:F1}m");
            return;
        }

        NavMeshPath path = new NavMeshPath();

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;

            Vector3 from = patrolPoints[i].position;
            Vector3 to = patrolPoints[(i + 1) % patrolPoints.Length].position;

            if (NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path)
                && path.status == NavMeshPathStatus.PathComplete)
            {
                bakedPathLength += PathLength(path);
            }
            else
            {
                bakedPathLength += Vector3.Distance(from, to);
            }
        }

        leashRadius = bakedPathLength * leashRadiusMultiplier;
    }

    private static float PathLength(NavMeshPath path)
    {
        float len = 0f;
        Vector3[] corners = path.corners;
        for (int i = 1; i < corners.Length; i++)
            len += Vector3.Distance(corners[i - 1], corners[i]);
        return len;
    }

    protected Vector3 NearestPatrolPoint(Vector3 pos)
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return transform.position;

        Vector3 best = patrolPoints[0] != null ? patrolPoints[0].position : transform.position;
        float minDist = float.MaxValue;

        foreach (Transform p in patrolPoints)
        {
            if (p == null) continue;
            float d = Vector3.Distance(pos, p.position);
            if (d < minDist) { minDist = d; best = p.position; }
        }

        return best;
    }

    /// <summary>
    /// Override to adjust the Y of the return destination before the agent path is set.
    /// Wisp uses this to keep Y at hover height so the XZ-only agent steers correctly.
    /// </summary>
    protected virtual Vector3 AdjustReturnDestination(Vector3 destination) => destination;

    // ─── Range Helpers ───────────────────────────────────────────────────────

    protected bool PlayerInView()
        => playerTransform != null
        && Vector3.Distance(transform.position, playerTransform.position) <= viewRange;

    protected bool PlayerInAttackRange()
        => playerTransform != null
        && Vector3.Distance(transform.position, playerTransform.position) <= attackRange;

    protected bool WithinLeash()
        => Vector3.Distance(transform.position, leashOrigin) <= leashRadius;

    // ──── State Machine ──────────────────────────────────────────────────────

    private void TickStateMachine()
    {
        switch (_state)
        {
            case EnemyState.Patrol: TickPatrol(); break;
            case EnemyState.Chase: TickChase(); break;
            case EnemyState.Attack: TickAttack(); break;
            case EnemyState.Return: TickReturn(); break;
            case EnemyState.Idle: TickIdle(); break;
        }
    }

    protected void SetState(EnemyState next)
    {
        if (next == _state) return;
        EnemyState prev = _state;
        _state = next;

        if (prev == EnemyState.Attack && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;

        if (next == EnemyState.Return)
        {
            returnDestination = AdjustReturnDestination(NearestPatrolPoint(transform.position));
            if (agent.enabled && agent.isOnNavMesh)
                agent.SetDestination(returnDestination);
        }

        if (next == EnemyState.Idle)
            idleTimer = idleDuration;

        if (prev == EnemyState.Idle && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;

        OnStateChanged(prev, next);

        if (enableDebugLog)
            Debug.Log($"[{enemyDisplayName}] {prev} → {next}");
    }

    private void TickPatrol()
    {
        // Leash origin tracks nearest patrol point while at peace
        leashOrigin = NearestPatrolPoint(transform.position);
        if (!agent.enabled || !agent.isOnNavMesh) return;

        agent.speed = baseMoveSpeed;
        agent.isStopped = false;

        AdvancePatrol();

        if (PlayerInView())
            SetState(EnemyState.Chase);
    }

    private void TickChase()
    {
        if (playerTransform == null) { SetState(EnemyState.Return); return; }
        if (!WithinLeash() || !PlayerInView()) { SetState(EnemyState.Return); return; }
        if (PlayerInAttackRange()) { SetState(EnemyState.Attack); return; }

        if (!agent.enabled || !agent.isOnNavMesh) return;

        agent.speed = baseMoveSpeed * chaseSpeedBoost;
        agent.isStopped = false;
        if (agent.enabled && agent.isOnNavMesh)
            agent.SetDestination(playerTransform.position);
    }

    private void TickAttack()
    {
        // Stop the agent while attacking — no sliding into the player
        if (agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;

        // While an attack animation is playing, don't evaluate transitions
        if (isAttackLocked) return;

        if (playerTransform == null) { SetState(EnemyState.Return); return; }
        if (!WithinLeash() || !PlayerInView()) { SetState(EnemyState.Return); return; }
        if (!PlayerInAttackRange()) { SetState(EnemyState.Chase); return; }

        // Face player
        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(toPlayer), Time.deltaTime * 8f);

        if (attackTimer <= 0f)
        {
            attackTimer = attackCooldown;
            PerformAttack();
        }
    }

    private Vector3 returnDestination;

    private bool returnDestinationSet = false;

    private void TickReturn()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;

        if (!returnDestinationSet)
        {
            agent.speed = baseMoveSpeed;
            agent.isStopped = false;
            agent.SetDestination(new Vector3(
                returnDestination.x,
                transform.position.y,
                returnDestination.z));
            returnDestinationSet = true;
            return;
        }

        if (agent.pathPending) return;

        Vector2 currentXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 destinationXZ = new Vector2(returnDestination.x, returnDestination.z);

        if (Vector2.Distance(currentXZ, destinationXZ) <= agent.stoppingDistance + 0.35f)
        {
            returnDestinationSet = false;
            leashOrigin = returnDestination;
            SetState(EnemyState.Idle);
        }
    }

    private void TickIdle()
    {
        if (agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;

        if (PlayerInView()) { SetState(EnemyState.Chase); return; }
        if (PlayerInAttackRange()) { SetState(EnemyState.Attack); return; }

        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
            SetState(EnemyState.Patrol);
    }

    // ──── Damage / Health ─────────────────────────────────────────────────

    /// <summary>Apply damage. Returns true if this hit killed the enemy.</summary>
    public bool TakeDamage(float amount, string sourceEffect = "")
    {
        if (isDead) return false;

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (enableDebugLog)
            Debug.Log($"[{enemyDisplayName}] -{amount:F1} from '{sourceEffect}'. HP {currentHealth:F1}/{maxHealth}");

        UpdateHealthRTPC();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f) { Die(); return true; }
        return false;
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthRTPC();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // ─── Speed Multiplier Stack ─────────────────────────────────────────────────

    /// <summary>
    /// Apply a named speed multiplier (0 = stopped, 1 = full speed).
    /// Multiple effects compose multiplicatively.
    /// </summary>
    public void SetSpeedMultiplier(string effectName, float multiplier)
    {
        speedMultipliers[effectName] = Mathf.Clamp01(multiplier);
        ApplyCompositeSpeed();
    }

    /// <summary>Remove a named speed multiplier (restores its contribution to 1).</summary>
    public void ClearSpeedMultiplier(string effectName)
    {
        if (speedMultipliers.Remove(effectName))
            ApplyCompositeSpeed();
    }

    private void ApplyCompositeSpeed()
    {
        float composite = 1f;
        foreach (var kv in speedMultipliers) composite *= kv.Value;

        float baseForState = (_state == EnemyState.Chase)
            ? baseMoveSpeed * chaseSpeedBoost
            : baseMoveSpeed;

        if (agent != null) agent.speed = baseForState * composite;
    }

    // ─── Status Effect Notification ────────────────────────────────────────────

    /// <summary>Broadcast a status effect change to any subscribed UI.</summary>
    protected void NotifyStatusEffect(string effectName, bool isActive)
        => OnStatusEffectChanged?.Invoke(effectName, isActive);

    // ─── Death ─────────────────────────────────────────────────────────────────

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        if (agent.enabled || agent.isOnNavMesh)
            agent.isStopped = true;
        agent.enabled = false;
        if (deathEvent != null) deathEvent.Post(gameObject);
        OnDied?.Invoke(gameObject);
        OnEnemyDeath();
        if (enableDebugLog) Debug.Log($"[{enemyDisplayName}] Died.");
    }

    private void UpdateHealthRTPC()

    {
        if (healthPercentRTPC != null)
            healthPercentRTPC.SetValue(gameObject, (currentHealth / maxHealth) * 100f);
    }

    // ─── Gizmos ────────────────────────────────────────────────────────────────

    protected virtual void OnDrawGizmos()
    {
        // View range
        Gizmos.color = new Color(1f, 1f, 1f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, viewRange);

        // Attack range
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Leash
        if (showLeashGizmo && Application.isPlaying)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(leashOrigin, leashRadius);
        }

        // State colour dot
        if (!Application.isPlaying) return;
        Gizmos.color = _state switch
        {
            EnemyState.Patrol => Color.green,
            EnemyState.Chase => Color.yellow,
            EnemyState.Attack => Color.red,
            EnemyState.Return => Color.cyan,
            _ => Color.white
        };
        Gizmos.DrawSphere(transform.position + Vector3.up * 2.2f, 0.18f);
    }
}