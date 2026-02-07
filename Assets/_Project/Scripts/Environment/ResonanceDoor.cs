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

    [Tooltip("Objects to disable when door is open (e.g., door mesh, collider)")]
    [SerializeField] private GameObject[] doorObjects;

    [Tooltip("How fast the door opens/closes (0-1 per second)")]
    [SerializeField] private float doorSpeed = 2f;

    [Header("Visual Feedback")]
    [Tooltip("Particle system for door opening")]
    [SerializeField] private ParticleSystem openParticles;

    [Tooltip("Material when door is closed")]
    [SerializeField] private Material closedMaterial;

    [Tooltip("Material when door is opening/open")]
    [SerializeField] private Material openMaterial;

    [Header("Wwise Events")]
    [SerializeField] private AK.Wwise.Event doorOpenEvent;
    [SerializeField] private AK.Wwise.Event doorCloseEvent;
    [SerializeField] private AK.Wwise.Event doorActiveLoopEvent;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = false;

    private bool isOpen = false;
    private bool playerInRange = false;
    private float openAmount = 0f; // 0 = closed, 1 = open
    private Renderer doorRenderer;

    private void Start()
    {
        doorRenderer = GetComponent<Renderer>();
        SetDoorState(0f);
    }

    private void Update()
    {
        // Smoothly update door state
        float targetOpen = (isOpen && playerInRange) ? 1f : 0f;
        openAmount = Mathf.MoveTowards(openAmount, targetOpen, doorSpeed * Time.deltaTime);

        SetDoorState(openAmount);
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

        if (enableDebugLog)
            Debug.Log($"[ResonanceDoor] {name} closing");
    }

    private void SetDoorState(float amount)
    {
        // Update visual appearance
        if (doorRenderer != null)
        {
            doorRenderer.material = amount > 0.5f ? openMaterial : closedMaterial;
        }

        // Enable/disable door objects based on state
        foreach (GameObject obj in doorObjects)
        {
            if (obj != null)
            {
                obj.SetActive(amount < 0.99f);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isOpen ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, maxResonanceDistance);
    }
}
