using UnityEngine;
using TMPro;
using System.Collections;

public class CollectablePanel : MonoBehaviour
{
    [SerializeField] private TMP_Text countText;
    [SerializeField] private LevelData levelData;

    [Header("Pulse on pickup")]
    [SerializeField] private float pulseScale = 1.25f;
    [SerializeField] private float pulseDuration = 0.2f;

    private PlayerHealth playerHealth;
    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    public void Initialise(PlayerHealth ph, LevelData data)
    {
        playerHealth = ph;
        levelData = data;
        ph.OnCollectableChanged += OnCollectableChanged;
        RefreshText(ph.Collectables);
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnCollectableChanged -= OnCollectableChanged;
    }

    private void OnCollectableChanged(int total)
    {
        RefreshText(total);
        StopAllCoroutines();
        StartCoroutine(Pulse());
    }

    private void RefreshText(int total)
    {
        if (countText == null) return;
        int required = levelData != null ? levelData.requiredCollectables : 0;
        countText.text = required > 0 ? $"{total} / {required}" : total.ToString();
    }

    private IEnumerator Pulse()
    {
        float elapsed = 0f;
        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pulseDuration;
            float s = Mathf.Lerp(pulseScale, 1f, t);
            transform.localScale = baseScale * s;
            yield return null;
        }
        transform.localScale = baseScale;
    }
}