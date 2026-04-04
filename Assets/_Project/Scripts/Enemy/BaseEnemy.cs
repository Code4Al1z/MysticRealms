using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;

/// <summary>
/// Base class for all Mystic Realms enemies.
/// Handles: health, death, damage routing, UI-readiness hooks, and shared Wwise plumbing.
///
/// UI READINESS:
///   - Exposes events for HealthChanged, Died, and StatusEffectChanged.
///   - Exposes IEnemyDamageable interface for HUD/healthbar integration.
///   - When you add a HUD canvas later, subscribe to these events from an EnemyHealthBarUI component.
///
/// INHERITANCE CONTRACT:
///   - Override OnEnemyDeath() for per-type death behaviour (particles, collectables, etc.)
///   - Override OnEnemyUpdate() called every Update() — avoid overriding Update() directly.
///   - Override GetEnemyTypeID() to return a unique string for Wwise state routing.
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

    protected float currentHealth;
    protected bool isDead = false;
    protected NavMeshAgent agent;

    // Speed multiplier stack – applied multiplicatively so effects compose cleanly.
    // Keys: effect name (e.g. "echo_pulse", "resonance_hum"), Value: 0–1 multiplier.
    private Dictionary<string, float> speedMultipliers = new Dictionary<string, float>();

    // ─── UI / Damage Events ────────────────────────────────────────────────────

    /// <summary>Fired whenever health changes. (currentHealth, maxHealth)</summary>
    public event Action<float, float> OnHealthChanged;

    /// <summary>Fired on death. Payload: the enemy GameObject.</summary>
    public event Action<GameObject> OnDied;

    /// <summary>
    /// Fired when a named status effect is applied or removed.
    /// (effectName, isActive) — useful for UI status icons.
    /// </summary>
    public event Action<string, bool> OnStatusEffectChanged;

    // ─── IEnemyDamageable ──────────────────────────────────────────────────────

    public float CurrentHealth => currentHealth;
    public float MaxHealth     => maxHealth;
    public bool  IsDead        => isDead;
    public string DisplayName  => enemyDisplayName;

    // ─── Unity Lifecycle ────────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
    }

    protected virtual void Start()
    {
        agent.speed = baseMoveSpeed;

        if (spawnEvent != null)
            spawnEvent.Post(gameObject);

        UpdateHealthRTPC();

        if (enableDebugLog)
            Debug.Log($"[{enemyDisplayName}] Spawned with {maxHealth} HP.");
    }

    private void Update()
    {
        if (isDead) return;
        OnEnemyUpdate();
    }

    // ─── Abstract / Virtual Interface for Subclasses ───────────────────────────

    /// <summary>Called every frame while alive. Override instead of Update().</summary>
    protected abstract void OnEnemyUpdate();

    /// <summary>Override to handle per-type death logic (VFX, collectables, etc.).</summary>
    protected virtual void OnEnemyDeath() { }

    /// <summary>Returns a short string ID for Wwise state routing, e.g. "Wisp" or "RockGolem".</summary>
    public abstract string GetEnemyTypeID();

    // ─── Damage / Health ───────────────────────────────────────────────────────

    /// <summary>Apply damage. Returns true if this hit killed the enemy.</summary>
    public bool TakeDamage(float amount, string sourceEffect = "")
    {
        if (isDead) return false;

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (enableDebugLog)
            Debug.Log($"[{enemyDisplayName}] Took {amount:F1} dmg from '{sourceEffect}'. HP: {currentHealth:F1}/{maxHealth}");

        UpdateHealthRTPC();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
            return true;
        }

        return false;
    }

    /// <summary>Heal the enemy (clamps to maxHealth).</summary>
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
        foreach (var kv in speedMultipliers)
            composite *= kv.Value;

        if (agent != null)
            agent.speed = baseMoveSpeed * composite;
    }

    // ─── Status Effect Notification ────────────────────────────────────────────

    /// <summary>Broadcast a status effect change to any subscribed UI.</summary>
    protected void NotifyStatusEffect(string effectName, bool isActive)
    {
        OnStatusEffectChanged?.Invoke(effectName, isActive);
    }

    // ─── Death ─────────────────────────────────────────────────────────────────

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        agent.isStopped = true;
        agent.enabled = false;

        if (deathEvent != null)
            deathEvent.Post(gameObject);

        OnDied?.Invoke(gameObject);
        OnEnemyDeath();

        if (enableDebugLog)
            Debug.Log($"[{enemyDisplayName}] Died.");
    }

    // ─── Wwise Helpers ─────────────────────────────────────────────────────────

    private void UpdateHealthRTPC()
    {
        if (healthPercentRTPC != null)
            healthPercentRTPC.SetValue(gameObject, (currentHealth / maxHealth) * 100f);
    }

    // ─── Gizmos ────────────────────────────────────────────────────────────────

    protected virtual void OnDrawGizmos()
    {
        if (!enableDebugLog) return;
        Gizmos.color = isDead ? Color.black : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
