using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Custom pill-style toggle matching the MenuButton aesthetic.
/// Draws a sliding knob inside a rounded rect body with glow border.
/// Assign a TMP_Text child as labelText and optionally wire MenuButtonSounds.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class DisplayToggle : Graphic,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Colours")]
    [SerializeField] private Color bodyOffColor     = new Color(0.05f, 0.08f, 0.18f, 0.72f);
    [SerializeField] private Color bodyOnColor      = new Color(0.05f, 0.18f, 0.28f, 0.85f);
    [SerializeField] private Color borderColor      = new Color(0.25f, 0.75f, 0.85f, 0.85f);
    [SerializeField] private Color borderHoverColor = new Color(0.45f, 0.90f, 1.00f, 1.00f);
    [SerializeField] private Color knobColor        = new Color(0.85f, 0.95f, 1.00f, 1.00f);
    [SerializeField] private Color knobOnColor      = new Color(0.35f, 0.80f, 1.00f, 1.00f);

    [Header("Shape")]
    [SerializeField] private float cornerRadius   = 10f;
    [SerializeField] private float borderWidth    = 2f;
    [SerializeField] private int   cornerSegments = 8;
    [SerializeField] private float knobPadding    = 4f;

    [Header("Glow")]
    [SerializeField] private int   glowLayers = 3;
    [SerializeField] private float glowSpread = 4f;
    [SerializeField] private float glowAlpha  = 0.18f;

    [Header("Label")]
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Color    labelColor       = new Color(0.92f, 0.96f, 1.00f);
    [SerializeField] private Color    labelActiveColor = new Color(1.00f, 1.00f, 1.00f);

    [Header("Transitions")]
    [SerializeField] private float transitionSpeed = 8f;

    [Header("Wwise")]
    [SerializeField] private MenuButtonSounds sounds;

    // ─── Public API ───────────────────────────────────────────────────────────

    public event System.Action<bool> OnValueChanged;

    private bool _isOn = false;

    public bool IsOn
    {
        get => _isOn;
        set
        {
            if (_isOn == value) return;
            _isOn = value;
            SetVerticesDirty();
            OnValueChanged?.Invoke(_isOn);
        }
    }

    /// <summary>Set state without firing OnValueChanged — use when restoring saved prefs.</summary>
    public void SetWithoutNotify(bool value)
    {
        _isOn  = value;
        _knobT = value ? 1f : 0f; // snap immediately, no animation
        SetVerticesDirty();
    }

    // ─── State ────────────────────────────────────────────────────────────────

    private bool  _hover = false;
    private float _knobT = 0f;   // 0 = off (left), 1 = on (right)
    private Color _currentBorder;
    private Color _currentBody;
    private Color _currentKnob;

    protected override void Awake()
    {
        base.Awake();
        _currentBorder = borderColor;
        _currentBody   = bodyOffColor;
        _currentKnob   = knobColor;
        _knobT         = _isOn ? 1f : 0f;
    }

    private void Update()
    {
        Color targetBorder = _hover ? borderHoverColor : borderColor;
        Color targetBody   = _isOn  ? bodyOnColor      : bodyOffColor;
        Color targetKnob   = _isOn  ? knobOnColor      : knobColor;
        float targetKnobT  = _isOn  ? 1f               : 0f;

        float dt = Time.unscaledDeltaTime * transitionSpeed;

        _currentBorder = Color.Lerp(_currentBorder, targetBorder, dt);
        _currentBody   = Color.Lerp(_currentBody,   targetBody,   dt);
        _currentKnob   = Color.Lerp(_currentKnob,   targetKnob,   dt);
        _knobT         = Mathf.Lerp(_knobT,         targetKnobT,  dt);

        SetVerticesDirty();

        if (labelText != null)
            labelText.color = Color.Lerp(labelText.color,
                _isOn ? labelActiveColor : labelColor, dt);
    }

    // ─── Pointer ──────────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData _)
    {
        _hover = true;
        sounds?.hoverEvent?.Post(gameObject);
    }

    public void OnPointerExit(PointerEventData _) => _hover = false;

    public void OnPointerClick(PointerEventData _)
    {
        IsOn = !_isOn;
        sounds?.clickEvent?.Post(gameObject);
    }

    // ─── Mesh ─────────────────────────────────────────────────────────────────

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;

        // Body
        UIShapeRenderer.DrawRoundedRect(vh, r, cornerRadius, _currentBody, cornerSegments);

        // Glow layers
        for (int i = 1; i <= glowLayers; i++)
        {
            float spread = glowSpread * i;
            Color gc = new Color(_currentBorder.r, _currentBorder.g,
                                 _currentBorder.b, glowAlpha / i);
            Rect gr = new Rect(r.x - spread, r.y - spread,
                               r.width + spread*2f, r.height + spread*2f);
            UIShapeRenderer.DrawRoundedRectBorder(vh, gr,
                cornerRadius + spread, borderWidth + spread * 0.5f, gc, cornerSegments);
        }

        // Border
        UIShapeRenderer.DrawRoundedRectBorder(vh, r, cornerRadius,
            borderWidth, _currentBorder, cornerSegments);

        // Sliding knob
        float knobSize   = r.height - knobPadding * 2f;
        float knobTravel = r.width  - knobPadding * 2f - knobSize;
        float knobX      = r.xMin  + knobPadding + _knobT * knobTravel;
        float knobY      = r.yMin  + knobPadding;
        float knobR      = knobSize * 0.5f;
        Vector2 knobCentre = new Vector2(knobX + knobR, knobY + knobR);

        DrawCircle(vh, knobCentre, knobR, _currentKnob, cornerSegments * 2);
    }

    private static void DrawCircle(VertexHelper vh, Vector2 centre,
                                   float radius, Color c, int segments)
    {
        float step = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float a0 = step * i       * Mathf.Deg2Rad;
            float a1 = step * (i + 1) * Mathf.Deg2Rad;
            int idx = vh.currentVertCount;
            UIShapeRenderer.AddVert(vh, centre, c);
            UIShapeRenderer.AddVert(vh, centre + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius, c);
            UIShapeRenderer.AddVert(vh, centre + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius, c);
            vh.AddTriangle(idx, idx+1, idx+2);
        }
    }
}
