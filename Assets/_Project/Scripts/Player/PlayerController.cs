using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player movement, jumping, and footsteps for 2.5D platformer.
/// Unity 6.3 + Wwise 2025.1.4 compatible.
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
    [SerializeField] private float maxSlopeAngle = 45f;
    private RaycastHit slopeHit;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float jumpCooldown = 0.5f;
    private float lastJumpTime = -999f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Wall Unstick")]
    [Tooltip("Impulse strength applied to separate the player from a wall. Tune in Inspector.")]
    [SerializeField] private float wallUnstickForce = 5f;
    [Tooltip("Contact normal |Y| below this value is treated as a wall (not floor/ceiling). " +
             "0.4 covers surfaces steeper than ~66 degrees from horizontal.")]
    [SerializeField] private float wallNormalYThreshold = 0.4f;
    [Tooltip("All layers that can pin the player against a wall. Include Ground and any solid obstacle layers.")]
    [SerializeField] private LayerMask wallLayers;

    private Vector3 accumulatedWallNormal = Vector3.zero;
    private bool hasWallContact = false;
    private bool isWallStuck = false;

    [Header("Footstep Timing")]
    [Tooltip("Seconds between footsteps when walking")]
    [SerializeField] private float walkFootstepInterval = 0.5f;
    [Tooltip("Seconds between footsteps when running")]
    [SerializeField] private float runFootstepInterval = 0.3f;
    [Tooltip("Speed threshold above which the run interval is used")]
    [SerializeField] private float runSpeedThreshold = 6f;
    [Tooltip("Minimum horizontal speed required to trigger footsteps")]
    [SerializeField] private float minSpeedForFootsteps = 0.5f;

    [Header("Wwise Audio - Optional")]
    [SerializeField] private AK.Wwise.RTPC playerSpeedRTPC;

    [Header("Surface Audio System - Required")]
    [SerializeField] private SurfaceAudioManager surfaceAudioManager;

    private Rigidbody rb;
    private bool isGrounded;
    private bool wasGrounded;
    private float horizontalInput;
    private float verticalInput;
    private bool jumpInput;
    private Vector3 cachedMoveDirection = Vector3.zero;
    private float footstepTimer = 0f;

    private Collider lastSurfaceCollider = null;

    public bool IsGrounded() => isGrounded;

    public void ResetVelocity()
    {
        if (rb != null)
            rb.linearVelocity = Vector3.zero;

        cachedMoveDirection = Vector3.zero;
        jumpInput = false;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (surfaceAudioManager == null)
            Debug.LogError("[PlayerController] SurfaceAudioManager reference is missing! Assign it in the Inspector.");

        if (groundCheck != null && surfaceAudioManager != null)
        {
            Collider[] overlaps = Physics.OverlapSphere(groundCheck.position, groundCheckRadius, groundLayer.value);
            if (overlaps != null && overlaps.Length > 0)
                TryUpdateSurface(overlaps[0]);
        }
    }

    private void Update()
    {
        var kb = Keyboard.current;
        float h = 0f;
        float v = 0f;

        if (kb != null)
        {
            if (kb.dKey != null && kb.dKey.isPressed) h += 1f;
            if (kb.aKey != null && kb.aKey.isPressed) h -= 1f;
            if (kb.wKey != null && kb.wKey.isPressed) v += 1f;
            if (kb.sKey != null && kb.sKey.isPressed) v -= 1f;

            if (kb.spaceKey != null && kb.spaceKey.wasPressedThisFrame
                && isGrounded && Time.time > lastJumpTime + jumpCooldown)
                jumpInput = true;
        }

        horizontalInput = Mathf.Clamp(h, -1f, 1f);
        verticalInput = Mathf.Clamp(v, -1f, 1f);

        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded && !wasGrounded)
            OnLand();

        rb.linearDamping = isGrounded ? groundDrag : airDrag;

        float currentSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        float footstepInterval = (currentSpeed > runSpeedThreshold) ? runFootstepInterval : walkFootstepInterval;
        bool isMoving = Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f;

        if (isGrounded && isMoving && currentSpeed > minSpeedForFootsteps)
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= footstepInterval)
            {
                PlayFootstep();
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }

        if (isGrounded && surfaceAudioManager != null && isMoving)
        {
            Collider[] overlaps = Physics.OverlapSphere(groundCheck.position, groundCheckRadius, groundLayer.value);
            if (overlaps != null && overlaps.Length > 0)
                TryUpdateSurface(overlaps[0]);
        }

        if (playerSpeedRTPC != null)
            playerSpeedRTPC.SetValue(gameObject, currentSpeed);
    }

    private void OnCollisionStay(Collision collision)
    {
        if ((wallLayers.value & (1 << collision.gameObject.layer)) == 0) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (Mathf.Abs(contact.normal.y) < wallNormalYThreshold)
            {
                accumulatedWallNormal += contact.normal;
                hasWallContact = true;
            }
        }
    }

    private void FixedUpdate()
    {
        Vector3 inputDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        if (isGrounded)
            cachedMoveDirection = inputDirection;

        Vector3 moveDirection = cachedMoveDirection;
        Vector3 slopeMoveDir = GetSlopeMoveDirection(moveDirection);
        Vector3 targetVelocity = slopeMoveDir * moveSpeed;

        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 velocityDifference = targetVelocity - new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        rb.AddForce(velocityDifference, ForceMode.VelocityChange);

        if (isGrounded && rb.linearVelocity.y <= 0f)
            rb.AddForce(Vector3.down * 20f, ForceMode.Force);

        if (jumpInput && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpInput = false;
            lastJumpTime = Time.time;
            OnJump();
        }

        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, Time.fixedDeltaTime * 10f);
        }

        if (hasWallContact && !isGrounded)
        {
            Vector3 escapeDir = Vector3.ProjectOnPlane(accumulatedWallNormal.normalized, Vector3.up).normalized;

            if (escapeDir != Vector3.zero)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                rb.AddForce(escapeDir * wallUnstickForce, ForceMode.VelocityChange);
                cachedMoveDirection = escapeDir;
                isWallStuck = true;
            }
        }
        else
        {
            isWallStuck = false;
        }

        accumulatedWallNormal = Vector3.zero;
        hasWallContact = false;
    }

    private void OnJump()
    {
        if (surfaceAudioManager != null)
        {
            // surfaceAudioManager.OnJump(gameObject);
        }
    }

    private void OnLand()
    {
        if (surfaceAudioManager != null)
            surfaceAudioManager.OnLand(gameObject);
    }

    private void PlayFootstep()
    {
        if (surfaceAudioManager != null)
            surfaceAudioManager.OnFootstep(gameObject);
    }

    private void TryUpdateSurface(Collider col)
    {
        if (col == lastSurfaceCollider) return;
        lastSurfaceCollider = col;
        surfaceAudioManager.UpdateCurrentSurface(col);
    }

    private Vector3 GetSlopeMoveDirection(Vector3 moveDir)
    {
        if (!isGrounded) return moveDir;

        if (Physics.Raycast(groundCheck.position, Vector3.down, out slopeHit,
            groundCheckRadius + 0.2f, groundLayer))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            if (angle > 0f && angle <= maxSlopeAngle)
                return Vector3.ProjectOnPlane(moveDir, slopeHit.normal).normalized;
        }
        return moveDir;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(groundCheck.position,
            groundCheck.position + Vector3.down * (groundCheckRadius + 0.1f));

        Gizmos.color = isWallStuck ? Color.magenta : new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);
    }
}