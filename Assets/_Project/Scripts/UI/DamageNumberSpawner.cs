using UnityEngine;
using System.Collections.Generic;

public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner Instance { get; private set; }

    [SerializeField] private DamageNumber prefab;
    [SerializeField] private int poolSize = 20;

    [Header("Enemy hits")]
    [SerializeField] private Color enemyHitColor = new Color(1f, 0.92f, 0.2f);
    [SerializeField] private Color enemyHeavyColor = new Color(1f, 0.4f, 0.1f);
    [SerializeField] private float enemyHeavyThreshold = 20f;
    [SerializeField] private float enemyVerticalOffset = 1.5f;
    [SerializeField] private float enemyHorizontalSpread = 0.4f;

    [Header("Player hits")]
    [SerializeField] private Color playerHitColor = new Color(0.95f, 0.2f, 0.2f);
    [SerializeField] private float playerVerticalOffset = 2.2f;
    [SerializeField] private float playerHorizontalSpread = 0.6f;

    [Header("Drain throttling")]
    [Tooltip("Minimum seconds between damage numbers on the same target. " +
             "Prevents per-frame drain (e.g. Resonance Hum) from flooding the pool.")]
    [SerializeField] private float drainNumberInterval = 0.3f;

    private Queue<DamageNumber> pool = new Queue<DamageNumber>();
    private Dictionary<int, float> lastSpawnTime = new Dictionary<int, float>();
    private Dictionary<int, float> accumulated = new Dictionary<int, float>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        for (int i = 0; i < poolSize; i++)
        {
            DamageNumber dn = Instantiate(prefab, transform);
            dn.Initialise(this);
            dn.gameObject.SetActive(false);
            pool.Enqueue(dn);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Called by enemies. Accumulates damage from per-frame drain sources and
    /// shows one number per drainNumberInterval rather than one per frame.
    /// </summary>
    public void SpawnEnemyHit(float amount, Vector3 worldPosition, int sourceID)
    {
        // Accumulate damage for this source
        if (!accumulated.ContainsKey(sourceID)) accumulated[sourceID] = 0f;
        accumulated[sourceID] += amount;

        // Check throttle
        float now = Time.time;
        if (lastSpawnTime.ContainsKey(sourceID) &&
            now - lastSpawnTime[sourceID] < drainNumberInterval)
            return;

        float total = accumulated[sourceID];
        accumulated[sourceID] = 0f;
        lastSpawnTime[sourceID] = now;

        float xOffset = Random.Range(-enemyHorizontalSpread, enemyHorizontalSpread);
        Vector3 pos = worldPosition + new Vector3(xOffset, enemyVerticalOffset, 0f);
        Color color = total >= enemyHeavyThreshold ? enemyHeavyColor : enemyHitColor;

        Spawn(total, pos, color);
    }

    /// <summary>
    /// Called by the player bridge. Player hits are always discrete so no throttle needed.
    /// </summary>
    public void SpawnPlayerHit(float amount, Vector3 worldPosition)
    {
        float xOffset = Random.Range(-playerHorizontalSpread, playerHorizontalSpread);
        Vector3 pos = worldPosition + new Vector3(xOffset, playerVerticalOffset, 0f);
        Spawn(amount, pos, playerHitColor);
    }

    public void ReturnToPool(DamageNumber dn)
    {
        pool.Enqueue(dn);
    }

    private void Spawn(float amount, Vector3 position, Color color)
    {
        if (prefab == null) return;

        DamageNumber dn = pool.Count > 0
            ? pool.Dequeue()
            : Instantiate(prefab, transform);

        dn.Initialise(this);
        dn.Show(amount, position, color);
    }
}