using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TutorialTrigger : MonoBehaviour
{
    [TextArea(6, 10)]
    [SerializeField] private string message = "Use this ability to interact with the world.";
    [SerializeField] private float displayDuration = 5f;
    [SerializeField] private bool oneShot = true;

    private bool fired = false;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (fired || !other.CompareTag("Player")) return;

        GameHUD hud = FindFirstObjectByType<GameHUD>();
        if (hud == null) return;

        hud.ShowTutorialMessage(message, displayDuration);

        if (oneShot)
        {
            fired = true;
            Destroy(gameObject);
        }
    }
}
