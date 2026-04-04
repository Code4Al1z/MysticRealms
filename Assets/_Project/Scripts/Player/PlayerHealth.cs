using UnityEngine;

/// <summary>
/// PlayerHealth — manages health, lives, and collectables for Mystic Realms.
///
/// ── RESPONSIBILITIES ────────────────────────────────────────────────────────
///   • Health pool with damage, heal, invincibility frames.
///   • Lives system — on death: lose a life, respawn OR game over.
///   • Collectable counter (wisp-body pickups, crystals, etc.).
///   • All state changes fire events — the HUD subscribes, nothing else couples.
///
/// ── WWISE SETUP ─────────────────────────────────────────────────────────────
///   Player_TakeDamage   one-shot on damage
///   Player_Death        on last-life death
///   Player_Respawn      on respawn
///   Player_Collect      on collectable pickup
///   Player_HealthPct    RTPC 0–100
///
/// ── HUD INTEGRATION ─────────────────────────────────────────────────────────
///   GameHUD subscribes to all public events in Start().
///   No direct HUD reference needed here — fully decoupled.
///
/// ── FUTURE: MENUS / GAME OVER ───────────────────────────────────────────────
///   OnGameOver fires when lives hit 0. A future GameManager or MainMenu
///   controller subscribes and transitions to the game-over / main-menu scene.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────── Inspector ───

    [Header("Health")]
    [SerializeField] private float maxHealth          = 100f;
    [SerializeField] private float invincibilityTime  = 0.8f; // seconds after a hit

    [Header("Lives")]
    [SerializeField] private int   startingLives      = 3;
    [SerializeField] private int   maxLives           = 5;

    [Header("Respawn")]
    [Tooltip("Transform to warp the player to on respawn. If null, uses spawn position.")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private PlayerDropTracker dropTracker;

    [Tooltip("Seconds of invincibility granted on respawn.")]
    [SerializeField] private float respawnInvincibilityTime = 2f;

    [Header("Wwise")]
    [SerializeField] private AK.Wwise.Event takeDamageEvent;
    [SerializeField] private AK.Wwise.Event deathEvent;
    [SerializeField] private AK.Wwise.Event respawnEvent;
    [SerializeField] private AK.Wwise.Event collectEvent;
    [SerializeField] private AK.Wwise.RTPC  healthPercentRTPC;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = false;

    // ──────────────────────────────────────────────────────────────── Events ─

    /// <summary>Health changed. (currentHealth, maxHealth)</summary>
    public event System.Action<float, float> OnHealthChanged;

    /// <summary>Player lost a life and is respawning. (livesRemaining)</summary>
    public event System.Action<int>          OnLifeLost;

    /// <summary>Gained a life (e.g. from a pickup). (livesRemaining)</summary>
    public event System.Action<int>          OnLifeGained;

    /// <summary>Lives hit 0 — game over. Subscribe in GameManager/MenuController.</summary>
    public event System.Action               OnGameOver;

    /// <summary>Collectable count changed. (newTotal)</summary>
    public event System.Action<int>          OnCollectableChanged;

    /// <summary>Invincibility state toggled. Useful for HUD flash effect. (isInvincible)</summary>
    public event System.Action<bool>         OnInvincibilityChanged;

    // ─────────────────────────────────────────────────────────── Properties ─

    public float CurrentHealth    => currentHealth;
    public float MaxHealth        => maxHealth;
    public int   Lives            => currentLives;
    public int   MaxLives         => maxLives;
    public int   Collectables     => collectableCount;
    public bool  IsInvincible     => invincibleTimer > 0f;
    public bool  IsAlive          => currentLives > 0 || currentHealth > 0f;

    // ──────────────────────────────────────────────────────── Runtime State ─

    private float currentHealth;
    private int   currentLives;
    private int   collectableCount = 0;
    private float invincibleTimer  = 0f;
    private Vector3 spawnPosition;

    // ────────────────────────────────────────────────────────── Lifecycle ────

    private void Awake()
    {
        currentHealth = maxHealth;
        currentLives  = startingLives;
        spawnPosition = transform.position;
    }

    private void Start()
    {
        UpdateHealthRTPC();

        if (enableDebugLog)
            Debug.Log($"[PlayerHealth] Ready. HP: {maxHealth}  Lives: {startingLives}");
    }

    private void Update()
    {
        if (invincibleTimer > 0f)
        {
            invincibleTimer -= Time.deltaTime;

            if (invincibleTimer <= 0f)
            {
                invincibleTimer = 0f;
                OnInvincibilityChanged?.Invoke(false);
            }
        }
    }

    // ──────────────────────────────────────────────────────── Public API ─────

    /// <summary>Apply damage. Respects invincibility frames.</summary>
    public void TakeDamage(float amount)
    {
        if (IsInvincible) return;
        if (currentLives <= 0 && currentHealth <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        takeDamageEvent?.Post(gameObject);
        UpdateHealthRTPC();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (enableDebugLog)
            Debug.Log($"[PlayerHealth] -{amount:F1}  HP: {currentHealth:F1}/{maxHealth}");

        if (currentHealth <= 0f)
            HandleDeath();
        else
            GrantInvincibility(invincibilityTime);
    }

    /// <summary>Heal the player (clamps to maxHealth).</summary>
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthRTPC();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>Award collectable points (wisp pickups, crystals, etc.).</summary>
    public void AddCollectable(int amount = 1)
    {
        collectableCount += amount;
        collectEvent?.Post(gameObject);
        OnCollectableChanged?.Invoke(collectableCount);

        if (enableDebugLog)
            Debug.Log($"[PlayerHealth] +{amount} collectable. Total: {collectableCount}");
    }

    /// <summary>Add a life (e.g. from a life-pickup).</summary>
    public void AddLife(int amount = 1)
    {
        currentLives = Mathf.Min(maxLives, currentLives + amount);
        OnLifeGained?.Invoke(currentLives);
    }

    /// <summary>Grant invincibility for a given duration (stacks — takes max).</summary>
    public void GrantInvincibility(float duration)
    {
        bool wasInvincible = IsInvincible;
        invincibleTimer = Mathf.Max(invincibleTimer, duration);
        if (!wasInvincible) OnInvincibilityChanged?.Invoke(true);
    }

    // ─────────────────────────────────────────────────────── Death / Respawn ──

    private void HandleDeath()
    {
        currentLives--;

        if (enableDebugLog)
            Debug.Log($"[PlayerHealth] Died. Lives remaining: {currentLives}");

        if (currentLives <= 0)
        {
            currentLives = 0;
            deathEvent?.Post(gameObject);
            OnLifeLost?.Invoke(0);
            OnGameOver?.Invoke();

            if (enableDebugLog)
                Debug.Log("[PlayerHealth] GAME OVER.");
        }
        else
        {
            deathEvent?.Post(gameObject);
            OnLifeLost?.Invoke(currentLives);
            Respawn();
        }
    }

    private void Respawn()
    {
        currentHealth = maxHealth;
        UpdateHealthRTPC();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (dropTracker.LastDropPoint != null)
            respawnPoint = dropTracker.LastDropPoint;

        Vector3 respawnPos = respawnPoint != null ? respawnPoint.position : spawnPosition;
        transform.position = respawnPos;

        // Optionally reset velocity if Rigidbody present
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        respawnEvent?.Post(gameObject);
        GrantInvincibility(respawnInvincibilityTime);

        if (enableDebugLog)
            Debug.Log($"[PlayerHealth] Respawned at {respawnPos}. Lives: {currentLives}");
    }

    // ───────────────────────────────────────────────────────── Wwise Helper ──

    private void UpdateHealthRTPC()
        => healthPercentRTPC?.SetValue(gameObject, (currentHealth / maxHealth) * 100f);
}
