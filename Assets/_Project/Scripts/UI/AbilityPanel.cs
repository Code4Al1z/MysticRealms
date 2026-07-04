using UnityEngine;

public class AbilityPanel : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private AbilitySlotUI echoPulseSlot;
    [SerializeField] private AbilitySlotUI resonanceHumSlot;

    private PlayerAbilities playerAbilities;
    private ResonanceHumAbility resonanceHum;
    private EchoPulseAbility echoPulse;

    // ─── Initialise ───────────────────────────────────────────────────────────

    public void Initialise(PlayerAbilities abilities)
    {
        playerAbilities = abilities;

        echoPulse = abilities.GetEchoPulse();
        resonanceHum = abilities.GetResonanceHum();

        if (resonanceHum != null)
            resonanceHum.OnEnergyChangedEvent += OnResonanceEnergyChanged;

        if (echoPulseSlot != null)
        {
            echoPulseSlot.SetLocked(echoPulse == null || echoPulse.IsLocked);
            echoPulseSlot.SetEnergy(0f);
        }

        if (resonanceHumSlot != null)
        {
            resonanceHumSlot.SetLocked(resonanceHum == null || resonanceHum.IsLocked);
            resonanceHumSlot.SetEnergy(resonanceHum != null ? resonanceHum.GetEnergyPercent() : 0f);
        }
    }

    private void OnDestroy()
    {
        if (resonanceHum != null)
            resonanceHum.OnEnergyChangedEvent -= OnResonanceEnergyChanged;
    }

    // ─── Update ───────────────────────────────────────────────────────────────

    private void Update()
    {
        if (playerAbilities == null) return;

        UpdateEchoPulseSlot();
        UpdateResonanceHumSlot();
    }

    private void UpdateEchoPulseSlot()
    {
        if (echoPulse == null) return;

        if (echoPulseSlot != null)
            echoPulseSlot.SetLocked(echoPulse.IsLocked);

        if (echoPulse.IsLocked) return;

        bool active = echoPulse.IsActive();
        if (echoPulseSlot != null)
        {
            echoPulseSlot.SetActive(active);
            echoPulseSlot.SetEnergy(active ? 1f : 0f);
        }
    }

    private void UpdateResonanceHumSlot()
    {
        if (resonanceHum == null) return;

        if (resonanceHumSlot != null)
            resonanceHumSlot.SetLocked(resonanceHum.IsLocked);

        if (resonanceHum.IsLocked) return;

        if (resonanceHumSlot != null)
        {
            resonanceHumSlot.SetActive(resonanceHum.IsActive());
            resonanceHumSlot.SetEnergy(resonanceHum.GetEnergyPercent());
        }

        // Recharge delay countdown
        if (!resonanceHum.IsActive() && resonanceHum.GetEnergyPercent() < 1f)
        {
            float delay = resonanceHum.GetRechargeDelayRemaining();
            if (resonanceHumSlot != null)
                resonanceHumSlot.SetRechargeTimer(delay);
        }
        else
        {
            if (resonanceHumSlot != null)
                resonanceHumSlot.SetRechargeTimer(0f);
        }
    }

    private void OnResonanceEnergyChanged(float energyPercent)
    {
        if (resonanceHumSlot != null)
            resonanceHumSlot.SetEnergy(energyPercent);
    }

    // ─── Unlock ───────────────────────────────────────────────────────────────

    public void UnlockEchoPulse()
    {
        if (echoPulse != null)
            echoPulse.SetLocked(false);
        if (echoPulseSlot != null)
            echoPulseSlot.SetLocked(false);
    }

    public void UnlockResonanceHum()
    {
        if (resonanceHum != null)
            resonanceHum.SetLocked(false);
        if (resonanceHumSlot != null)
            resonanceHumSlot.SetLocked(false);
    }
}