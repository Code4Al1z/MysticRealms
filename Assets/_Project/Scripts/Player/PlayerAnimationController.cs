using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    public enum PlayerAnimState
    {
        Idle = 0,
        Run = 1,
        Jump = 2,
        Cast = 3,
        Dead = 4
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rigidBody;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerAbilities playerAbilities;

    [Header("Animator Parameters")]
    [SerializeField] private string stateParam = "State";

    [Header("Settings")]
    [SerializeField] private float runThreshold = 0.1f;

    private int stateHash;

    private PlayerAnimState currentState;

    private void Awake()
    {
        stateHash = Animator.StringToHash(stateParam);
    }

    private void Update()
    {
        if (animator == null || rigidBody == null || playerController == null)
            return;

        float speed = new Vector3(rigidBody.linearVelocity.x, 0f, rigidBody.linearVelocity.z).magnitude;
        bool isGrounded = playerController.IsGrounded();

        PlayerAnimState newState = ResolveState(speed, isGrounded);

        if (newState != currentState)
        {
            currentState = newState;
            animator.SetInteger(stateHash, (int)currentState);
        }
    }

    private PlayerAnimState ResolveState(float speed, bool isGrounded)
    {
        // Change later once we have player health implemented
        if (currentState == PlayerAnimState.Dead)
            return PlayerAnimState.Dead;

        if (playerAbilities != null && (playerAbilities.IsEchoPulseActive() || playerAbilities.IsResonanceActive()))
            return PlayerAnimState.Cast;

        // Air has priority
        if (!isGrounded)
            return PlayerAnimState.Jump;

        // Ground states
        if (speed > runThreshold)
            return PlayerAnimState.Run;

        return PlayerAnimState.Idle;
    }

    // --- External triggers (called from other systems) ---

    public void TriggerJump()
    {
        SetState(PlayerAnimState.Jump);
    }

    public void TriggerCast()
    {
        SetState(PlayerAnimState.Cast);
    }

    public void SetDead(bool dead)
    {
        if (dead)
            SetState(PlayerAnimState.Dead);
    }

    private void SetState(PlayerAnimState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        animator.SetInteger(stateHash, (int)newState);
    }
}