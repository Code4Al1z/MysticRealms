using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class WaveformHealthBar : Graphic
{
    [Header("Bar shape")]
    [Tooltip("Number of vertical bars in the waveform.")]
    [SerializeField] private int barCount = 32;

    [Tooltip("Gap between bars in pixels.")]
    [SerializeField] private float barGap = 2f;

    [Tooltip("Minimum bar height as a fraction of the RectTransform height.")]
    [SerializeField] private float minHeightFraction = 0.15f;

    [Tooltip("Maximum bar height as a fraction of the RectTransform height.")]
    [SerializeField] private float maxHeightFraction = 0.90f;

    [Tooltip("Controls how many full sine cycles appear across the bar.")]
    [SerializeField] private float waveFrequency = 2.8f;

    [Tooltip("Horizontal phase shift — randomise this per enemy/player for variety.")]
    [SerializeField] private float wavePhaseOffset = 0f;

    [Header("Colours")]
    [SerializeField] private Color colorFull = new Color(0.88f, 0.63f, 0.19f);
    [SerializeField] private Color colorLow = new Color(0.82f, 0.25f, 0.12f);
    [SerializeField] private Color colorDepleted = new Color(0.22f, 0.18f, 0.08f);

    [Tooltip("Health fraction below which the bar shifts toward colorLow.")]
    [SerializeField] private float lowHealthThreshold = 0.35f;

    [Header("Animation")]
    [Tooltip("If true the waveform gently breathes when health is above the low threshold.")]
    [SerializeField] private bool animateWave = true;
    [SerializeField] private float breatheSpeed = 0.9f;
    [SerializeField] private float breatheAmount = 0.06f;

    // 0–1, set from outside
    private float healthFraction = 1f;
    private float breatheTimer = 0f;

    // ─── Public API ───────────────────────────────────────────────────────────

    public void SetHealth(float normalised)
    {
        healthFraction = Mathf.Clamp01(normalised);
        SetVerticesDirty();
    }

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Update()
    {
        if (!animateWave || healthFraction <= lowHealthThreshold) return;

        breatheTimer += Time.deltaTime * breatheSpeed;
        SetVerticesDirty();
    }

    // ─── Mesh Generation ─────────────────────────────────────────────────────

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        var s = new WaveformRenderer.Settings
        {
            barCount = barCount,
            barGap = barGap,
            minHeightFraction = minHeightFraction,
            maxHeightFraction = maxHeightFraction,
            waveFrequency = waveFrequency,
            wavePhaseOffset = wavePhaseOffset,
            breatheTimer = breatheTimer,
            breatheAmount = animateWave ? breatheAmount : 0f,
        };

        WaveformRenderer.Draw(vh, rectTransform.rect, s,
            healthFraction, colorFull, colorDepleted, colorLow, lowHealthThreshold);
    }
}
