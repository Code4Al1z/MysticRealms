using UnityEngine;

/// <summary>
/// Handles player movement, jumping, and footsteps for 2.5D platformer
/// Unity 6.3 + Wwise 2025.1.4 compatible
/// Mystic Realms - Grounded Character Controller with Footsteps
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float maxSpeed = 10f;
    public float groundDrag = 6f;
    public float airDrag = 2f;

    [Header("Jump")]
    public float jumpForce = 8f;
    public float jumpCooldown = 0.5f;
    private float lastJumpTime = -999f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer;

    [Header("Footstep Timing")]
    [Tooltip("Seconds between footsteps when walking (slow speed)")]
    public float walkFootstepInterval = 0.5f;

    [Tooltip("Seconds between footsteps when running (fast speed)")]
    public float runFootstepInterval = 0.3f;

    [Tooltip("Speed threshold - above this value, use run interval")]
    public float runSpeedThreshold = 6f;

    [Tooltip("Minimum speed to trigger footsteps (prevents footsteps when barely moving)")]
    public float minSpeedForFootsteps = 0.5f;

    [Header("Wwise Audio")]
    [Tooltip("Optional: Generic jump event (use if not using surface-specific jumps)")]
    public AK.Wwise.Event jumpEvent;

    [Tooltip("Optional: Generic land event (use if not using surface-specific lands)")]
    public AK.Wwise.Event landEvent;

    [Tooltip("Optional: RTPC to control audio based on player speed")]
    public AK.Wwise.RTPC playerSpeedRTPC;

    [Header("Surface Audio System")]
    [Tooltip("Reference to SurfaceAudioManager - handles surface-specific sounds")]
    public SurfaceAudioManager surfaceAudioManager;

    private Rigidbody rb;
    private Animator animator;
    private bool isGrounded;
    private bool wasGrounded;
    private float horizontalInput;
    private float verticalInput;
    private bool jumpInput;

    // Footstep timing
    private float footstepTimer = 0f;
    private float currentFootstepInterval;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // Lock rotation so player doesn't tip over
        rb.freezeRotation = true;
    }

    private void Update()
    {
        // Input
        horizontalInput = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows
        verticalInput = Input.GetAxis("Vertical");     // W/S or Up/Down arrows

        // Jump input
        if (Input.GetButtonDown("Jump") && isGrounded && Time.time > lastJumpTime + jumpCooldown)
        {
            jumpInput = true;
        }

        // Ground check
        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        // Landing detection
        if (isGrounded && !wasGrounded)
        {
            OnLand();
        }

        // Apply drag
        rb.linearDamping = isGrounded ? groundDrag : airDrag;

        // Calculate current movement speed (horizontal only, ignore Y velocity)
        float currentSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;

        // Determine footstep interval based on current speed
        currentFootstepInterval = (currentSpeed > runSpeedThreshold) ? runFootstepInterval : walkFootstepInterval;

        // Check if player is actively moving (input-based)
        bool isMoving = (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f);
        bool isMovingFastEnough = currentSpeed > minSpeedForFootsteps;

        // Footstep logic - only when grounded, moving, and above minimum speed
        if (isGrounded && isMoving && isMovingFastEnough)
        {
            footstepTimer += Time.deltaTime;

            if (footstepTimer >= currentFootstepInterval)
            {
                PlayFootstep();
                footstepTimer = 0f; // Reset timer
            }
        }
        else
        {
            // Reset timer when not moving (prevents footstep immediately when starting to move)
            footstepTimer = 0f;
        }

        // Update surface detection when grounded
        if (isGrounded && surfaceAudioManager != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(groundCheck.position, Vector3.down, out hit, groundCheckRadius + 0.1f, groundLayer))
            {
                surfaceAudioManager.UpdateCurrentSurface(hit.collider);
            }
        }

        // Optional: Update speed RTPC for Wwise (can control footstep pitch/volume)
        if (playerSpeedRTPC != null)
        {
            playerSpeedRTPC.SetValue(gameObject, currentSpeed);
        }

        // Animation updates
        if (animator != null)
        {
            float moveAmount = Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput);
            animator.SetFloat("Speed", moveAmount);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        }
    }

    private void FixedUpdate()
    {
        // Movement in 2.5D space (X and Z axes)
        Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;
        rb.AddForce(moveDirection * moveSpeed, ForceMode.Acceleration);

        // Clamp horizontal speed
        Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVelocity.magnitude > maxSpeed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
        }

        // Jump
        if (jumpInput)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z); // Reset Y velocity
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpInput = false;
            lastJumpTime = Time.time;
            OnJump();
        }

        // Rotate player to face movement direction
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, Time.fixedDeltaTime * 10f);
        }
    }

    private void OnJump()
    {
        // Generic jump event (not surface-specific)
        //if (jumpEvent != null)
        //{
        //    jumpEvent.Post(gameObject);
        //}

        // Surface-specific jump
        if (surfaceAudioManager != null)
        {
            surfaceAudioManager.OnJump(gameObject);
        }

        // Trigger jump animation
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
    }

    private void OnLand()
    {
        // Generic land event
        //if (landEvent != null)
        //{
        //    landEvent.Post(gameObject);
        //}

        // Surface-specific land
        if (surfaceAudioManager != null)
        {
            surfaceAudioManager.OnLand(gameObject);
        }
    }

    private void PlayFootstep()
    {
        // Trigger surface-specific footstep via SurfaceAudioManager
        if (surfaceAudioManager != null)
        {
            surfaceAudioManager.OnFootstep(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            // Draw ground check sphere
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

            // Draw raycast line for surface detection
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * (groundCheckRadius + 0.1f));
        }
    }
}