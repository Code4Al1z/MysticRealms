using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FallZoneRespawn : MonoBehaviour
{
    [Header("Who can be teleported")]
    [SerializeField] private string playerTag = "Player";

    [Header("Respawn Settings")]
    [Tooltip("Optional upward offset applied to the respawn position so the player spawns slightly above the floor")]
    [SerializeField] private float respawnHeightOffset = 0.1f;

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        PlayerDropTracker tracker = other.GetComponent<PlayerDropTracker>();
        if (tracker == null || tracker.LastDropPoint == null) return;

        Teleport(other, tracker.LastDropPoint.position);
    }

    private void Teleport(Collider playerCollider, Vector3 targetPosition)
    {
        Transform playerTransform = playerCollider.transform;

        Vector3 spawnPos = targetPosition + Vector3.up * respawnHeightOffset;
        playerTransform.position = spawnPos;

        PlayerController playerController = playerCollider.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.ResetVelocity();
        }
        else
        {
            // Fallback: directly zero out the Rigidbody if PlayerController isn't found
            Rigidbody rb = playerCollider.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = Vector3.zero;

            Debug.LogWarning("[FallZoneRespawn] PlayerController not found on player — " +
                             "velocity reset via Rigidbody directly. Consider adding PlayerController.");
        }
    }
}