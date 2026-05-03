using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthPanel : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private WaveformHealthBar healthBar;

    [Header("Lives — Icon Row")]
    [SerializeField] private Image[] lifeIcons;
    [SerializeField] private Color lifeActiveColor = Color.white;
    [SerializeField] private Color lifeInactiveColor = new Color(1f, 1f, 1f, 0.18f);

    [Header("Lives — Text (alternative to icons)")]
    [SerializeField] private TMP_Text livesText;

    private PlayerHealth playerHealth;

    private void Awake() { }

    public void Initialise(PlayerHealth ph)
    {
        playerHealth = ph;
        ph.OnHealthChanged += OnHealthChanged;
        ph.OnLifeLost += OnLivesChanged;
        ph.OnLifeGained += OnLivesChanged;

        OnHealthChanged(ph.CurrentHealth, ph.MaxHealth);
        OnLivesChanged(ph.Lives);
    }

    private void OnDestroy()
    {
        if (playerHealth == null) return;
        playerHealth.OnHealthChanged -= OnHealthChanged;
        playerHealth.OnLifeLost -= OnLivesChanged;
        playerHealth.OnLifeGained -= OnLivesChanged;
    }

    private void OnHealthChanged(float current, float max)
    {
        if (healthBar != null)
            healthBar.SetHealth(max > 0f ? current / max : 0f);
    }

    private void OnLivesChanged(int remaining)
    {
        if (livesText != null)
            livesText.text = $"♥ {remaining}";

        if (lifeIcons == null) return;
        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;
            lifeIcons[i].color = i < remaining ? lifeActiveColor : lifeInactiveColor;
        }
    }
}