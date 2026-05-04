using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Static utility — draws waveform bar geometry into a Unity UI VertexHelper.
/// Used by WaveformHealthBar, WaveformSlider, and BossHealthBar.
/// </summary>
public static class WaveformRenderer
{
    public struct Settings
    {
        public int barCount;
        public float barGap;
        public float minHeightFraction;
        public float maxHeightFraction;
        public float waveFrequency;
        public float wavePhaseOffset;
        public float breatheTimer;
        public float breatheAmount;
    }

    public static Settings Default => new Settings
    {
        barCount = 32,
        barGap = 2f,
        minHeightFraction = 0.15f,
        maxHeightFraction = 0.90f,
        waveFrequency = 2.8f,
        wavePhaseOffset = 0f,
        breatheTimer = 0f,
        breatheAmount = 0.06f,
    };

    public static void Draw(
        VertexHelper vh,
        Rect rect,
        Settings s,
        float fillFraction,
        Color activeColor,
        Color inactiveColor,
        Color lowColor,
        float lowThreshold = 0.35f)
    {
        float totalWidth = rect.width;
        float height = rect.height;
        float left = rect.xMin;
        float bottom = rect.yMin;

        float barWidth = (totalWidth - s.barGap * (s.barCount - 1)) / s.barCount;
        if (barWidth <= 0f) return;

        float minH = height * s.minHeightFraction;
        float maxH = height * s.maxHeightFraction;
        float breathe = Mathf.Sin(s.breatheTimer) * s.breatheAmount;

        for (int i = 0; i < s.barCount; i++)
        {
            float t = s.barCount > 1 ? (float)i / (s.barCount - 1) : 0f;
            float sine = Mathf.Sin(t * s.waveFrequency * Mathf.PI * 2f
                                      + s.wavePhaseOffset
                                      + s.breatheTimer * 0.5f);
            float norm = Mathf.Clamp01((sine + 1f) * 0.5f + breathe);
            float barH = Mathf.Lerp(minH, maxH, norm);
            bool active = t <= fillFraction;

            Color barColor;
            if (!active)
            {
                barH = minH * 0.25f;
                barColor = inactiveColor;
            }
            else if (fillFraction <= lowThreshold)
            {
                barColor = lowColor;
            }
            else
            {
                float fade = Mathf.InverseLerp(1f, lowThreshold, fillFraction);
                barColor = Color.Lerp(activeColor, lowColor, fade);
            }

            float x = left + i * (barWidth + s.barGap);
            float y = bottom + (height - barH) * 0.5f;

            AddQuad(vh, x, y, barWidth, barH, barColor);
        }
    }

    public static void DrawSolid(
        VertexHelper vh,
        Rect rect,
        Settings s,
        float fillFraction,
        Color activeColor,
        Color inactiveColor)
    {
        Draw(vh, rect, s, fillFraction, activeColor, inactiveColor, activeColor, 0f);
    }

    private static void AddQuad(VertexHelper vh, float x, float y, float w, float h, Color c)
    {
        int idx = vh.currentVertCount;
        UIVertex v = UIVertex.simpleVert;
        v.color = c;

        v.position = new Vector3(x, y); vh.AddVert(v);
        v.position = new Vector3(x, y + h); vh.AddVert(v);
        v.position = new Vector3(x + w, y + h); vh.AddVert(v);
        v.position = new Vector3(x + w, y); vh.AddVert(v);

        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx + 2, idx + 3, idx);
    }
}
