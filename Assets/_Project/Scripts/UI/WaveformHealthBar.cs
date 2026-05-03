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
    [SerializeField] private bool  animateWave = true;
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

        Rect rect = rectTransform.rect;
        float totalWidth = rect.width;
        float height = rect.height;
        float left = rect.xMin;
        float bottom = rect.yMin;

        float barWidth = (totalWidth - barGap * (barCount - 1)) / barCount;
        if (barWidth <= 0f) return;

        float minH = height * minHeightFraction;
        float maxH = height * maxHeightFraction;

        float breathe = animateWave
            ? Mathf.Sin(breatheTimer) * breatheAmount
            : 0f;

        for (int i = 0; i < barCount; i++)
        {
            float t = (float)i / (barCount - 1);

            // Sine wave height
            float sine = Mathf.Sin(t * waveFrequency * Mathf.PI * 2f + wavePhaseOffset + breatheTimer * 0.5f);
            float sineNorm = (sine + 1f) * 0.5f + breathe;
            float barH = Mathf.Lerp(minH, maxH, Mathf.Clamp01(sineNorm));

            // Is this bar in the active (health) region or the silent region?
            bool active = t <= healthFraction;

            Color barColor;
            if (!active)
            {
                // Silent / depleted — flat and very short
                barH = minH * 0.25f;
                barColor = colorDepleted;
            }
            else if (healthFraction <= lowHealthThreshold)
            {
                barColor = colorLow;
            }
            else
            {
                // Crossfade from full colour toward low colour as health drops
                float fade = Mathf.InverseLerp(1f, lowHealthThreshold, healthFraction);
                barColor = Color.Lerp(colorFull, colorLow, fade);
            }

            // Build the quad for this bar
            float x = left + i * (barWidth + barGap);
            float y = bottom + (height - barH) * 0.5f; // vertically centred

            AddQuad(vh, x, y, barWidth, barH, barColor);
        }
    }

    private static void AddQuad(VertexHelper vh, float x, float y, float w, float h, Color c)
    {
        int index = vh.currentVertCount;

        UIVertex v = UIVertex.simpleVert;
        v.color = c;

        v.position = new Vector3(x, y); vh.AddVert(v);
        v.position = new Vector3(x, y + h); vh.AddVert(v);
        v.position = new Vector3(x + w, y + h); vh.AddVert(v);
        v.position = new Vector3(x + w, y); vh.AddVert(v);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index + 2, index + 3, index);
    }
}
