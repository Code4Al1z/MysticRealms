using UnityEngine;
using System.Collections.Generic;

public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner Instance { get; private set; }

    [SerializeField] private DamageNumber prefab;
    [SerializeField] private int          poolSize = 16;

    [Header("Enemy hits")]
    [SerializeField] private Color  enemyHitColor   = new Color(1f, 0.92f, 0.2f);
    [SerializeField] private Color  enemyHeavyColor = new Color(1f, 0.4f, 0.1f);
    [SerializeField] private float  enemyHeavyThreshold = 20f;
    [SerializeField] private float  enemyVerticalOffset  = 1.5f;

    [Header("Player hits")]
    [SerializeField] private Color  playerHitColor  = new Color(0.95f, 0.2f, 0.2f);
    [SerializeField] private float  playerVerticalOffset  = 2.2f;
    [Tooltip("Random horizontal spread applied to player damage numbers.")]
    [SerializeField] private float  playerHorizontalSpread = 0.6f;

    private Queue<DamageNumber> pool = new Queue<DamageNumber>();

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

    public void SpawnEnemyHit(float amount, Vector3 worldPosition)
    {
        Color color = amount >= enemyHeavyThreshold ? enemyHeavyColor : enemyHitColor;
        Spawn(amount, worldPosition + Vector3.up * enemyVerticalOffset, color);
    }

    public void SpawnPlayerHit(float amount, Vector3 worldPosition)
    {
        // Small random offset in X for a scattered, Trine-like feel
        float xOffset = Random.Range(-playerHorizontalSpread, playerHorizontalSpread);
        Vector3 spawnPos = worldPosition + new Vector3(xOffset, playerVerticalOffset, 0f);
        Spawn(amount, spawnPos, playerHitColor);
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
