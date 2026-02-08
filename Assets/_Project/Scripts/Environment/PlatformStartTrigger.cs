using UnityEngine;

public class PlatformStartTrigger : MonoBehaviour
{
    [SerializeField] private MovingPlatform platform;
    [SerializeField] private int targetStopIndex = 0;
    [SerializeField] private bool targetOtherEnd = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            platform.MoveToStopNumber(targetOtherEnd ? platform.LastStopIndex : targetStopIndex);
    }
}
