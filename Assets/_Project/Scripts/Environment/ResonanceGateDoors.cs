using UnityEngine;

/// <summary>
/// Gate doors that open while connected ResonanceLamp has energy
/// Closes when lamp is depleted
/// </summary>
public class ResonanceGateDoors : MonoBehaviour
{
    [Header("Door References")]
    [Tooltip("Left door transform")]
    [SerializeField] private Transform leftDoor;

    [Tooltip("Right door transform")]
    [SerializeField] private Transform rightDoor;

    [Header("Lamp Connection")]
    [Tooltip("The ResonanceLamp that powers these doors")]
    [SerializeField] private ResonanceLamp connectedLamp;

    [Header("Door Movement")]
    [Tooltip("How fast the doors open/close (0-1 per second)")]
    [SerializeField] private float doorSpeed = 2f;

    [Tooltip("Rotation for left door when opening (local euler angles)")]
    [SerializeField] private Vector3 leftDoorOpenRotation = new Vector3(0f, 90f, 0f);

    [Tooltip("Rotation for right door when opening (local euler angles)")]
    [SerializeField] private Vector3 rightDoorOpenRotation = new Vector3(0f, 90f, 0f);

    [Header("Visual Feedback")]
    [Tooltip("Particle system for door opening")]
    [SerializeField] private ParticleSystem openParticles;

    [Tooltip("Particle system for door closing")]
    [SerializeField] private ParticleSystem closeParticles;

    [Header("Wwise Events")]
    [SerializeField] private AK.Wwise.Event doorOpenEvent;
    [SerializeField] private AK.Wwise.Event doorCloseEvent;
    [SerializeField] private AK.Wwise.Event doorMovingLoopEvent;
    [SerializeField] private AK.Wwise.Event doorStopEvent;

    [Header("Wwise RTPCs")]
    [SerializeField] private AK.Wwise.RTPC doorOpenAmountRTPC;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = false;

    private Vector3 leftDoorClosedRotation;
    private Vector3 rightDoorClosedRotation;

    private bool isOpen = false;
    private bool isMoving = false;
    private float currentOpenAmount = 0f;
    private uint movingLoopPlayingID = 0;

    private void Start()
    {
        if (leftDoor != null)
        {
            leftDoorClosedRotation = leftDoor.localEulerAngles;
        }

        if (rightDoor != null)
        {
            rightDoorClosedRotation = rightDoor.localEulerAngles;
        }

        if (connectedLamp != null)
        {
            connectedLamp.OnEnergyChangedEvent += OnLampEnergyChanged;
        }
    }

    private void OnDestroy()
    {
        if (connectedLamp != null)
        {
            connectedLamp.OnEnergyChangedEvent -= OnLampEnergyChanged;
        }
    }

    private void Update()
    {
        UpdateDoorPositions();
        UpdateWwiseRTPC();
    }

    private void OnLampEnergyChanged(float energyPercent)
    {
        bool shouldBeOpen = connectedLamp.HasEnergy();

        if (shouldBeOpen && !isOpen)
        {
            OpenDoors();
        }
        else if (!shouldBeOpen && isOpen)
        {
            CloseDoors();
        }
    }

    private void OpenDoors()
    {
        isOpen = true;
        isMoving = true;

        if (doorOpenEvent != null)
        {
            doorOpenEvent.Post(gameObject);
        }

        if (doorMovingLoopEvent != null && movingLoopPlayingID == 0)
        {
            movingLoopPlayingID = doorMovingLoopEvent.Post(gameObject);
        }

        if (openParticles != null)
        {
            openParticles.Play();
        }

        if (enableDebugLog)
            Debug.Log($"[ResonanceGateDoors] {name} opening");
    }

    private void CloseDoors()
    {
        isOpen = false;
        isMoving = true;

        if (doorCloseEvent != null)
        {
            doorCloseEvent.Post(gameObject);
        }

        if (doorMovingLoopEvent != null && movingLoopPlayingID == 0)
        {
            movingLoopPlayingID = doorMovingLoopEvent.Post(gameObject);
        }

        if (closeParticles != null)
        {
            closeParticles.Play();
        }

        if (enableDebugLog)
            Debug.Log($"[ResonanceGateDoors] {name} closing");
    }

    private void UpdateDoorPositions()
    {
        float targetOpenAmount = isOpen ? 1f : 0f;

        currentOpenAmount = Mathf.MoveTowards(currentOpenAmount, targetOpenAmount, doorSpeed * Time.deltaTime);

        // Stop moving loop when doors reach target position
        if (isMoving && Mathf.Approximately(currentOpenAmount, targetOpenAmount))
        {
            isMoving = false;

            if (doorStopEvent != null)
            {
                doorStopEvent.Post(gameObject);
            }
            else if (movingLoopPlayingID != 0)
            {
                AkUnitySoundEngine.StopPlayingID(movingLoopPlayingID);
                movingLoopPlayingID = 0;
            }
        }

        if (leftDoor != null)
        {
            Vector3 targetRotation = Vector3.Lerp(leftDoorClosedRotation, leftDoorOpenRotation, currentOpenAmount);
            leftDoor.localEulerAngles = targetRotation;
        }

        if (rightDoor != null)
        {
            Vector3 targetRotation = Vector3.Lerp(rightDoorClosedRotation, rightDoorOpenRotation, currentOpenAmount);
            rightDoor.localEulerAngles = targetRotation;
        }
    }

    private void UpdateWwiseRTPC()
    {
        if (doorOpenAmountRTPC != null)
        {
            doorOpenAmountRTPC.SetValue(gameObject, currentOpenAmount * 100f);
        }
    }

    public bool IsFullyOpen()
    {
        return Mathf.Approximately(currentOpenAmount, 1f);
    }

    public bool IsFullyClosed()
    {
        return Mathf.Approximately(currentOpenAmount, 0f);
    }

    public float GetOpenAmount()
    {
        return currentOpenAmount;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isOpen ? Color.green : Color.red;

        if (leftDoor != null)
        {
            Gizmos.DrawWireCube(leftDoor.position, leftDoor.lossyScale);
        }

        if (rightDoor != null)
        {
            Gizmos.DrawWireCube(rightDoor.position, rightDoor.lossyScale);
        }

        // Draw connection line to lamp
        if (connectedLamp != null && Application.isPlaying)
        {
            Gizmos.color = connectedLamp.HasEnergy() ? Color.yellow : Color.gray;
            Gizmos.DrawLine(transform.position, connectedLamp.transform.position);
        }
    }
}