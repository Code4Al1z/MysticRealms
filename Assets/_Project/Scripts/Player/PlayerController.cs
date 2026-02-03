using UnityEngine;

/// <summary>
/// Handles player movement, jumping, and footsteps for 2.5D platformer
/// Unity 6.3 + Wwise 2025.1.4 compatible
/// Mystic Realms - Grounded Character Controller with Surface-Based Footsteps
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float groundDrag = 6f;
    [SerializeField] private float airDrag = 2f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float jumpCooldown = 0.5f;
    private float lastJumpTime = -999f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Footstep Timing")]
    [Tooltip("Seconds between footsteps when walking (slow speed)")]
    [SerializeField] private float walkFootstepInterval = 0.5f;

    [Tooltip("Seconds between footsteps when running (fast speed)")]
    [SerializeField] private float runFootstepInterval = 0.3f;

    [Tooltip("Speed threshold - above this value, use run interval")]
    [SerializeField] private float runSpeedThreshold = 6f;

    [Tooltip("Minimum speed to trigger footsteps (prevents footsteps when barely moving)")]
    [SerializeField] private float minSpeedForFootsteps = 0.5f;

    [Header("Wwise Audio - Optional")]
    [Tooltip("Optional: RTPC to control audio based on player speed (pitch/volume)")]
    [SerializeField] private AK.Wwise.RTPC playerSpeedRTPC;

    [Header("Surface Audio System - Required")]
    [Tooltip("REQUIRED: Reference to SurfaceAudioManager - handles all surface-specific sounds")]
    [SerializeField] private SurfaceAudioManager surfaceAudioManager;

    private Rigidbody rb;
    private Animator animator;
    private bool isGrounded;
    private bool wasGrounded;
    private float horizontalInput;
    private float verticalInput;
    private bool jumpInput;

    // Footstep timing
    private float footstepTimer = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // Lock rotation so player doesn't tip over
        rb.freezeRotation = true;

        // Validate surface audio manager reference
        if (surfaceAudioManager == null)
        {
            Debug.LogError("[PlayerController] SurfaceAudioManager reference is missing! Assign it in the Inspector.");
        }
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
        float currentFootstepInterval = (currentSpeed > runSpeedThreshold) ? runFootstepInterval : walkFootstepInterval;

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

        // Optional: Update speed RTPC for Wwise (can control footstep pitch/volume dynamically)
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

    /// <summary>
    /// Called when player jumps - triggers surface-specific jump sound via SurfaceAudioManager
    /// NOTE: If you want jumps, uncomment the OnJump() call in SurfaceAudioManager
    /// For minimal version, you can skip jump sounds entirely
    /// </summary>
    private void OnJump()
    {
        if (surfaceAudioManager != null)
        {
            // Uncomment this line if you have jump sounds:
            // surfaceAudioManager.OnJump(gameObject);
        }

        // Trigger jump animation
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
    }

    /// <summary>
    /// Called when player lands - triggers surface-specific landing sound via SurfaceAudioManager
    /// </summary>
    private void OnLand()
    {
        if (surfaceAudioManager != null)
        {
            surfaceAudioManager.OnLand(gameObject);
        }
    }

    /// <summary>
    /// Called periodically while moving - triggers surface-specific footstep sound via SurfaceAudioManager
    /// </summary>
    private void PlayFootstep()
    {
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