using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasRenderer))]
public class WaveformSlider : Graphic,
    IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Waveform Shape")]
    [SerializeField] private int barCount = 28;
    [SerializeField] private float barGap = 2f;
    [SerializeField] private float minHeightFraction = 0.2f;
    [SerializeField] private float maxHeightFraction = 0.85f;
    [SerializeField] private float waveFrequency = 2.2f;

    [Header("Idle Animation")]
    [SerializeField] private float idleNoiseSpeed = 0.8f;
    [SerializeField] private float idleNoiseAmount = 0.12f;

    [Header("Colours")]
    [SerializeField] private Color activeColor = new Color(0.35f, 0.80f, 1.00f);
    [SerializeField] private Color inactiveColor = new Color(0.12f, 0.22f, 0.30f);
    [SerializeField] private Color handleColor = new Color(0.85f, 0.95f, 1.00f);

    [Header("Handle")]
    [SerializeField] private float handleWidth = 4f;

    // ─── Public ───────────────────────────────────────────────────────────────

    public event System.Action<float> OnValueChanged;

    private float value = 1f;

    public void SetValue(float v)
    {
        value = Mathf.Clamp01(v);
        SetVerticesDirty();
    }

    public float Value => value;

    // ─── Runtime ──────────────────────────────────────────────────────────────

    private float noiseTimer = 0f;
    private bool isDragging = false;

    private void Update()
    {
        noiseTimer += Time.unscaledDeltaTime * idleNoiseSpeed;
        SetVerticesDirty();
    }

    // ─── Pointer / Drag ───────────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData e)
    {
        isDragging = true;
        UpdateFromPointer(e);
    }

    public void OnPointerUp(PointerEventData _)
    {
        isDragging = false;
    }

    public void OnDrag(PointerEventData e)
    {
        UpdateFromPointer(e);
    }

    private void UpdateFromPointer(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, e.position, e.pressEventCamera, out Vector2 local);

        float t = Mathf.InverseLerp(rectTransform.rect.xMin, rectTransform.rect.xMax, local.x);
        float prev = value;
        value = Mathf.Clamp01(t);

        if (!Mathf.Approximately(prev, value))
        {
            SetVerticesDirty();
            if (OnValueChanged != null) OnValueChanged.Invoke(value);
        }
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
            wavePhaseOffset = 0f,
            breatheTimer = noiseTimer,
            breatheAmount = idleNoiseAmount,
        };

        WaveformRenderer.DrawSolid(vh, rectTransform.rect, s, value, activeColor, inactiveColor);

        float handleX = rectTransform.rect.xMin + value * (rectTransform.rect.width - handleWidth);
        AddQuad(vh, handleX, rectTransform.rect.yMin, handleWidth, rectTransform.rect.height, handleColor);
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

        vh.AddTriangle(idx, idx+1, idx+2);
        vh.AddTriangle(idx+2, idx+3, idx);
    }
}
