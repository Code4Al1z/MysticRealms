using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBarPanel : MonoBehaviour
{
    [SerializeField] private Slider   healthSlider;
    [SerializeField] private TMP_Text bossNameText;

    private IEnemyDamageable currentTarget;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void SetTarget(IEnemyDamageable boss)
    {
        if (currentTarget != null)
        {
            currentTarget.OnHealthChanged -= OnHealthChanged;
            currentTarget.OnDied -= OnDied;
        }

        currentTarget = boss;
        gameObject.SetActive(boss != null);

        if (boss == null) return;

        if (bossNameText  != null) 
            bossNameText.text = boss.DisplayName;
        if (healthSlider  != null) 
            healthSlider.value = 1f;

        boss.OnHealthChanged += OnHealthChanged;
        boss.OnDied += OnDied;
    }

    private void OnHealthChanged(float current, float max)
    {
        if (healthSlider != null)
            healthSlider.value = max > 0f ? current / max : 0f;
    }

    private void OnDied(UnityEngine.GameObject _)
    {
        gameObject.SetActive(false);
        currentTarget = null;
    }

    private void OnDestroy()
    {
        if (currentTarget == null) return;
        currentTarget.OnHealthChanged -= OnHealthChanged;
        currentTarget.OnDied -= OnDied;
    }
}
