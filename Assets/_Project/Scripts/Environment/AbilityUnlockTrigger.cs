using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AbilityUnlockTrigger : MonoBehaviour
{
    public enum AbilityType { EchoPulse, ResonanceHum }

    [SerializeField] private AbilityType abilityToUnlock = AbilityType.ResonanceHum;
    [SerializeField] private EchoPulseAbility echoPulse;
    [SerializeField] private ResonanceHumAbility resonanceHum;
    [TextArea(6,10)]
    [SerializeField] private string unlockMessage = "New ability unlocked!";
    [SerializeField] private float displayDuration = 6f;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        GameHUD hud = FindFirstObjectByType<GameHUD>();
        if (hud == null) return;

        switch (abilityToUnlock)
        {
            case AbilityType.EchoPulse:
                if (echoPulse == null) break;
                echoPulse.SetLocked(false);
                hud.UnlockEchoPulse();
                break;

            case AbilityType.ResonanceHum:
                if (resonanceHum == null) break;
                resonanceHum.SetLocked(false);
                hud.UnlockResonanceHum();
                break;
        }

        hud.ShowTutorialMessage(unlockMessage, displayDuration);
        Destroy(gameObject);
    }
}
