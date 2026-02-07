using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FallZoneRespawn : MonoBehaviour
{
    [Header("Who can be teleported")]
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        // Ensure collider is trigger
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        PlayerDropTracker tracker = other.GetComponent<PlayerDropTracker>();
        if (tracker == null || tracker.LastDropPoint == null) return;

        Teleport(other.transform, tracker.LastDropPoint.position);
    }

    private void Teleport(Transform target, Vector3 position)
    {
        target.position = position;
    }
}
