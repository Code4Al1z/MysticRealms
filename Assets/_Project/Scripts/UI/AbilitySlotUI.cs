using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilitySlotUI : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Slider energyBar;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image activeGlow;
    [SerializeField] private Image lockedOverlay;
    [SerializeField] private TMP_Text rechargeTimerText;

    [Header("Active Glow")]
    [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.2f, 0.6f);

    private bool isLocked = true;

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        if (lockedOverlay != null) lockedOverlay.gameObject.SetActive(locked);
        if (energyBar != null) energyBar.gameObject.SetActive(!locked);
    }

    public void SetEnergy(float normalised)
    {
        if (energyBar != null)
            energyBar.value = Mathf.Clamp01(normalised);
    }

    public void SetActive(bool active)
    {
        if (activeGlow == null) return;
        activeGlow.gameObject.SetActive(active);
        activeGlow.color = glowColor;
    }

    public void SetRechargeTimer(float secondsRemaining)
    {
        if (rechargeTimerText == null) return;
        rechargeTimerText.gameObject.SetActive(secondsRemaining > 0f);
        rechargeTimerText.text = secondsRemaining > 0f ? $"{secondsRemaining:F1}s" : string.Empty;
    }

    public void SetIcon(Sprite sprite)
    {
        if (iconImage != null) iconImage.sprite = sprite;
    }
}