using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float invincibilityTime = 0.8f;

    [Header("Lives")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private int maxLives = 5;

    [Header("Respawn")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float respawnInvincibilityTime = 2f;

    [Header("Wwise")]
    [SerializeField] private AK.Wwise.Event takeDamageEvent;
    [SerializeField] private AK.Wwise.Event deathEvent;
    [SerializeField] private AK.Wwise.Event respawnEvent;
    [SerializeField] private AK.Wwise.Event collectEvent;
    [SerializeField] private AK.Wwise.RTPC healthPercentRTPC;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = false;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public int Lives => currentLives;
    public int MaxLives => maxLives;
    public int Collectables => collectableCount;
    public bool IsInvincible => invincibleTimer > 0f;
    public bool IsAlive => currentLives > 0 || currentHealth > 0f;

    public event System.Action<float, float> OnHealthChanged;
    public event System.Action<int> OnLifeLost;
    public event System.Action<int> OnLifeGained;
    public event System.Action OnGameOver;
    public event System.Action<int> OnCollectableChanged;
    public event System.Action<bool> OnInvincibilityChanged;

    private float currentHealth;
    private int currentLives;
    private int collectableCount = 0;
    private float invincibleTimer = 0f;
    private Vector3 spawnPosition;
    private PlayerDropTracker dropTracker;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentLives = startingLives;
        spawnPosition = transform.position;
        dropTracker = GetComponent<PlayerDropTracker>();
    }

    private void Start()
    {
        UpdateHealthRTPC();
    }

    private void Update()
    {
        if (invincibleTimer <= 0f) return;

        invincibleTimer -= Time.deltaTime;
        if (invincibleTimer <= 0f)
        {
            invincibleTimer = 0f;
            if (OnInvincibilityChanged != null) OnInvincibilityChanged.Invoke(false);
        }
    }

    public void TakeDamage(float amount)
    {
        if (IsInvincible) return;
        if (currentLives <= 0 && currentHealth <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        if (takeDamageEvent != null)
            takeDamageEvent.Post(gameObject);
        UpdateHealthRTPC();
        if (OnHealthChanged != null)
            OnHealthChanged.Invoke(currentHealth, maxHealth);

        if (enableDebugLog)
            Debug.Log($"[PlayerHealth] -{amount:F1}  HP: {currentHealth:F1}/{maxHealth}");

        if (currentHealth <= 0f)
            HandleDeath();
        else
            GrantInvincibility(invincibilityTime);
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthRTPC();
        if (OnHealthChanged != null) OnHealthChanged.Invoke(currentHealth, maxHealth);
    }

    public void AddCollectable(int amount = 1)
    {
        collectableCount += amount;
        if (collectEvent != null) collectEvent.Post(gameObject);
        if (OnCollectableChanged != null) OnCollectableChanged.Invoke(collectableCount);

        if (enableDebugLog)
            Debug.Log($"[PlayerHealth] Collectables: {collectableCount}");
    }

    public void AddLife(int amount = 1)
    {
        currentLives = Mathf.Min(maxLives, currentLives + amount);
        if (OnLifeGained != null) OnLifeGained.Invoke(currentLives);
    }

    public void GrantInvincibility(float duration)
    {
        bool wasInvincible = IsInvincible;
        invincibleTimer = Mathf.Max(invincibleTimer, duration);
        if (!wasInvincible && OnInvincibilityChanged != null) OnInvincibilityChanged.Invoke(true);
    }

    private void HandleDeath()
    {
        currentLives--;

        if (currentLives <= 0)
        {
            currentLives = 0;
            if (deathEvent != null)
                deathEvent.Post(gameObject);
            if (OnLifeLost != null)
                OnLifeLost.Invoke(0);
            if (OnGameOver != null)
                OnGameOver.Invoke();
        }
        else
        {
            if (deathEvent != null)
                deathEvent.Post(gameObject);
            if (OnLifeLost != null)
                OnLifeLost.Invoke(currentLives);
            Respawn();
        }
    }

    private void Respawn()
    {
        currentHealth = maxHealth;
        UpdateHealthRTPC();
        if (OnHealthChanged != null)
            OnHealthChanged.Invoke(currentHealth, maxHealth);

        // Collectables kept on respawn intentionally.
        // When enemy respawning is added, revisit:
        // collectableCount = 0; OnCollectableChanged?.Invoke(0); // C# delegate — ?. is fine here

        Vector3 pos = spawnPosition;
        if (respawnPoint != null) pos = respawnPoint.position;
        if (dropTracker != null && dropTracker.LastDropPoint != null) pos = dropTracker.LastDropPoint.position;
        transform.position = pos;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        if (respawnEvent != null)
            respawnEvent.Post(gameObject);
        GrantInvincibility(respawnInvincibilityTime);

        if (enableDebugLog)
            Debug.Log($"[PlayerHealth] Respawned. Lives: {currentLives}");
    }

    private void UpdateHealthRTPC()
    {
        if (healthPercentRTPC != null)
            healthPercentRTPC.SetValue(gameObject, (currentHealth / maxHealth) * 100f);
    }
}