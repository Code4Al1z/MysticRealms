using UnityEngine;

public class PlatformCallTrigger : MonoBehaviour
{
    [SerializeField] private MovingPlatform platform;
    [SerializeField] private int stopIndex;
    [SerializeField] private bool snapToStop = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!platform.gameObject.activeSelf) return;

        if (other.CompareTag("Player"))
        {
            if (snapToStop)
            {
                platform.CallPlatformToSnap(stopIndex);
            }
            else
            {
                platform.CallPlatformToRouted(stopIndex);
            }
        }
    }
}
