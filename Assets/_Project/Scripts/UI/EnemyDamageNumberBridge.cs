using UnityEngine;

[RequireComponent(typeof(BaseEnemy))]
public class EnemyDamageNumberBridge : MonoBehaviour
{
    [SerializeField] private float spawnHeightOffset = 2f;

    private IEnemyDamageable damageable;
    private float previousHealth;
    private int sourceID;

    private void Awake()
    {
        damageable = GetComponent<IEnemyDamageable>();
        previousHealth = damageable != null ? damageable.CurrentHealth : 0f;
        sourceID = gameObject.GetInstanceID();
    }

    private void OnEnable()
    {
        if (damageable != null) damageable.OnHealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (damageable != null) damageable.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float current, float max)
    {
        float delta = previousHealth - current;
        previousHealth = current;

        if (delta <= 0f || DamageNumberSpawner.Instance == null) return;

        Vector3 spawnPos = transform.position + Vector3.up * spawnHeightOffset;
        DamageNumberSpawner.Instance.SpawnEnemyHit(delta, spawnPos, sourceID);
    }
}