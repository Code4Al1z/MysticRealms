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

        Rect rect = rectTransform.rect;
        float totalWidth = rect.width;
        float height = rect.height;
        float left = rect.xMin;
        float bottom = rect.yMin;
        float barWidth = (totalWidth - barGap * (barCount - 1)) / barCount;

        if (barWidth <= 0f) return;

        float minH = height * minHeightFraction;
        float maxH = height * maxHeightFraction;

        for (int i = 0; i < barCount; i++)
        {
            float t = (float)i / (barCount - 1);

            // Base sine wave
            float sine = Mathf.Sin(t * waveFrequency * Mathf.PI * 2f);
            // Idle noise offset — each bar has its own phase
            float noise = Mathf.Sin(noiseTimer + t * 7.3f) * idleNoiseAmount;
            float norm = Mathf.Clamp01((sine + 1f) * 0.5f + noise);
            float barH = Mathf.Lerp(minH, maxH, norm);

            bool active = t <= value;
            Color c = active ? activeColor : inactiveColor;

            float x = left + i * (barWidth + barGap);
            float y = bottom + (height - barH) * 0.5f;

            AddQuad(vh, x, y, barWidth, barH, c);
        }

        // Handle bar — vertical line at value position
        float handleX = left + value * (totalWidth - handleWidth);
        AddQuad(vh, handleX, bottom, handleWidth, height, handleColor);
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
