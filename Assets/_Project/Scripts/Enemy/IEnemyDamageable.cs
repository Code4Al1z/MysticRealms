/// <summary>
/// Interface implemented by all damageable enemies.
///
/// PURPOSE — UI & SYSTEM DECOUPLING:
///   Any future HealthBar, damage number display, or targeting system
///   can reference IEnemyDamageable without knowing the concrete enemy type.
///
/// USAGE EXAMPLE (future EnemyHealthBarUI.cs):
///   void Start() {
///       IEnemyDamageable target = enemy.GetComponent<IEnemyDamageable>();
///       target.OnHealthChanged += UpdateBar;
///       target.OnDied += HideBar;
///   }
///
/// PLAYER HEALTH:
///   When PlayerHealth.cs is added, implement this same interface on the player
///   so the HUD can use one unified pattern for both enemy and player bars.
/// </summary>
public interface IEnemyDamageable
{
    float CurrentHealth { get; }
    float MaxHealth     { get; }
    bool  IsDead        { get; }

    /// <summary>Human-readable name for HUD display.</summary>
    string DisplayName  { get; }

    /// <summary>
    /// Apply damage. Returns true if the hit was lethal.
    /// sourceEffect: optional tag for damage source (used in debug + future damage-type UI).
    /// </summary>
    bool TakeDamage(float amount, string sourceEffect = "");

    // ── Events (implemented on the concrete class via BaseEnemy) ──────────────

    /// <summary>Fired on every health change. (currentHealth, maxHealth)</summary>
    event System.Action<float, float> OnHealthChanged;

    /// <summary>Fired when the enemy dies. Payload: the enemy GameObject.</summary>
    event System.Action<UnityEngine.GameObject> OnDied;

    /// <summary>
    /// Fired when a named status effect is toggled.
    /// Useful for future UI status icons (e.g. "Slowed", "Stunned").
    /// </summary>
    event System.Action<string, bool> OnStatusEffectChanged;
}
