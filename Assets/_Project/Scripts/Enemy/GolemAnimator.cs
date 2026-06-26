using UnityEngine;

public class GolemAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Tooltip("Fallback attack duration used when no animator is assigned.")]
    [SerializeField] private float fallbackAttackDuration = 1.2f;

    private static readonly int StateHash = Animator.StringToHash("State");

    private enum State { Idle = 0, Walk = 1, Attack = 2, Damage = 3, Dead = 4 }

    private int currentState = -1;

    public bool IsAttacking => currentState == (int)State.Attack;

    private void SetState(int state)
    {
        if (state == currentState) return;
        currentState = state;
        animator.SetInteger(StateHash, state);
    }

    public void SetIdle() => SetState((int)State.Idle);
    public void SetWalk() => SetState((int)State.Walk);
    public void SetAttack() => SetState((int)State.Attack);
    public void SetDamage() => SetState((int)State.Damage);
    public void SetDead() => SetState((int)State.Dead);

    public float AttackClipLength()
    {
        if (animator == null) return fallbackAttackDuration;

        RuntimeAnimatorController rac = animator.runtimeAnimatorController;
        if (rac == null) return fallbackAttackDuration;

        foreach (AnimationClip clip in rac.animationClips)
        {
            if (clip.name.ToLower().Contains("attack"))
                return clip.length;
        }

        return fallbackAttackDuration;
    }
}