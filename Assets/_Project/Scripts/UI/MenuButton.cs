using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasRenderer))]
public class MenuButton : Graphic, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Colours")]
    [Tooltip("Tint of the semi-transparent body.")]
    [SerializeField] private Color bodyColor = new Color(0.05f, 0.08f, 0.18f, 0.72f);

    [Tooltip("Border colour at rest.")]
    [SerializeField] private Color borderColor = new Color(0.25f, 0.75f, 0.85f, 0.85f);

    [Tooltip("Border colour on hover — brighter glow.")]
    [SerializeField] private Color borderHoverColor = new Color(0.45f, 0.90f, 1.00f, 1.00f);

    [Tooltip("Border colour on press.")]
    [SerializeField] private Color borderPressColor = new Color(0.15f, 0.55f, 0.70f, 1.00f);

    [Tooltip("Inner shimmer line across the top — gives a glassy feel.")]
    [SerializeField] private Color shimmerColor = new Color(1f, 1f, 1f, 0.08f);

    [Header("Shape")]
    [SerializeField] private float cornerRadius = 10f;
    [SerializeField] private float borderWidth = 2f;
    [Tooltip("Number of vertices per corner arc — higher = smoother.")]
    [SerializeField] private int cornerSegments = 8;

    [Header("Glow")]
    [Tooltip("Extra border layers drawn outside the main border to fake a soft glow.")]
    [SerializeField] private int glowLayers = 3;
    [SerializeField] private float glowSpread = 4f;
    [Tooltip("Alpha of the outermost glow layer.")]
    [SerializeField] private float glowAlpha = 0.18f;

    [Header("Label")]
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Color labelColor = new Color(0.92f, 0.96f, 1.00f);
    [SerializeField] private Color labelHoverColor = new Color(1.00f, 1.00f, 1.00f);

    [Header("Transitions")]
    [SerializeField] private float transitionSpeed = 8f;

    // ─── Runtime state ────────────────────────────────────────────────────────

    private enum ButtonState { Normal, Hover, Pressed }
    private ButtonState state = ButtonState.Normal;
    private Color currentBorder;
    private float scaleTarget = 1f;
    private Vector3 baseScale;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        currentBorder = borderColor;
        baseScale = transform.localScale;
    }

    private void Update()
    {
        // Smoothly interpolate border colour
        Color targetBorder = state switch
        {
            ButtonState.Hover => borderHoverColor,
            ButtonState.Pressed => borderPressColor,
            _ => borderColor
        };

        bool changed = currentBorder != targetBorder;
        currentBorder = Color.Lerp(currentBorder, targetBorder, Time.unscaledDeltaTime * transitionSpeed);

        // Scale pop on hover
        float currentScale = transform.localScale.x / baseScale.x;
        if (!Mathf.Approximately(currentScale, scaleTarget))
        {
            float s = Mathf.Lerp(currentScale, scaleTarget, Time.unscaledDeltaTime * transitionSpeed);
            transform.localScale = baseScale * s;
            changed = true;
        }

        if (changed) SetVerticesDirty();

        // Label colour
        if (labelText != null)
            labelText.color = Color.Lerp(labelText.color,
                state == ButtonState.Normal ? labelColor : labelHoverColor,
                Time.unscaledDeltaTime * transitionSpeed);
    }

    // ─── Pointer events ───────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData _)
    {
        state = ButtonState.Hover;
        scaleTarget = 1.04f;
    }

    public void OnPointerExit(PointerEventData _)
    {
        state = ButtonState.Normal;
        scaleTarget = 1f;
    }

    public void OnPointerDown(PointerEventData _)
    {
        state = ButtonState.Pressed;
        scaleTarget = 0.97f;
    }

    public void OnPointerUp(PointerEventData _)
    {
        state = ButtonState.Hover;
        scaleTarget = 1.04f;
    }

    // ─── Mesh generation ─────────────────────────────────────────────────────

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r = rectTransform.rect;

        // Body
        DrawRoundedRect(vh, r, cornerRadius, bodyColor);

        // Glow layers (drawn outside, increasingly transparent)
        for (int i = 1; i <= glowLayers; i++)
        {
            float spread = glowSpread * i;
            float alpha = glowAlpha / i;
            Color glowCol = new Color(currentBorder.r, currentBorder.g, currentBorder.b, alpha);
            Rect glowRect = new Rect(r.x - spread, r.y - spread,
                                      r.width + spread * 2f, r.height + spread * 2f);
            DrawRoundedRectBorder(vh, glowRect, cornerRadius + spread, borderWidth + spread * 0.5f, glowCol);
        }

        // Main border
        DrawRoundedRectBorder(vh, r, cornerRadius, borderWidth, currentBorder);

        // Shimmer — thin bright strip across the top third of the button
        float shimmerH = r.height * 0.28f;
        Rect shimmerRect = new Rect(r.x + cornerRadius, r.yMax - shimmerH,
                                    r.width - cornerRadius * 2f, shimmerH * 0.3f);
        DrawRect(vh, shimmerRect, shimmerColor);
    }

    // ─── Drawing helpers ─────────────────────────────────────────────────────

    private void DrawRect(VertexHelper vh, Rect r, Color c)
    {
        if (r.width <= 0 || r.height <= 0) return;
        int i = vh.currentVertCount;
        AddVert(vh, new Vector2(r.xMin, r.yMin), c);
        AddVert(vh, new Vector2(r.xMin, r.yMax), c);
        AddVert(vh, new Vector2(r.xMax, r.yMax), c);
        AddVert(vh, new Vector2(r.xMax, r.yMin), c);
        vh.AddTriangle(i, i+1, i+2);
        vh.AddTriangle(i+2, i+3, i);
    }

    /// <summary>
    /// Filled rounded rectangle using a central quad + four edge quads + corner fans.
    /// </summary>
    private void DrawRoundedRect(VertexHelper vh, Rect r, float radius, Color c)
    {
        radius = Mathf.Min(radius, r.width * 0.5f, r.height * 0.5f);

        // Centre
        DrawRect(vh, new Rect(r.x + radius, r.y + radius,
                              r.width - radius * 2f, r.height - radius * 2f), c);

        // Edges
        DrawRect(vh, new Rect(r.x + radius, r.y, r.width - radius * 2f, radius), c);
        DrawRect(vh, new Rect(r.x + radius, r.yMax - radius, r.width - radius * 2f, radius), c);
        DrawRect(vh, new Rect(r.x, r.y + radius, radius, r.height - radius * 2f), c);
        DrawRect(vh, new Rect(r.xMax - radius, r.y + radius, radius, r.height - radius * 2f), c);

        // Corners
        DrawCornerFan(vh, new Vector2(r.x + radius, r.y + radius), radius, 180f, c);
        DrawCornerFan(vh, new Vector2(r.xMax - radius, r.y + radius), radius, 270f, c);
        DrawCornerFan(vh, new Vector2(r.xMax - radius, r.yMax - radius), radius, 0f,   c);
        DrawCornerFan(vh, new Vector2(r.x + radius, r.yMax - radius), radius, 90f,  c);
    }

    /// <summary>
    /// Rounded rect border as a series of thin quads along each edge + corner arcs.
    /// </summary>
    private void DrawRoundedRectBorder(VertexHelper vh, Rect r, float radius, float bw, Color c)
    {
        radius = Mathf.Min(radius, r.width * 0.5f, r.height * 0.5f);

        // Straight edge strips
        DrawRect(vh, new Rect(r.x + radius, r.y, r.width - radius * 2f, bw), c);
        DrawRect(vh, new Rect(r.x + radius, r.yMax - bw, r.width - radius * 2f, bw), c);
        DrawRect(vh, new Rect(r.x, r.y + radius, bw, r.height - radius * 2f), c);
        DrawRect(vh, new Rect(r.xMax - bw, r.y + radius, bw, r.height - radius * 2f), c);

        // Corner arcs
        DrawCornerArc(vh, new Vector2(r.x + radius, r.y + radius), radius, bw, 180f, c);
        DrawCornerArc(vh, new Vector2(r.xMax - radius, r.y + radius), radius, bw, 270f, c);
        DrawCornerArc(vh, new Vector2(r.xMax - radius, r.yMax - radius), radius, bw, 0f,   c);
        DrawCornerArc(vh, new Vector2(r.x + radius, r.yMax - radius), radius, bw, 90f,  c);
    }

    private void DrawCornerFan(VertexHelper vh, Vector2 centre, float radius, float startDeg, Color c)
    {
        float step = 90f / cornerSegments;
        for (int i = 0; i < cornerSegments; i++)
        {
            float a0 = (startDeg + step * i) * Mathf.Deg2Rad;
            float a1 = (startDeg + step * (i + 1)) * Mathf.Deg2Rad;

            int idx = vh.currentVertCount;
            AddVert(vh, centre, c);
            AddVert(vh, centre + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius, c);
            AddVert(vh, centre + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius, c);
            vh.AddTriangle(idx, idx + 1, idx + 2);
        }
    }

    private void DrawCornerArc(VertexHelper vh, Vector2 centre, float radius, float bw, float startDeg, Color c)
    {
        float step = 90f / cornerSegments;
        float innerR = radius - bw;

        for (int i = 0; i < cornerSegments; i++)
        {
            float a0 = (startDeg + step * i) * Mathf.Deg2Rad;
            float a1 = (startDeg + step * (i + 1)) * Mathf.Deg2Rad;

            Vector2 o0 = centre + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
            Vector2 o1 = centre + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
            Vector2 i0 = centre + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * innerR;
            Vector2 i1 = centre + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * innerR;

            int idx = vh.currentVertCount;
            AddVert(vh, i0, c);
            AddVert(vh, o0, c);
            AddVert(vh, o1, c);
            AddVert(vh, i1, c);
            vh.AddTriangle(idx, idx+1, idx+2);
            vh.AddTriangle(idx+2, idx+3, idx);
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
