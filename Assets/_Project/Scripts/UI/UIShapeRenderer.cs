using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared static drawing utilities used by DisplayToggle and DisplayDropdown.
/// No MonoBehaviour — pure helper methods.
/// </summary>
public static class UIShapeRenderer
{
    public static void DrawRect(VertexHelper vh, Rect r, Color c)
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

    public static void DrawRoundedRect(VertexHelper vh, Rect r,
                                       float radius, Color c, int segments = 8)
    {
        radius = Mathf.Min(radius, r.width * 0.5f, r.height * 0.5f);
        DrawRect(vh, new Rect(r.x + radius, r.y + radius,
                              r.width - radius*2f, r.height - radius*2f), c);
        DrawRect(vh, new Rect(r.x + radius, r.y,
                              r.width - radius*2f, radius), c);
        DrawRect(vh, new Rect(r.x + radius, r.yMax - radius,
                              r.width - radius*2f, radius), c);
        DrawRect(vh, new Rect(r.x, r.y + radius,
                              radius, r.height - radius*2f), c);
        DrawRect(vh, new Rect(r.xMax - radius, r.y + radius,
                              radius, r.height - radius*2f), c);
        DrawCornerFan(vh, new Vector2(r.x + radius,    r.y + radius),    radius, 180f, c, segments);
        DrawCornerFan(vh, new Vector2(r.xMax - radius, r.y + radius),    radius, 270f, c, segments);
        DrawCornerFan(vh, new Vector2(r.xMax - radius, r.yMax - radius), radius,   0f, c, segments);
        DrawCornerFan(vh, new Vector2(r.x + radius,    r.yMax - radius), radius,  90f, c, segments);
    }

    public static void DrawRoundedRectBorder(VertexHelper vh, Rect r,
                                             float radius, float bw, Color c,
                                             int segments = 8)
    {
        radius = Mathf.Min(radius, r.width * 0.5f, r.height * 0.5f);
        DrawRect(vh, new Rect(r.x + radius,    r.y,         r.width - radius*2f, bw), c);
        DrawRect(vh, new Rect(r.x + radius,    r.yMax - bw, r.width - radius*2f, bw), c);
        DrawRect(vh, new Rect(r.x,             r.y + radius, bw, r.height - radius*2f), c);
        DrawRect(vh, new Rect(r.xMax - bw,     r.y + radius, bw, r.height - radius*2f), c);
        DrawCornerArc(vh, new Vector2(r.x + radius,    r.y + radius),    radius, bw, 180f, c, segments);
        DrawCornerArc(vh, new Vector2(r.xMax - radius, r.y + radius),    radius, bw, 270f, c, segments);
        DrawCornerArc(vh, new Vector2(r.xMax - radius, r.yMax - radius), radius, bw,   0f, c, segments);
        DrawCornerArc(vh, new Vector2(r.x + radius,    r.yMax - radius), radius, bw,  90f, c, segments);
    }

    private static void DrawCornerFan(VertexHelper vh, Vector2 centre,
                                      float radius, float startDeg, Color c, int segments)
    {
        float step = 90f / segments;
        for (int i = 0; i < segments; i++)
        {
            float a0 = (startDeg + step * i)       * Mathf.Deg2Rad;
            float a1 = (startDeg + step * (i + 1)) * Mathf.Deg2Rad;
            int idx = vh.currentVertCount;
            AddVert(vh, centre, c);
            AddVert(vh, centre + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius, c);
            AddVert(vh, centre + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius, c);
            vh.AddTriangle(idx, idx+1, idx+2);
        }
    }

    private static void DrawCornerArc(VertexHelper vh, Vector2 centre,
                                      float radius, float bw, float startDeg,
                                      Color c, int segments)
    {
        float step   = 90f / segments;
        float innerR = radius - bw;
        for (int i = 0; i < segments; i++)
        {
            float a0 = (startDeg + step * i)       * Mathf.Deg2Rad;
            float a1 = (startDeg + step * (i + 1)) * Mathf.Deg2Rad;
            Vector2 o0 = centre + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
            Vector2 o1 = centre + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
            Vector2 i0 = centre + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * innerR;
            Vector2 i1 = centre + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * innerR;
            int idx = vh.currentVertCount;
            AddVert(vh, i0, c); AddVert(vh, o0, c);
            AddVert(vh, o1, c); AddVert(vh, i1, c);
            vh.AddTriangle(idx, idx+1, idx+2);
            vh.AddTriangle(idx+2, idx+3, idx);
        }
    }

    public static void AddVert(VertexHelper vh, Vector2 pos, Color c)
    {
        UIVertex v = UIVertex.simpleVert;
        v.position = pos;
        v.color    = c;
        vh.AddVert(v);
    }
}
