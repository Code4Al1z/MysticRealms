using UnityEngine;

/// <summary>
/// Smooth camera follow system for 2.5D side-scrolling gameplay.
/// Follows player on X-axis with full tracking and Y-axis with constraints.
/// Z-axis stays at fixed depth (no following).
/// Features smooth damping and optional rotation towards target.
/// </summary>
[RequireComponent(typeof(Camera))]
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

    [Header("Smart Y-Tracking")]
    [Range(0.1f, 1.0f)]
    [Tooltip("Percent of the screen (vertical) where the player can move without the camera following. e.g., 0.8 = center 80%.")]
    [SerializeField] private float yDeadZonePercent = 0.8f;

    [Tooltip("Time in seconds the player must stay at a new Y height before the camera centers on them.")]
    [SerializeField] private float idleAdjustDelay = 5f;

    [Tooltip("How smoothly the camera adjusts during the idle period.")]
    [SerializeField] private float idleSmoothTime = 1.0f;

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
    private Camera cam;

    private float currentTrackedY;
    private float idleTimer;
    private float currentYSmoothTime; // Dynamic smooth time for idle vs active movement

    private void Start()
    {
        cam = GetComponent<Camera>();

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

        // Store initial rotation and Z
        initialRotation = transform.rotation;
        initialCameraZ = transform.position.z;

        // Initialize Y tracking
        if (target != null)
        {
            currentTrackedY = target.position.y + offset.y;
        }

        currentYSmoothTime = smoothTime;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        CalculateTargetPosition();

        // Apply smooth follow with damping
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            currentYSmoothTime, // Now uses a dynamic smooth time
            maxSpeed
        );

        transform.position = smoothedPosition;

        if (enableRotation)
        {
            ApplyRotation();
        }
    }

    private void CalculateTargetPosition()
    {
        // X-axis: Full follow with optional bounds
        float targetX = target.position.x + offset.x;
        if (useBounds)
        {
            targetX = Mathf.Clamp(targetX, minXBound, maxXBound);
        }

        // Y-axis: Smart Follow Logic
        float targetY = transform.position.y;

        if (followYAxis)
        {
            HandleSmartYTracking();
            targetY = Mathf.Clamp(currentTrackedY, minYPosition, maxYPosition);
        }

        // Z-axis: Fixed depth
        float targetZ = initialCameraZ;

        targetPosition = new Vector3(targetX, targetY, targetZ);
    }

    private void HandleSmartYTracking()
    {
        // 1. Convert player's world position to Viewport space (0 to 1)
        Vector3 viewportPos = cam.WorldToViewportPoint(target.position);

        // Calculate viewport boundaries based on deadZoneHeight percentage
        // e.g., if percent is 0.8, margins are 0.1 at bottom and 0.9 at top
        float margin = (1f - yDeadZonePercent) / 2f;
        float bottomBound = margin;
        float topBound = 1f - margin;

        bool isOutsideDeadZone = viewportPos.y < bottomBound || viewportPos.y > topBound;

        float desiredY = target.position.y + offset.y;

        if (isOutsideDeadZone)
        {
            // Player is falling or rising (platforms/big jumps)
            currentTrackedY = desiredY;
            currentYSmoothTime = smoothTime; // Use responsive smoothing
            idleTimer = 0f;
        }
        else
        {
            // Player is inside the 80-90% safe area. 
            // Check if they have stayed at a new height for at least idleAdjustDelay
            if (Mathf.Abs(currentTrackedY - desiredY) > 0.05f)
            {
                idleTimer += Time.deltaTime;
                if (idleTimer >= idleAdjustDelay)
                {
                    // Slowly adjust the camera to center the player
                    currentYSmoothTime = idleSmoothTime;
                    currentTrackedY = desiredY;
                }
            }
            else
            {
                idleTimer = 0f;
            }
        }
    }

    private void ApplyRotation()
    {
        Vector3 dir = target.position - transform.position;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion lookRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot * initialRotation, Time.deltaTime / rotationSmoothTime);
    }

    #region Public API
    public void SetTarget(Transform newTarget) => target = newTarget;
    public void SetOffset(Vector3 newOffset) => offset = newOffset;
    public void SnapToTarget()
    {
        if (target == null) return;
        CalculateTargetPosition();
        transform.position = targetPosition;
        currentTrackedY = target.position.y + offset.y;
        velocity = Vector3.zero;
    }
    public void SetYConstraints(float min, float max) { minYPosition = min; maxYPosition = max; }
    public void SetXBounds(float min, float max) { minXBound = min; maxXBound = max; }
    public void SetFollowYAxis(bool enable) => followYAxis = enable;
    public void SetUseBounds(bool enable) => useBounds = enable;
    public Vector3 GetTargetPosition() => targetPosition;
    public Vector3 GetVelocity() => velocity;
    public void SetCameraDepth(float newZ)
    {
        initialCameraZ = newZ;
        transform.position = new Vector3(transform.position.x, transform.position.y, newZ);
    }
    #endregion

    #region Debug & Gizmos
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || target == null) return;

        // Draw viewport deadzone lines in world space for visualization
        if (cam == null) cam = GetComponent<Camera>();
        Gizmos.color = Color.yellow;
        float margin = (1f - yDeadZonePercent) / 2f;

        // Visualizing the thresholds
        for (float i = 0; i < 1.1f; i += 1f)
        {
            float yRatio = (i == 0) ? margin : 1f - margin;
            Vector3 worldLineStart = cam.ViewportToWorldPoint(new Vector3(0, yRatio, Mathf.Abs(offset.z)));
            Vector3 worldLineEnd = cam.ViewportToWorldPoint(new Vector3(1, yRatio, Mathf.Abs(offset.z)));
            Gizmos.DrawLine(worldLineStart, worldLineEnd);
        }

        // Draw original constraints
        Gizmos.color = Color.red;
        float xPos = target.position.x;
        Gizmos.DrawLine(new Vector3(xPos - 2, minYPosition, 0), new Vector3(xPos + 2, minYPosition, 0));
        Gizmos.DrawLine(new Vector3(xPos - 2, maxYPosition, 0), new Vector3(xPos + 2, maxYPosition, 0));
    }
    #endregion

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (minYPosition > maxYPosition) { float t = minYPosition; minYPosition = maxYPosition; maxYPosition = t; }
        if (minXBound > maxXBound) { float t = minXBound; minXBound = maxXBound; maxXBound = t; }
        smoothTime = Mathf.Max(0f, smoothTime);
        idleAdjustDelay = Mathf.Max(0f, idleAdjustDelay);
    }
#endif
}