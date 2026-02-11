using UnityEngine;
using System.Collections;

public class ResonanceDoor : MonoBehaviour, IResonanceResponsive
{
    [Header("Door Settings")]
    [SerializeField] private float maxResonanceDistance = 15f;
    [SerializeField] private Transform doorObject; // The actual mesh/moving part
    [SerializeField] private float doorSpeed = 2f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Open State (Targets)")]
    [SerializeField] private Vector3 openPosition;
    [SerializeField] private Vector3 openRotation;
    [SerializeField] private Vector3 openScale = Vector3.one;

    [Header("Visual & Audio")]
    [SerializeField] private ParticleSystem openParticles;
    [SerializeField] private AK.Wwise.Event doorOpenEvent;
    [SerializeField] private AK.Wwise.Event doorCloseEvent;

    private Vector3 closedPosition;
    private Quaternion closedRotation;
    private Vector3 closedScale;

    private Coroutine transitionRoutine;
    private bool isOpen = false;

    private void Awake()
    {
        // Cache starting values
        if (doorObject == null) doorObject = transform;
        closedPosition = doorObject.localPosition;
        closedRotation = doorObject.localRotation;
        closedScale = doorObject.localScale;
    }

    public void OnResonanceHumActive(Vector3 sourcePosition, float distance)
    {
        bool inRange = distance <= maxResonanceDistance;

        if (inRange && !isOpen) ToggleDoor(true);
        else if (!inRange && isOpen) ToggleDoor(false);
    }

    public void OnResonanceHumStopped()
    {
        if (isOpen) ToggleDoor(false);
    }

    private void ToggleDoor(bool state)
    {
        isOpen = state;

        // Sound & FX
        if (isOpen)
        {
            doorOpenEvent?.Post(gameObject);
            if (openParticles != null)
                openParticles.Play();
        }
        else
        {
            doorCloseEvent?.Post(gameObject);
            if (openParticles != null)
                openParticles.Stop();
        }

        // Handle Movement
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);

        Vector3 targetPos = isOpen ? openPosition : closedPosition;
        Quaternion targetRot = Quaternion.Euler(isOpen ? openRotation : closedRotation.eulerAngles);
        Vector3 targetScale = isOpen ? openScale : closedScale;

        transitionRoutine = StartCoroutine(MoveDoor(targetPos, targetRot, targetScale));
    }

    private IEnumerator MoveDoor(Vector3 targetPos, Quaternion targetRot, Vector3 targetScale)
    {
        float elapsed = 0;
        Vector3 startPos = doorObject.localPosition;
        Quaternion startRot = doorObject.localRotation;
        Vector3 startScale = doorObject.localScale;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * doorSpeed;
            float percent = transitionCurve.Evaluate(elapsed);

            doorObject.localPosition = Vector3.Lerp(startPos, targetPos, percent);
            doorObject.localRotation = Quaternion.Slerp(startRot, targetRot, percent);
            doorObject.localScale = Vector3.Lerp(startScale, targetScale, percent);

            yield return null;
        }

        // Snap to final values
        doorObject.localPosition = targetPos;
        doorObject.localRotation = targetRot;
        doorObject.localScale = targetScale;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isOpen ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, maxResonanceDistance);
    }
}