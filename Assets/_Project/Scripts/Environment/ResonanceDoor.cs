using UnityEngine;

/// <summary>
/// Door/gate that opens while player maintains Resonance Hum
/// Closes when resonance stops
/// </summary>
public class ResonanceDoor : MonoBehaviour, IResonanceResponsive
{
    [Header("Door Settings")]
    [Tooltip("Maximum distance player can be for resonance to work")]
    [SerializeField] private float maxResonanceDistance = 15f;

    [Tooltip("Objects to modify when door is open")]
    [SerializeField] private Transform doorObject;

    [Tooltip("How fast the door opens/closes (0-1 per second)")]
    [SerializeField] private float doorSpeed = 2f;

    [Header("Visual Feedback")]
    [Tooltip("Particle system for door opening")]
    [SerializeField] private ParticleSystem openParticles;

    [Tooltip("Target position and scale when door is opening/open")]
    [SerializeField] private Vector3 doorOpenPosition;
    [SerializeField] private Vector3 doorOpenScale;

    [Header("Wwise Events")]
    [SerializeField] private AK.Wwise.Event doorOpenEvent;
    [SerializeField] private AK.Wwise.Event doorCloseEvent;
    [SerializeField] private AK.Wwise.Event doorActiveLoopEvent;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = false;

    private Vector3 doorClosedPosition;
    private Vector3 doorClosedScale;

    private bool isOpen = false;
    private bool playerInRange = false;

    private void Start()
    {
        doorClosedPosition = transform.localPosition;
        doorClosedScale = transform.localScale;
    }

    public void OnResonanceHumActive(Vector3 sourcePosition, float distance)
    {
        playerInRange = distance <= maxResonanceDistance;

        if (playerInRange && !isOpen)
        {
            OpenDoor();
        }
        else if (!playerInRange && isOpen)
        {
            CloseDoor();
        }
    }

    public void OnResonanceHumStopped()
    {
        playerInRange = false;
        if (isOpen)
        {
            CloseDoor();
        }
    }

    private void OpenDoor()
    {
        isOpen = true;

        if (doorOpenEvent != null)
        {
            doorOpenEvent.Post(gameObject);
        }

        if (doorActiveLoopEvent != null)
        {
            doorActiveLoopEvent.Post(gameObject);
        }

        if (openParticles != null)
        {
            openParticles.Play();
        }

        SetDoorState(isOpen);

        if (enableDebugLog)
            Debug.Log($"[ResonanceDoor] {name} opening");
    }

    private void CloseDoor()
    {
        isOpen = false;

        if (doorCloseEvent != null)
        {
            doorCloseEvent.Post(gameObject);
        }

        SetDoorState(isOpen);

        if (enableDebugLog)
            Debug.Log($"[ResonanceDoor] {name} closing");
    }

    private void SetDoorState(bool isOpen)
    {
        Vector3 targetPosition = isOpen ? doorOpenPosition : doorClosedPosition;
        Vector3 targetScale = isOpen ? doorOpenScale : doorClosedScale;
        gameObject.transform.localPosition = Vector3.Lerp(gameObject.transform.localPosition, targetPosition, doorSpeed);
        gameObject.transform.localScale = Vector3.Lerp(gameObject.transform.localScale, targetScale, doorSpeed);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isOpen ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, maxResonanceDistance);
    }
}
