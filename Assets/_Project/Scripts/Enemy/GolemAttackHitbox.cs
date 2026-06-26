using UnityEngine;

public class GolemAttackHitbox : MonoBehaviour
{
    private float _damage;
    private GolemAnimator _golemAnimator;

    public void Initialise(float damage, GolemAnimator golemAnimator)
    {
        _damage = damage;
        _golemAnimator = golemAnimator;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_golemAnimator == null || !_golemAnimator.IsAttacking) return;

        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph == null) return;

        Debug.Log($"[GolemAttackHitbox] Hit {other.name} for {_damage}");
        ph.TakeDamage(_damage);
    }
}