using UnityEngine;

public class PortalActivator : MonoBehaviour
{
    [SerializeField] private LevelData levelData;
    [SerializeField] private Collider portalTrigger;
    [SerializeField] private PortalRound_Controller portalRoundController;

    [Header("Wwise")]
    [SerializeField] private AK.Wwise.Event portalActivateEvent;
    [SerializeField] private AK.Wwise.Event portalEnterEvent;

    private PlayerHealth playerHealth;
    private bool isActivated = false;

    private void Start()
    {
        if (portalTrigger != null)
            portalTrigger.enabled = false;

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null) return;

        playerHealth = playerGO.GetComponent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.OnCollectableChanged += CheckThreshold;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnCollectableChanged -= CheckThreshold;
    }

    private void CheckThreshold(int total)
    {
        if (isActivated || levelData == null) return;
        if (total < levelData.requiredCollectables) return;

        isActivated = true;

        if (portalTrigger != null) 
            portalTrigger.enabled = true;
        if (portalRoundController != null)
            portalRoundController.F_TogglePortalRound(true);
        if (portalActivateEvent != null)
            portalActivateEvent.Post(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActivated) return;
        if (!other.CompareTag("Player")) return;

        if (portalEnterEvent != null)
            portalEnterEvent.Post(gameObject);
        if (GameManager.Instance != null)
            GameManager.Instance.TriggerVictory();
    }
}
