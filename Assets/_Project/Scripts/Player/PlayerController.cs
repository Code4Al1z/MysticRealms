using UnityEngine;

/// <summary>
/// Handles hovering creature movement, jumping, and input for 2.5D platformer
/// Unity 6.3 + Wwise 2025.1.4 compatible
/// Mystic Realms - Hovering Character Controller with Jump
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float maxSpeed = 10f;
    public float hoverHeight = 2f;
    public float hoverForce = 10f;
    public float hoverDamping = 5f;
    public float movementDrag = 6f;

    [Header("Jump")]
    public float jumpForce = 8f;
    public float jumpCooldown = 0.5f;
    private float lastJumpTime = -999f;

    [Header("Hover Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 3f;
    public LayerMask groundLayer;

    [Header("Wwise Audio")]
    public AK.Wwise.Event hoverStartEvent;        // Triggered once when hovering begins
    public AK.Wwise.Event hoverStopEvent;         // Triggered when hovering stops
    public AK.Wwise.Event moveEvent;              // Movement whoosh sounds
    public AK.Wwise.Event jumpEvent;              // Jump sound
    public AK.Wwise.Event landEvent;              // Landing sound
    public AK.Wwise.RTPC playerSpeedRTPC;         // Controls hover intensity based on speed
    public AK.Wwise.RTPC hoverHeightRTPC;         // Controls hover pitch based on height from ground

    [Header("Surface Audio System")]
    public SurfaceAudioManager surfaceAudioManager;

    private Rigidbody rb;
    private Animator animator;
    private bool isGrounded;
    private bool wasGrounded;
    private float horizontalInput;
    private float verticalInput; // For 2.5D movement (Z-axis)
    private bool jumpInput;
    
    private bool isHovering;
    private float currentHoverHeight;
    private float moveTimer;
    public float moveEventInterval = 0.3f; // How often to trigger movement whoosh

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // Lock rotation so player doesn't tip over
        rb.freezeRotation = true;

        // Start hovering
        StartHover();
    }

    private void Update()
    {
        // Input
        horizontalInput = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows
        verticalInput = Input.GetAxis("Vertical");     // W/S or Up/Down arrows (for 2.5D depth)

        // Jump input
        if (Input.GetButtonDown("Jump") && isGrounded && Time.time > lastJumpTime + jumpCooldown)
        {
            jumpInput = true;
        }

        // Check height from ground for hover calculations
        RaycastHit hit;
        wasGrounded = isGrounded;
        if (Physics.Raycast(groundCheck.position, Vector3.down, out hit, groundCheckDistance, groundLayer))
        {
            currentHoverHeight = hit.distance;
            isGrounded = currentHoverHeight <= hoverHeight + 0.5f; // Within hover range = grounded
            isHovering = true;

            // Check surface type when grounded/near ground
            if (surfaceAudioManager != null && isGrounded)
            {
                surfaceAudioManager.UpdateCurrentSurface(hit.collider);
            }
        }
        else
        {
            currentHoverHeight = groundCheckDistance;
            isGrounded = false;
            isHovering = true; // Always hovering in this game
        }

        // Landing detection
        if (isGrounded && !wasGrounded)
        {
            OnLand();
        }

        // Update hover height RTPC for Wwise (affects pitch/filtering)
        if (hoverHeightRTPC != null)
        {
            float normalizedHeight = Mathf.Clamp01(currentHoverHeight / groundCheckDistance);
            hoverHeightRTPC.SetValue(gameObject, normalizedHeight * 100f); // 0-100 range
        }

        // Movement sound triggers (whoosh when moving)
        bool isMoving = Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f;
        if (isMoving && isGrounded)
        {
            moveTimer += Time.deltaTime;
            if (moveTimer >= moveEventInterval)
            {
                PlayMoveSound();
                moveTimer = 0f;
            }
        }
        else
        {
            moveTimer = 0f;
        }

        // Update speed RTPC for Wwise (affects hover hum intensity)
        float currentSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
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
            animator.SetBool("IsHovering", isHovering);
            animator.SetFloat("HoverHeight", currentHoverHeight);
            animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        }

        // Apply constant drag for floaty feel
        rb.linearDamping = movementDrag;
    }

    private void FixedUpdate()
    {
        // Apply hover force to maintain height
        ApplyHoverForce();

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

    private void ApplyHoverForce()
    {
        // Only apply hover force when grounded/close to ground
        if (!isGrounded) return;

        // Cast ray downward to detect ground
        RaycastHit hit;
        if (Physics.Raycast(groundCheck.position, Vector3.down, out hit, groundCheckDistance, groundLayer))
        {
            // Calculate hover force based on distance from desired height
            float heightDifference = hoverHeight - hit.distance;
            float force = heightDifference * hoverForce - rb.linearVelocity.y * hoverDamping;

            // Apply upward force
            rb.AddForce(Vector3.up * force, ForceMode.Acceleration);
        }
    }

    private void StartHover()
    {
        if (hoverStartEvent != null)
        {
            hoverStartEvent.Post(gameObject);
        }
        isHovering = true;
    }

    private void StopHover()
    {
        if (hoverStopEvent != null)
        {
            hoverStopEvent.Post(gameObject);
        }
        isHovering = false;
    }

    private void OnJump()
    {
        if (jumpEvent != null)
        {
            jumpEvent.Post(gameObject);
        }

        // Trigger surface-specific jump sound
        if (surfaceAudioManager != null)
        {
            surfaceAudioManager.OnJump(gameObject);
        }

        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
    }

    private void OnLand()
    {
        if (landEvent != null)
        {
            landEvent.Post(gameObject);
        }

        // Trigger surface-specific landing sound
        if (surfaceAudioManager != null)
        {
            surfaceAudioManager.OnLand(gameObject);
        }
    }

    private void PlayMoveSound()
    {
        if (moveEvent != null)
        {
            moveEvent.Post(gameObject);
        }

        // Trigger surface-specific movement sound
        if (surfaceAudioManager != null)
        {
            surfaceAudioManager.OnMove(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            // Draw hover height indicator
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);
            
            // Draw desired hover height
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position + Vector3.down * hoverHeight, 0.3f);

            // Draw grounded threshold
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position + Vector3.down * (hoverHeight + 0.5f), 0.25f);
        }
    }

    private void OnDestroy()
    {
        // Stop hover sound when destroyed
        StopHover();
    }
}
