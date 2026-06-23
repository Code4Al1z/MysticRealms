using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Custom dropdown matching the MenuButton aesthetic.
/// Option rows use a dedicated OptionRow MonoBehaviour instead of EventTrigger,
/// which was silently losing its triggers list after AddComponent.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class DisplayDropdown : Graphic,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Colours")]
    [SerializeField] private Color bodyColor = new Color(0.05f, 0.08f, 0.18f, 0.72f);
    [SerializeField] private Color borderColor = new Color(0.25f, 0.75f, 0.85f, 0.85f);
    [SerializeField] private Color borderHoverColor = new Color(0.45f, 0.90f, 1.00f, 1.00f);
    [SerializeField] private Color optionBodyColor = new Color(0.04f, 0.07f, 0.16f, 0.92f);
    [SerializeField] private Color optionHoverColor = new Color(0.08f, 0.18f, 0.30f, 0.95f);
    [SerializeField] private Color shimmerColor = new Color(1f, 1f, 1f, 0.08f);

    [Header("Shape")]
    [SerializeField] private float cornerRadius = 10f;
    [SerializeField] private float borderWidth = 2f;
    [SerializeField] private int cornerSegments = 8;

    [Header("Glow")]
    [SerializeField] private int glowLayers = 3;
    [SerializeField] private float glowSpread = 4f;
    [SerializeField] private float glowAlpha = 0.18f;

    [Header("Label & Arrow")]
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Color labelColor = new Color(0.92f, 0.96f, 1.00f);
    [SerializeField] private Color labelHoverColor = new Color(1.00f, 1.00f, 1.00f);
    [SerializeField] private float arrowSize = 8f;

    [Header("Options Panel")]
    [Tooltip("RectTransform child anchored below this rect. Add a Canvas component with Override Sorting ON.")]
    [SerializeField] private RectTransform optionContainer;
    [SerializeField] private float optionHeight = 40f;
    [SerializeField] private TMP_FontAsset optionFont;

    [Header("Transitions")]
    [SerializeField] private float transitionSpeed = 8f;

    [Header("Wwise")]
    [SerializeField] private MenuButtonSounds sounds;

    // ─── Public API ───────────────────────────────────────────────────────────

    public event System.Action<int> OnOptionSelected;

    private List<string> _options = new();
    private int _selectedIndex = 0;
    private bool _isOpen = false;

    public int SelectedIndex => _selectedIndex;
    public string SelectedOption => _options.Count > 0 ? _options[_selectedIndex] : "";

    public void SetOptions(List<string> options, int selectedIndex = 0)
    {
        _options = options;
        _selectedIndex = Mathf.Clamp(selectedIndex, 0, options.Count - 1);
        UpdateLabel();
        RebuildOptionPanel();
        SetVerticesDirty();
        Debug.Log($"[DisplayDropdown] SetOptions: {options.Count} options, " +
                  $"selected {_selectedIndex} (\"{SelectedOption}\")");
    }

    public void SelectWithoutNotify(int index)
    {
        _selectedIndex = Mathf.Clamp(index, 0, _options.Count - 1);
        UpdateLabel();
        SetVerticesDirty();
    }

    // ─── Internal selection — called by OptionRow ─────────────────────────────

    internal void SelectOption(int index, string label)
    {
        Debug.Log($"[DisplayDropdown] SelectOption({index}) \"{label}\"");

        _selectedIndex = index;
        _isOpen = false;

        if (optionContainer != null)
            optionContainer.gameObject.SetActive(false);

        if (labelText != null)
            labelText.gameObject.SetActive(true);

        UpdateLabel();
        SetVerticesDirty();
        OnOptionSelected?.Invoke(index);
        sounds?.clickEvent?.Post(gameObject);

        Debug.Log($"[DisplayDropdown] Label updated to \"{SelectedOption}\"");
    }

    // ─── State ────────────────────────────────────────────────────────────────

    private bool _hover = false;
    private Color _currentBorder;
    private float _arrowAngle = 0f;

    private readonly List<GameObject> _optionRows = new();

    protected override void Awake()
    {
        base.Awake();
        _currentBorder = borderColor;

        if (labelText != null)
        {
            labelText.raycastTarget = false;
            Debug.Log("[DisplayDropdown] Awake: labelText raycastTarget = false");
        }
        else
        {
            Debug.LogWarning("[DisplayDropdown] Awake: labelText not assigned.");
        }

        if (optionContainer != null)
            optionContainer.gameObject.SetActive(false);
        else
            Debug.LogWarning("[DisplayDropdown] Awake: optionContainer not assigned.");
    }

    private void Update()
    {
        Color targetBorder = _hover ? borderHoverColor : borderColor;
        float dt = Time.unscaledDeltaTime * transitionSpeed;

        _currentBorder = Color.Lerp(_currentBorder, targetBorder, dt);
        _arrowAngle = Mathf.Lerp(_arrowAngle, _isOpen ? 180f : 0f, dt);

        SetVerticesDirty();

        if (labelText != null)
            labelText.color = Color.Lerp(labelText.color,
                _hover ? labelHoverColor : labelColor, dt);
    }

    // ─── Pointer (dropdown button itself) ─────────────────────────────────────

    public void OnPointerEnter(PointerEventData _)
    {
        _hover = true;
        sounds?.hoverEvent?.Post(gameObject);
    }

    public void OnPointerExit(PointerEventData _) => _hover = false;

    public void OnPointerClick(PointerEventData e)
    {
        _isOpen = !_isOpen;
        Debug.Log($"[DisplayDropdown] Clicked — panel is now {(_isOpen ? "OPEN" : "CLOSED")}");

        if (optionContainer != null)
            optionContainer.gameObject.SetActive(_isOpen);

        if (labelText != null)
            labelText.gameObject.SetActive(!_isOpen);

        sounds?.clickEvent?.Post(gameObject);
    }

    // ─── Option panel ─────────────────────────────────────────────────────────

    private void RebuildOptionPanel()
    {
        if (optionContainer == null)
        {
            Debug.LogWarning("[DisplayDropdown] RebuildOptionPanel: optionContainer is null.");
            return;
        }

        foreach (var row in _optionRows) Destroy(row);
        _optionRows.Clear();

        optionContainer.sizeDelta = new Vector2(
            optionContainer.sizeDelta.x, optionHeight * _options.Count);

        for (int i = 0; i < _options.Count; i++)
        {
            // ── Row root ──────────────────────────────────────────────────────
            var row = new GameObject($"Option_{i}", typeof(RectTransform));
            row.transform.SetParent(optionContainer, false);

            var rowRT = row.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0, 1);
            rowRT.anchorMax = new Vector2(1, 1);
            rowRT.pivot = new Vector2(0, 1);
            rowRT.anchoredPosition = new Vector2(0, -optionHeight * i);
            rowRT.sizeDelta = new Vector2(0, optionHeight);

            // ── Background — raycast target, receives all clicks ───────────────
            var bg = row.AddComponent<Image>();
            bg.color = optionBodyColor;
            bg.raycastTarget = true;

            // ── Label — must NOT intercept raycasts ────────────────────────────
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(row.transform, false);
            var lRT = labelGO.GetComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero;
            lRT.anchorMax = Vector2.one;
            lRT.offsetMin = new Vector2(12, 0);
            lRT.offsetMax = new Vector2(-12, 0);

            var lbl = labelGO.AddComponent<TextMeshProUGUI>();
            if (optionFont != null) lbl.font = optionFont;
            lbl.text = _options[i];
            lbl.color = labelColor;
            lbl.fontSize = 16;
            lbl.alignment = TextAlignmentOptions.MidlineLeft;
            lbl.raycastTarget = false;

            // ── OptionRow MonoBehaviour — owns the pointer logic ───────────────
            // Added LAST so Awake fires after Image and TMP are already on the GO.
            var optRow = row.AddComponent<OptionRow>();
            optRow.Initialise(i, _options[i], bg, optionBodyColor, optionHoverColor, this);

            _optionRows.Add(row);

            Debug.Log($"[DisplayDropdown] Built Option_{i}: \"{_options[i]}\"  " +
                      $"pos:{rowRT.anchoredPosition}  size:{rowRT.sizeDelta}");
        }
    }

    private void UpdateLabel()
    {
        if (labelText != null && _options.Count > 0)
        {
            labelText.text = _options[_selectedIndex];
            Debug.Log($"[DisplayDropdown] UpdateLabel → \"{labelText.text}\"");
        }
    }

    // ─── Mesh ─────────────────────────────────────────────────────────────────

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;

        UIShapeRenderer.DrawRoundedRect(vh, r, cornerRadius, bodyColor, cornerSegments);

        for (int i = 1; i <= glowLayers; i++)
        {
            float spread = glowSpread * i;
            Color gc = new Color(_currentBorder.r, _currentBorder.g,
                                 _currentBorder.b, glowAlpha / i);
            Rect gr = new Rect(r.x - spread, r.y - spread,
                               r.width + spread * 2f, r.height + spread * 2f);
            UIShapeRenderer.DrawRoundedRectBorder(vh, gr,
                cornerRadius + spread, borderWidth + spread * 0.5f, gc, cornerSegments);
        }

        UIShapeRenderer.DrawRoundedRectBorder(vh, r, cornerRadius,
            borderWidth, _currentBorder, cornerSegments);

        float shimmerH = r.height * 0.28f;
        UIShapeRenderer.DrawRect(vh,
            new Rect(r.x + cornerRadius, r.yMax - shimmerH,
                     r.width - cornerRadius * 2f, shimmerH * 0.3f), shimmerColor);

        DrawArrow(vh, r);
    }

    private void DrawArrow(VertexHelper vh, Rect r)
    {
        float cx = r.xMax - arrowSize * 2.5f;
        float cy = r.center.y;
        float half = arrowSize * 0.5f;

        Vector2[] pts =
        {
            new Vector2(-half, -half * 0.5f),
            new Vector2( half, -half * 0.5f),
            new Vector2( 0,     half * 0.6f),
        };

        float rad = _arrowAngle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        int idx = vh.currentVertCount;
        foreach (var p in pts)
        {
            float rx = p.x * cos - p.y * sin;
            float ry = p.x * sin + p.y * cos;
            UIShapeRenderer.AddVert(vh, new Vector2(cx + rx, cy + ry), _currentBorder);
        }
        vh.AddTriangle(idx, idx + 1, idx + 2);
    }
}


/// <summary>
/// Attached to each option row. Implements pointer interfaces directly —
/// avoids the EventTrigger issue where triggers are lost after AddComponent.
/// </summary>
[RequireComponent(typeof(Image))]
public class OptionRow : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private int _index;
    private string _label;
    private Image _bg;
    private Color _normalColor;
    private Color _hoverColor;
    private DisplayDropdown _owner;

    public void Initialise(int index, string label, Image bg,
                           Color normalColor, Color hoverColor,
                           DisplayDropdown owner)
    {
        _index = index;
        _label = label;
        _bg = bg;
        _normalColor = normalColor;
        _hoverColor = hoverColor;
        _owner = owner;

        Debug.Log($"[OptionRow] Initialised index:{_index} label:\"{_label}\"");
    }

    public void OnPointerClick(PointerEventData e)
    {
        Debug.Log($"[OptionRow] OnPointerClick index:{_index} \"{_label}\"  pos:{e.position}");
        _owner.SelectOption(_index, _label);
    }

    public void OnPointerEnter(PointerEventData _)
    {
        Debug.Log($"[OptionRow] OnPointerEnter index:{_index}");
        if (_bg != null) _bg.color = _hoverColor;
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (_bg != null) _bg.color = _normalColor;
    }
}