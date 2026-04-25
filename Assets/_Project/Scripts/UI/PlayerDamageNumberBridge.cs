using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerDamageNumberBridge : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private float        previousHealth;

    private void Awake()
    {
        playerHealth   = GetComponent<PlayerHealth>();
        previousHealth = playerHealth != null ? playerHealth.CurrentHealth : 0f;
    }

    private void OnEnable()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float current, float max)
    {
        float delta    = previousHealth - current;
        previousHealth = current;

        if (delta <= 0f || DamageNumberSpawner.Instance == null) return;

        DamageNumberSpawner.Instance.SpawnPlayerHit(delta, transform.position);
    }
}
