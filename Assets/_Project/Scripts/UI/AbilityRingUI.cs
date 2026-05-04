using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class AbilityRingUI : Graphic
{
    [Header("Ring Colours")]
    [SerializeField] private Color ringTrackColor = new Color(0.10f, 0.15f, 0.25f, 1f);
    [SerializeField] private Color ringFillColor = new Color(0.25f, 0.75f, 0.90f, 1f);
    [SerializeField] private Color ringLockedColor = new Color(0.25f, 0.22f, 0.35f, 1f);
    [SerializeField] private Color shineColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] private Color glowColor = new Color(0.25f, 0.75f, 0.90f, 0.30f);

    [Header("Ring Shape")]
    [SerializeField] private float ringWidth = 6f;
    [SerializeField] private int ringSegments = 56;

    [Header("Glow")]
    [SerializeField] private int glowLayers = 3;
    [SerializeField] private float glowSpread = 3.5f;

    private float energy = 1f;
    private bool isLocked = true;
    private float activeGlow = 0f;

    public void UpdateState(float energy, bool isLocked, float activeGlow)
    {
        this.energy = energy;
        this.isLocked = isLocked;
        this.activeGlow = activeGlow;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r = rectTransform.rect;
        float cx = r.center.x;
        float cy = r.center.y;
        float radius = Mathf.Min(r.width, r.height) * 0.5f - 1f;

        // Glow behind ring when active
        if (activeGlow > 0.01f)
        {
            for (int g = glowLayers; g >= 1; g--)
            {
                float spread = glowSpread * g;
                float alpha = glowColor.a * activeGlow / g;
                Color gc = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
                DrawArc(vh, cx, cy, radius + spread, ringWidth + spread * 0.5f, 0f, 360f, ringSegments, gc);
            }
        }

        // Ring track
        DrawArc(vh, cx, cy, radius, ringWidth, 0f, 360f, ringSegments,
            isLocked ? ringLockedColor : ringTrackColor);

        // Ring fill
        if (!isLocked && energy > 0f)
        {
            Color fill = Color.Lerp(ringFillColor, Color.white, activeGlow * 0.25f);
            DrawArc(vh, cx, cy, radius, ringWidth, 90f, 90f - energy * 360f, ringSegments, fill);
        }

        // Shine arc when active
        if (activeGlow > 0.01f)
        {
            Color shine = new Color(shineColor.r, shineColor.g, shineColor.b, shineColor.a * activeGlow);
            DrawArc(vh, cx, cy, radius - ringWidth - 1f, (radius - ringWidth) * 0.55f,
                110f, 70f, 16, shine);
        }
    }

    private void DrawArc(VertexHelper vh, float cx, float cy,
        float radius, float width,
        float startDeg, float endDeg,
        int segs, Color c)
    {
        float innerR = Mathf.Max(0f, radius - width);
        float step = (endDeg - startDeg) / segs;
        for (int i = 0; i < segs; i++)
        {
            float a0 = (startDeg + step * i) * Mathf.Deg2Rad;
            float a1 = (startDeg + step * (i + 1)) * Mathf.Deg2Rad;
            Vector2 o0 = new Vector2(cx + Mathf.Cos(a0) * radius, cy + Mathf.Sin(a0) * radius);
            Vector2 o1 = new Vector2(cx + Mathf.Cos(a1) * radius, cy + Mathf.Sin(a1) * radius);
            Vector2 i0 = new Vector2(cx + Mathf.Cos(a0) * innerR, cy + Mathf.Sin(a0) * innerR);
            Vector2 i1 = new Vector2(cx + Mathf.Cos(a1) * innerR, cy + Mathf.Sin(a1) * innerR);
            int idx = vh.currentVertCount;
            AddVert(vh, i0, c); AddVert(vh, o0, c);
            AddVert(vh, o1, c); AddVert(vh, i1, c);
            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx + 2, idx + 3, idx);
        }
    }

    private static void AddVert(VertexHelper vh, Vector2 pos, Color c)
    {
        UIVertex v = UIVertex.simpleVert;
        v.position = pos;
        v.color = c;
        vh.AddVert(v);
    }
}
