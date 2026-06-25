using UnityEngine;

public class GolemAttackHitbox : MonoBehaviour
{
    private float _damage;
    private bool _active;
    private RockGolemEnemy _owner;

    public void Activate(float damage, RockGolemEnemy owner)
    {
        _damage = damage;
        _owner = owner;
        _active = true;
    }

    public void Deactivate() => _active = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!_active) return;

        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph == null) return;

        Debug.Log($"[GolemAttackHitbox] Hit {other.name} for {_damage}");
        ph.TakeDamage(_damage);
        Deactivate(); // one hit per swing
    }
}