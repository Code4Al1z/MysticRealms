using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasRenderer))]
public class AbilitySlotUI : Graphic
{
    [Header("Child References")]
    [SerializeField] private Image symbolImage;
    [SerializeField] private TMP_Text keyBadgeText;
    [SerializeField] private TMP_Text rechargeTimerText;

    [Header("Background")]
    [SerializeField] private Color bgColor = new Color(0.05f, 0.08f, 0.14f, 0.88f);
    [SerializeField] private Color bgLockedColor = new Color(0.04f, 0.04f, 0.08f, 0.92f);

    [Header("Ring Shape")]
    [SerializeField] private float ringWidth = 6f;
    [SerializeField] private int ringSegments = 56;

    [Header("Glow")]
    [SerializeField] private int glowLayers = 3;
    [SerializeField] private float glowSpread = 3.5f;

    [Header("Transition Speed")]
    [SerializeField] private float transitionSpeed = 7f;

    private float energy = 1f;
    private bool isLocked = true;
    private bool isActive = false;
    private float activeGlow = 0f;

    // ─── Public API — same signatures as original ─────────────────────────────

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (symbolImage != null)
            symbolImage.color = locked ? new Color(0.35f, 0.32f, 0.42f, 1f) : Color.white;

        if (keyBadgeText != null)
            keyBadgeText.color = locked ? new Color(0.40f, 0.38f, 0.50f, 1f) : Color.white;

        SetVerticesDirty();
    }

    public void SetEnergy(float normalised)
    {
        energy = Mathf.Clamp01(normalised);
        SetVerticesDirty();
        SyncRing();
    }

    public void SetActive(bool active)
    {
        isActive = active;
        SetVerticesDirty();
        SyncRing();
    }

    public void SetRechargeTimer(float secondsRemaining)
    {
        if (rechargeTimerText == null) return;
        rechargeTimerText.gameObject.SetActive(secondsRemaining > 0f);
        rechargeTimerText.text = secondsRemaining > 0f ? $"{secondsRemaining:F1}s" : string.Empty;
    }

    public void SetIcon(Sprite sprite)
    {
        if (symbolImage != null) symbolImage.sprite = sprite;
    }

    // ─── Animation ────────────────────────────────────────────────────────────

    private void Update()
    {
        float target = isActive && !isLocked ? 1f : 0f;
        float prev = activeGlow;
        activeGlow = Mathf.Lerp(activeGlow, target, Time.deltaTime * transitionSpeed);
        if (!Mathf.Approximately(prev, activeGlow))
        {
            SetVerticesDirty();
            SyncRing();
        }
    }

    // ─── Mesh generation ──────────────────────────────────────────────────────

    [Header("Ring Child")]
    [Tooltip("Assign a child GameObject that sits above the symbol image in the hierarchy.")]
    [SerializeField] private AbilityRingUI ringUI;

    public void SetRingReference(AbilityRingUI ring) { ringUI = ring; }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r = rectTransform.rect;
        float cx = r.center.x;
        float cy = r.center.y;
        float radius = Mathf.Min(r.width, r.height) * 0.5f - 1f;

        // Background disc only — ring is drawn by AbilityRingUI child
        Color bg = isLocked ? bgLockedColor : bgColor;
        DrawDisc(vh, cx, cy, radius - ringWidth, bg);
    }

    private void SyncRing()
    {
        if (ringUI == null) return;
        ringUI.UpdateState(energy, isLocked, activeGlow);
    }

    // ─── Drawing helpers ──────────────────────────────────────────────────────

    private void DrawDisc(VertexHelper vh, float cx, float cy, float radius, Color c)
    {
        if (radius <= 0f) return;
        float step = 360f / ringSegments;
        for (int i = 0; i < ringSegments; i++)
        {
            float a0 = step * i * Mathf.Deg2Rad;
            float a1 = step * (i + 1) * Mathf.Deg2Rad;
            int idx = vh.currentVertCount;
            AddVert(vh, new Vector2(cx, cy), c);
            AddVert(vh, new Vector2(cx + Mathf.Cos(a0) * radius, cy + Mathf.Sin(a0) * radius), c);
            AddVert(vh, new Vector2(cx + Mathf.Cos(a1) * radius, cy + Mathf.Sin(a1) * radius), c);
            vh.AddTriangle(idx, idx + 1, idx + 2);
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