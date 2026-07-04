using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class AbilityRingUI : Graphic
{
    [Header("Ring Colours")]
    [SerializeField] private Color ringTrackColor = new Color(0.10f, 0.15f, 0.25f, 1f);
    [SerializeField] private Color ringFillColor = new Color(0.25f, 0.75f, 0.90f, 1f);
    [SerializeField] private Color ringActiveColor = new Color(0.50f, 0.92f, 1.00f, 1f);
    [SerializeField] private Color ringLockedColor = new Color(0.25f, 0.22f, 0.35f, 1f);

    [Header("Ring Shape")]
    [SerializeField] private float ringWidth = 6f;
    [SerializeField] private int ringSegments = 56;

    [Header("Pulse (active state)")]
    [Tooltip("How many times per second the ring pulses brighter while active")]
    [SerializeField] private float pulseFrequency = 1.8f;
    [Tooltip("How much brighter the ring gets at the pulse peak (0 = no pulse, 1 = full white)")]
    [SerializeField] private float pulseStrength = 0.28f;

    // ─── State set by AbilitySlotUI ───────────────────────────────────────────

    private float energy = 1f;
    private bool isLocked = true;
    private float activeGlow = 0f;   // 0..1 lerped from AbilitySlotUI

    // ─── Internal ─────────────────────────────────────────────────────────────

    private float _pulseTimer = 0f;

    public void UpdateState(float energy, bool isLocked, float activeGlow)
    {
        this.energy = energy;
        this.isLocked = isLocked;
        this.activeGlow = activeGlow;
        SetVerticesDirty();
    }

    private void Update()
    {
        if (activeGlow > 0.01f)
        {
            _pulseTimer += Time.deltaTime * pulseFrequency * Mathf.PI * 2f;
            SetVerticesDirty();
        }
        else if (_pulseTimer != 0f)
        {
            _pulseTimer = 0f;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r = rectTransform.rect;
        float cx = r.center.x;
        float cy = r.center.y;
        float radius = Mathf.Min(r.width, r.height) * 0.5f - 1f;

        // Ring track
        DrawArc(vh, cx, cy, radius, ringWidth, 0f, 360f, ringSegments,
            isLocked ? ringLockedColor : ringTrackColor);

        // Ring fill — colour lerps from normal to active, plus pulse on top
        if (!isLocked && energy > 0f)
        {
            float pulse = activeGlow > 0.01f
                ? Mathf.Sin(_pulseTimer) * 0.5f + 0.5f   // 0..1
                : 0f;
            float brightness = activeGlow * pulseStrength * pulse;

            Color fillBase = Color.Lerp(ringFillColor, ringActiveColor, activeGlow);
            Color fill = Color.Lerp(fillBase, Color.white, brightness);

            DrawArc(vh, cx, cy, radius, ringWidth, 90f, 90f - energy * 360f, ringSegments, fill);
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