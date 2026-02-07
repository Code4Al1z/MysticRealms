using UnityEngine;

/// <summary>
/// Smooth camera follow system for 2.5D side-scrolling gameplay.
/// Follows player on X-axis with full tracking and Y-axis with constraints.
/// Z-axis stays at fixed depth (no following).
/// Features smooth damping and optional rotation towards target.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);

    [Header("Follow Behavior")]
    [Tooltip("Smoothing time for camera movement. Lower = snappier, Higher = smoother")]
    [SerializeField] private float smoothTime = 0.3f;

    [Tooltip("Maximum speed the camera can move")]
    [SerializeField] private float maxSpeed = 20f;

    [Header("Y-Axis Constraints")]
    [Tooltip("Enable Y-axis following (with constraints)")]
    [SerializeField] private bool followYAxis = true;

    [Tooltip("Minimum Y position the camera can reach (world space)")]
    [SerializeField] private float minYPosition = 0f;

    [Tooltip("Maximum Y position the camera can reach (world space)")]
    [SerializeField] private float maxYPosition = 10f;

    [Tooltip("Dead zone range on Y-axis where camera won't move")]
    [SerializeField] private float yDeadZone = 2f;

    [Header("Rotation Settings")]
    [Tooltip("Enable camera rotation towards target")]
    [SerializeField] private bool enableRotation = false;

    [Tooltip("Maximum rotation angle (degrees) towards target")]
    [SerializeField] private float maxRotationAngle = 5f;

    [Tooltip("Smoothing time for rotation")]
    [SerializeField] private float rotationSmoothTime = 0.5f;

    [Header("Boundaries (Optional)")]
    [Tooltip("Enable camera bounds (useful for keeping camera within level)")]
    [SerializeField] private bool useBounds = false;

    [Tooltip("Minimum X position (left boundary)")]
    [SerializeField] private float minXBound = -100f;

    [Tooltip("Maximum X position (right boundary)")]
    [SerializeField] private float maxXBound = 100f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    // Private state
    private Vector3 velocity = Vector3.zero;
    private Vector3 targetPosition;
    private Quaternion initialRotation;
    private float initialCameraZ;

    private void Start()
    {
        // Auto-find player if target not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("CameraFollow: Auto-assigned player as target");
            }
            else
            {
                Debug.LogWarning("CameraFollow: No target assigned and no GameObject with 'Player' tag found!");
            }
        }

        // Store initial rotation
        initialRotation = transform.rotation;

        // Store initial Z position (camera depth stays fixed)
        initialCameraZ = transform.position.z;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Calculate desired position
        CalculateTargetPosition();

        // Apply smooth follow with damping
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime,
            maxSpeed
        );

        transform.position = smoothedPosition;

        // Apply rotation if enabled
        if (enableRotation)
        {
            ApplyRotation();
        }
    }

    /// <summary>
    /// Calculates the target position the camera should move towards
    /// </summary>
    private void CalculateTargetPosition()
    {
        Vector3 desiredPosition = target.position + offset;

        // X-axis: Full follow
        float targetX = desiredPosition.x;

        // Apply X bounds if enabled
        if (useBounds)
        {
            targetX = Mathf.Clamp(targetX, minXBound, maxXBound);
        }

        // Y-axis: Constrained follow with dead zone
        float targetY;
        if (followYAxis)
        {
            float playerY = target.position.y;
            float currentCameraY = transform.position.y;

            // Calculate distance from player
            float yDistance = Mathf.Abs(playerY + offset.y - currentCameraY);

            // Only move if outside dead zone
            if (yDistance > yDeadZone)
            {
                targetY = playerY + offset.y;

                // Apply Y constraints
                targetY = Mathf.Clamp(targetY, minYPosition, maxYPosition);
            }
            else
            {
                // Stay at current Y position (within dead zone)
                targetY = currentCameraY;
            }
        }
        else
        {
            // Don't follow Y-axis, keep current Y position
            targetY = transform.position.y;
        }

        // Z-axis: Stay at fixed camera depth (don't follow player Z at all)
        // Always use the initial camera Z position for 2.5D side-scrolling
        float targetZ = initialCameraZ;

        targetPosition = new Vector3(targetX, targetY, targetZ);
    }

    /// <summary>
    /// Applies smooth rotation towards the target using LookAt
    /// </summary>
    private void ApplyRotation()
    {
        Vector3 dir = target.position - transform.position;

        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion lookRot = Quaternion.LookRotation(dir);

        transform.rotation = lookRot * initialRotation;
    }

    /// <summary>
    /// Sets a new target for the camera to follow
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// Sets the camera offset from the target
    /// </summary>
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }

    /// <summary>
    /// Immediately snaps camera to target position (no smoothing)
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null)
            return;

        CalculateTargetPosition();
        transform.position = targetPosition;
        velocity = Vector3.zero;
    }

    /// <summary>
    /// Sets Y-axis constraints at runtime
    /// </summary>
    public void SetYConstraints(float min, float max)
    {
        minYPosition = min;
        maxYPosition = max;
    }

    /// <summary>
    /// Sets X-axis bounds at runtime
    /// </summary>
    public void SetXBounds(float min, float max)
    {
        minXBound = min;
        maxXBound = max;
    }

    /// <summary>
    /// Enables or disables Y-axis following
    /// </summary>
    public void SetFollowYAxis(bool enable)
    {
        followYAxis = enable;
    }

    /// <summary>
    /// Enables or disables camera bounds
    /// </summary>
    public void SetUseBounds(bool enable)
    {
        useBounds = enable;
    }

    /// <summary>
    /// Gets the current target position the camera is moving towards
    /// </summary>
    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }

    /// <summary>
    /// Gets the current velocity of the camera
    /// </summary>
    public Vector3 GetVelocity()
    {
        return velocity;
    }

    /// <summary>
    /// Sets a new fixed Z position for the camera (useful for camera zones)
    /// </summary>
    public void SetCameraDepth(float newZ)
    {
        initialCameraZ = newZ;
        transform.position = new Vector3(transform.position.x, transform.position.y, newZ);
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || target == null)
            return;

        // Draw target position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(target.position, 0.5f);

        // Draw offset position
        Gizmos.color = Color.yellow;
        Vector3 offsetPos = target.position + offset;
        Gizmos.DrawWireSphere(offsetPos, 0.3f);

        // Draw line from camera to target
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.position);

        // Draw Y constraints
        if (followYAxis)
        {
            Gizmos.color = Color.red;
            float xPos = target.position.x;
            Vector3 minYPos = new Vector3(xPos, minYPosition, transform.position.z);
            Vector3 maxYPos = new Vector3(xPos, maxYPosition, transform.position.z);

            Gizmos.DrawLine(minYPos + Vector3.left * 2f, minYPos + Vector3.right * 2f);
            Gizmos.DrawLine(maxYPos + Vector3.left * 2f, maxYPos + Vector3.right * 2f);

            // Draw dead zone
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            float centerY = target.position.y + offset.y;
            Vector3 deadZoneMin = new Vector3(xPos, centerY - yDeadZone, transform.position.z);
            Vector3 deadZoneMax = new Vector3(xPos, centerY + yDeadZone, transform.position.z);
            Gizmos.DrawLine(deadZoneMin + Vector3.left * 1f, deadZoneMin + Vector3.right * 1f);
            Gizmos.DrawLine(deadZoneMax + Vector3.left * 1f, deadZoneMax + Vector3.right * 1f);
        }

        // Draw X bounds
        if (useBounds)
        {
            Gizmos.color = Color.magenta;
            float yPos = transform.position.y;
            Vector3 leftBound = new Vector3(minXBound, yPos, transform.position.z);
            Vector3 rightBound = new Vector3(maxXBound, yPos, transform.position.z);

            Gizmos.DrawLine(leftBound + Vector3.up * 5f, leftBound + Vector3.down * 5f);
            Gizmos.DrawLine(rightBound + Vector3.up * 5f, rightBound + Vector3.down * 5f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos)
            return;

        // Draw detailed info when selected
        Gizmos.color = Color.white;

        if (target != null)
        {
            // Draw camera frustum bounds at target position
            UnityEngine.Camera cam = GetComponent<UnityEngine.Camera>();
            if (cam != null)
            {
                float frustumHeight = 2.0f * Mathf.Abs(offset.z) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float frustumWidth = frustumHeight * cam.aspect;

                Vector3 targetPos = target.position + offset;

                Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
                Gizmos.DrawWireCube(targetPos, new Vector3(frustumWidth, frustumHeight, 0.1f));
            }
        }
    }

#if UNITY_EDITOR
    // Editor-only validation
    private void OnValidate()
    {
        // Ensure constraints are valid
        if (minYPosition > maxYPosition)
        {
            Debug.LogWarning("CameraFollow: minYPosition is greater than maxYPosition. Swapping values.");
            float temp = minYPosition;
            minYPosition = maxYPosition;
            maxYPosition = temp;
        }

        if (minXBound > maxXBound)
        {
            Debug.LogWarning("CameraFollow: minXBound is greater than maxXBound. Swapping values.");
            float temp = minXBound;
            minXBound = maxXBound;
            maxXBound = temp;
        }

        // Ensure smooth time is positive
        if (smoothTime < 0f)
        {
            smoothTime = 0f;
        }

        // Ensure max speed is positive
        if (maxSpeed < 0f)
        {
            maxSpeed = 0f;
        }

        // Ensure dead zone is non-negative
        if (yDeadZone < 0f)
        {
            yDeadZone = 0f;
        }

        // Clamp rotation angle
        maxRotationAngle = Mathf.Clamp(maxRotationAngle, 0f, 45f);
    }
#endif
}