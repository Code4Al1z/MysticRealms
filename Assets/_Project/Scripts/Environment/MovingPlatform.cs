using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if AK_WWISE
using AK.Wwise;
#endif

[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class MovingPlatform : MonoBehaviour
{
    public enum PlatformMode { Automatic, Triggered }

    #region Inspector

    [Header("Path")]
    [SerializeField] private List<Transform> stops = new();

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 3f;
    [SerializeField] private float acceleration = 2f;
    [SerializeField] private float deceleration = 3f;
    [SerializeField] private float arrivalThreshold = 0.02f;
    [SerializeField] private float waitAtStop = 0.5f;

    [Header("Behaviour")]
    [SerializeField] private PlatformMode mode = PlatformMode.Automatic;
    [SerializeField] private bool startOnEnable = true;
    [SerializeField] private bool moveToFirstStopOnEnable = true;

    [Header("Player")]
    [SerializeField] private bool parentPlayer = true;
    [SerializeField] private string playerTag = "Player";

#if AK_WWISE
    [Header("Wwise")]
    [SerializeField] private Event startMoveEvent;
    [SerializeField] private Event stopMoveEvent;
    [SerializeField] private Event travelLoopEvent;
#endif

    #endregion

    #region Runtime

    private Rigidbody rb;
    private int currentIndex;
    private int direction = 1;

    private bool isMoving;
    private float currentSpeed;
    private Coroutine moveRoutine;

    // Public API accessors
    public bool IsMoving => isMoving;
    public float CurrentSpeed => currentSpeed;
    public int CurrentStopIndex => currentIndex;
    public List<Transform> PlatformStops => stops;
    public int LastStopIndex => (currentIndex - direction + stops.Count) % stops.Count;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void OnEnable()
    {
        if (stops.Count == 0) return;

        for (int i = 0; i < stops.Count; i++)
        {
            Vector3 targetPos = stops[i].localPosition;

            Vector3 dist = targetPos - transform.localPosition;
            Debug.Log($"Distance to stop {i}: {dist.magnitude}");
            if (dist.magnitude <= 1)
                currentIndex = i;
        }

        if (moveToFirstStopOnEnable)
            SnapOrMoveToFirstStop();

        if (mode == PlatformMode.Automatic && startOnEnable)
            StartAutomatic();
    }

    private void OnDisable() => StopMoving();

    #endregion

    #region Public API

    public void StartAutomatic()
    {
        if (isMoving || stops.Count < 2) return;
        StopMoving();
        moveRoutine = StartCoroutine(AutoLoop());
    }

    public void MoveToStopNumber(int index)
    {
        if (!IsValid(index) || isMoving) return;
        StopMoving();
        moveRoutine = StartCoroutine(MoveTo(index));
    }

    public void StopMoving()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        isMoving = false;
        currentSpeed = 0f;
#if AK_WWISE
        stopMoveEvent?.Post(gameObject);
#endif
    }

    public void CallPlatformToSnap(int stopIndex)
    {
        if (!IsValid(stopIndex)) return;
        StopMoving();
        currentIndex = stopIndex;
        rb.position = stops[currentIndex].position;
    }

    public void CallPlatformToRouted(int stopIndex)
    {
        if (!IsValid(stopIndex)) return;
        StopMoving();
        moveRoutine = StartCoroutine(RoutedTravel(stopIndex));
    }

    #endregion

    #region Coroutines

    private IEnumerator AutoLoop()
    {
        isMoving = true;
        while (true)
        {
            int next = currentIndex + direction;

            if (next >= stops.Count || next < 0)
            {
                direction *= -1;
                next = currentIndex + direction;
            }

            yield return MoveTo(next);
            currentIndex = next;

            if (waitAtStop > 0f)
                yield return new WaitForSeconds(waitAtStop);

            if (mode == PlatformMode.Triggered)
            {
                isMoving = false;
                yield break;
            }
        }
    }

    private IEnumerator RoutedTravel(int targetIndex)
    {
        isMoving = true;
        while (currentIndex != targetIndex)
        {
            int step = targetIndex > currentIndex ? 1 : -1;
            int next = currentIndex + step;

            yield return MoveTo(next);

            Vector3 targetPos = stops[next].localPosition;

            Vector3 dist = targetPos - transform.localPosition;

            if (dist.magnitude <= arrivalThreshold)
                currentIndex = next;

            if (waitAtStop > 0f)
                yield return new WaitForSeconds(waitAtStop);
        }
        isMoving = false;
    }

    private IEnumerator MoveTo(int index)
    {
        Vector3 target = stops[index].position;
        isMoving = true;

#if AK_WWISE
        startMoveEvent?.Post(gameObject);
        travelLoopEvent?.Post(gameObject);
#endif

        while (Vector3.Distance(rb.position, target) > arrivalThreshold)
        {
            float distance = Vector3.Distance(rb.position, target);
            float brakingDistance = (currentSpeed * currentSpeed) / (2f * deceleration);

            if (brakingDistance >= distance)
                currentSpeed = Mathf.Max(0.1f, currentSpeed - deceleration * Time.deltaTime);
            else
                currentSpeed = Mathf.Min(maxSpeed, currentSpeed + acceleration * Time.deltaTime);

            Vector3 nextPos = Vector3.MoveTowards(rb.position, target, currentSpeed * Time.deltaTime);

            // Move via physics engine
            rb.MovePosition(nextPos);

            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(target);
        currentSpeed = 0f;
#if AK_WWISE
        stopMoveEvent?.Post(gameObject);
#endif
    }

    #endregion

    #region Setup & Physics

    private void SnapOrMoveToFirstStop()
    {
        //currentIndex = 0;
        if (Vector3.Distance(rb.position, stops[0].position) <= arrivalThreshold)
        {
            rb.position = stops[0].position;
            return;
        }
        StopMoving();
        moveRoutine = StartCoroutine(MoveTo(0));
    }

    private bool IsValid(int index) => index >= 0 && index < stops.Count;

    private void OnCollisionEnter(Collision other)
    {
        if (parentPlayer && other.gameObject.CompareTag(playerTag))
            other.transform.SetParent(transform);
    }

    private void OnCollisionExit(Collision other)
    {
        if (parentPlayer && other.gameObject.CompareTag(playerTag))
            other.transform.SetParent(null);
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (stops == null || stops.Count < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < stops.Count - 1; i++)
        {
            if (stops[i] && stops[i + 1])
                Gizmos.DrawLine(stops[i].position, stops[i + 1].position);
        }
    }
}