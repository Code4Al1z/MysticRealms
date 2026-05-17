using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class EnemyActivationZone : MonoBehaviour
{
    [Header("Enemy")]
    [Tooltip("The enemy GameObject to enable when the player enters range. Must start DISABLED.")]
    [SerializeField] private GameObject enemyObject;

    [Header("Behaviour")]
    [Tooltip("If true, disables the enemy again when the player leaves the zone. " +
             "Leave false for small levels — enemies stay active once awoken.")]
    [SerializeField] private bool deactivateWhenPlayerLeaves = false;

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;

    private SphereCollider zone;
    private bool hasActivated = false;

    private void Awake()
    {
        zone = GetComponent<SphereCollider>();
        zone.isTrigger = true;

        if (enemyObject == null)
            Debug.LogWarning($"[EnemyActivationZone] {gameObject.name} has no enemy assigned.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (enemyObject == null) return;

        if (!hasActivated || deactivateWhenPlayerLeaves)
        {
            enemyObject.SetActive(true);
            hasActivated = true;

            // If the enemy will never deactivate, this zone's job is done.
            // Disable the collider to remove it from the physics pipeline entirely.
            if (!deactivateWhenPlayerLeaves)
                zone.enabled = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!deactivateWhenPlayerLeaves) return;
        if (!other.CompareTag("Player")) return;
        if (enemyObject == null) return;

        enemyObject.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null) return;

        Gizmos.color = hasActivated
            ? new Color(0f, 1f, 0f, 0.15f)   // green when active
            : new Color(1f, 0.5f, 0f, 0.15f); // orange when waiting

        Gizmos.DrawWireSphere(transform.position, col.radius);

        // Draw a line to the enemy if assigned
        if (enemyObject != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            Gizmos.DrawLine(transform.position, enemyObject.transform.position);
        }
    }
}
