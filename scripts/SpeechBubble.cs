using System.Collections.Generic;
using Godot;

public enum BubbleStyle { Speech, Shout, Thought }
public enum TailStyle { Straight, Curved, Uniform }   // Uniform = constant width, flat end (pointer / connector)
public enum TargetSpace { World, Screen }

public partial class SpeechBubble : Control
{
    private enum Side { Top, Right, Bottom, Left }

    private Vector2 _globalTarget;
    private BubbleStyle _style = BubbleStyle.Speech;
    private TailStyle _tail = TailStyle.Curved;
    private float _tailBend = 0.35f;
    private int _seed = 1;
    private float _roundness = 0f;
    private TargetSpace _space = TargetSpace.World;
    private Vector2 _lastLocalTarget = new(float.NaN, float.NaN);

    // ---------- Target ----------
    /// Optional: track a node instead of a raw position. Works from any canvas/layer.
    [Export] public CanvasItem TargetNode { get; set; }

    /// Offset in the TargetNode's local space (e.g. (0, -40) = above its origin).
    [Export] public Vector2 TargetOffset { get; set; } = Vector2.Zero;

    /// Raw target position, used when no TargetNode is set. Interpreted according to GlobalTargetSpace.
    [Export] public Vector2 GlobalTarget
    {
        get => _globalTarget;
        set { _globalTarget = value; QueueRedraw(); }
    }

    /// World = same as Node2D.GlobalPosition in the game world. Screen = viewport pixels (UI space).
    [Export] public TargetSpace GlobalTargetSpace
    {
        get => _space;
        set { _space = value; QueueRedraw(); }
    }

    // ---------- Common ----------
    [Export] public BubbleStyle Style
    {
        get => _style;
        set { _style = value; QueueRedraw(); }
    }

    // Changing the fields below at runtime? Call QueueRedraw() afterwards.
    [Export] public float BorderWidth = 2f;
    [Export] public Color FillColor = Colors.White;
    [Export] public Color BorderColor = Colors.Black;
    [Export] public bool Draggable = true;

    // ---------- Speech ----------
    [Export] public TailStyle Tail
    {
        get => _tail;
        set { _tail = value; QueueRedraw(); }
    }

    /// Curved tail only: sideways bend as a fraction of tail length. Negative flips direction.
    [Export] public float TailBend
    {
        get => _tailBend;
        set { _tailBend = value; QueueRedraw(); }
    }

    [Export] public float TailHalfWidth = 12f;
    [Export] public float CornerRadius = 10f;

    /// 0 = rounded rectangle (uses CornerRadius), 1 = full ellipse. In between = increasingly bubble-like.
    [Export(PropertyHint.Range, "0,1,0.05")] public float Roundness
    {
        get => _roundness;
        set { _roundness = value; QueueRedraw(); }
    }
    [Export] public int CurveSegments = 12;

    // ---------- Shout ----------
    [Export] public float SpikeWidth = 26f;      // approx. distance between spike tips (px)
    [Export] public float SpikeDepth = 14f;      // how far the valleys cut in (px)
    [Export(PropertyHint.Range, "0,1,0.05")] public float Jitter = 0.5f;

    /// Changes the random spike pattern. The pattern stays stable between redraws.
    [Export] public int ShoutSeed
    {
        get => _seed;
        set { _seed = value; QueueRedraw(); }
    }

    // ---------- Shout + Thought base shape ----------
    /// 2 = ellipse, higher = boxier (fills the corners so text fits better).
    [Export(PropertyHint.Range, "2,8,0.1")] public float Squareness = 3f;

    // ---------- Thought ----------
    [Export] public float BumpSize = 30f;        // approx. width of each cloud bump (px)
    [Export(PropertyHint.Range, "0.1,0.5,0.01")] public float BumpDepth = 0.4f;
    [Export] public int TrailCount = 3;
    [Export] public float TrailRadius = 10f;

    private bool _chained;

    /// Set automatically by the BubbleChain this bubble was added to. When true, this bubble draws no outline of its own
    /// (the chain draws one fused outline behind all its bubbles); text, size and dragging still work.
    public bool Chained
    {
        get => _chained;
        set { _chained = value; QueueRedraw(); NotifyChain(); }
    }

    internal BubbleChain Chain;   // set by the BubbleChain that owns this bubble

    private void NotifyChain()
    {
        if (_chained && Chain != null && IsInstanceValid(Chain))
            Chain.Invalidate();
    }

    private bool _dragging;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        SetNotifyTransform(true);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTransformChanged || what == NotificationResized || what == NotificationVisibilityChanged)
        {
            QueueRedraw();
            NotifyChain();
        }
    }

    public override void _Process(double delta)
    {
        if (_chained) return;   // the chain tracks the target

        // Catches target movement, camera movement, container re-layout, parent moves, etc.
        Vector2 local = GetTargetLocal();
        if (!local.IsEqualApprox(_lastLocalTarget))
        {
            _lastLocalTarget = local;
            QueueRedraw();
        }
    }

    public override void _GuiInput(InputEvent e)
    {
        if (!Draggable) return;

        if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
            _dragging = mb.Pressed;
        else if (e is InputEventMouseMotion mm && _dragging)
            Position += mm.Relative;
    }

    /// Converts the target (node or raw position, world or screen) into this control's local space,
    /// regardless of CanvasLayers, cameras, or parent containers.
    private Vector2 GetTargetLocal()
    {
        Vector2 viewportPos;

        if (TargetNode != null && IsInstanceValid(TargetNode))
            viewportPos = TargetNode.GetGlobalTransformWithCanvas() * TargetOffset;
        else if (_space == TargetSpace.World)
            viewportPos = GetViewport().GetCanvasTransform() * _globalTarget;
        else
            viewportPos = _globalTarget;

        // viewport space -> this control's local space
        return GetGlobalTransformWithCanvas().AffineInverse() * viewportPos;
    }

    public override void _Draw()
    {
        if (_chained) { NotifyChain(); return; }   // parent BubbleChain draws the outline

        var trail = new List<(Vector2 center, float radius)>();
        var pts = BuildOutline(GetTargetLocal(), true, BorderWidth, trail);
        if (pts == null || pts.Count < 3) return;

        FillStroke(pts);

        foreach (var (center, radius) in trail)
        {
            DrawCircle(center, radius, FillColor);
            DrawArc(center, radius, 0f, Mathf.Tau, 24, BorderColor, BorderWidth, true);
        }
    }

    /// Builds this bubble's outline in its own local space.
    /// targetLocal: the tail target in this control's local space. withTail: include the tail/spike/trail.
    /// trail: (thought style only) receives the trailing circles. Returns null if the control is too small.
    public List<Vector2> BuildOutline(Vector2 targetLocal, bool withTail, float borderWidth,
                                      List<(Vector2 center, float radius)> trail = null,
                                      TailStyle? tailOverride = null)
    {
        TailStyle tailStyle = tailOverride ?? _tail;
        // Inset by half the border so the stroke stays inside the control's rect
        float bh = borderWidth / 2f;
        var r = new Rect2(bh, bh, Size.X - borderWidth, Size.Y - borderWidth);
        if (r.Size.X < 4f || r.Size.Y < 4f) return null;

        Vector2 t = targetLocal;

        switch (_style)
        {
            case BubbleStyle.Speech:
            {
                bool hasTail = withTail && !new Rect2(Vector2.Zero, Size).HasPoint(t);
                var pts = Clean(BuildSpeech(r, t, hasTail, tailStyle));
                if (hasTail && !CanFill(pts))             // tail self-intersects: drop it this frame
                    pts = Clean(BuildSpeech(r, t, false, tailStyle));
                return pts;
            }
            case BubbleStyle.Shout:
            {
                var pts = Clean(BuildShout(r, t, withTail));
                if (withTail && !CanFill(pts))             // stretched spike crosses its neighbours
                    pts = Clean(BuildShout(r, t, false));
                return pts;
            }
            default:
                return Clean(BuildThought(r, t, withTail, trail));
        }
    }

    private void FillStroke(List<Vector2> pts)
    {
        pts = Clean(pts);
        if (pts.Count < 3) return;

        // Only fill if Godot can triangulate it, otherwise it logs an error every redraw
        if (CanFill(pts))
            DrawColoredPolygon(pts.ToArray(), FillColor);

        var closed = new List<Vector2>(pts) { pts[0] };
        DrawPolyline(closed.ToArray(), BorderColor, BorderWidth, true);
    }

    /// True if the outline can be triangulated (no near-duplicate points or bad self-intersections).
    internal static bool CanFill(List<Vector2> pts)
    {
        if (pts.Count < 3) return false;
        return Geometry2D.TriangulatePolygon(pts.ToArray()).Length > 0;
    }

    /// Removes consecutive (near-)duplicate points, including last vs. first.
    internal static List<Vector2> Clean(List<Vector2> pts)
    {
        const float minDistSq = 0.05f * 0.05f;
        var res = new List<Vector2>(pts.Count);
        foreach (var p in pts)
        {
            if (res.Count == 0 || res[res.Count - 1].DistanceSquaredTo(p) > minDistSq)
                res.Add(p);
        }
        while (res.Count > 1 && res[0].DistanceSquaredTo(res[res.Count - 1]) <= minDistSq)
            res.RemoveAt(res.Count - 1);
        return res;
    }

    // =====================================================================
    //  SPEECH: rounded rect with a straight or curved tail
    // =====================================================================
    private List<Vector2> BuildSpeech(Rect2 r, Vector2 t, bool hasTail, TailStyle tailStyle)
    {
        float maxRx = r.Size.X / 2f, maxRy = r.Size.Y / 2f;
        float rad = Mathf.Clamp(CornerRadius, 0f, Mathf.Min(maxRx, maxRy));
        float k = Mathf.Clamp(Roundness, 0f, 1f);
        Vector2 c = r.GetCenter();

        // ---- Base outline: rounded rectangle, clockwise on screen ----
        var outline = new List<Vector2>();

        void Arc(Vector2 center, float startDeg)
        {
            if (rad < 0.5f) { outline.Add(center); return; }
            const int seg = 12;
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.DegToRad(startDeg + 90f * i / seg);
                outline.Add(center + Vector2.FromAngle(a) * rad);
            }
        }

        // Extra points along straight edges so they can bend when blended toward the ellipse
        void Edge(Vector2 from, Vector2 to)
        {
            if (k <= 0f) return;
            int steps = Mathf.CeilToInt(from.DistanceTo(to) / 6f);
            for (int i = 1; i < steps; i++)
                outline.Add(from.Lerp(to, (float)i / steps));
        }

        float x0 = r.Position.X, y0 = r.Position.Y, x1 = r.End.X, y1 = r.End.Y;
        Arc(new Vector2(x0 + rad, y0 + rad), 180f); Edge(new Vector2(x0 + rad, y0), new Vector2(x1 - rad, y0));
        Arc(new Vector2(x1 - rad, y0 + rad), 270f); Edge(new Vector2(x1, y0 + rad), new Vector2(x1, y1 - rad));
        Arc(new Vector2(x1 - rad, y1 - rad),   0f); Edge(new Vector2(x1 - rad, y1), new Vector2(x0 + rad, y1));
        Arc(new Vector2(x0 + rad, y1 - rad),  90f); Edge(new Vector2(x0, y1 - rad), new Vector2(x0, y0 + rad));

        // ---- Roundness: slide every point along its line from the center toward the inscribed ellipse ----
        if (k > 0f && maxRx > 0.5f && maxRy > 0.5f)
        {
            for (int i = 0; i < outline.Count; i++)
            {
                Vector2 v = outline[i] - c;
                float len = v.Length();
                if (len < 1e-4f) continue;
                Vector2 u = v / len;
                float ex = u.X / maxRx, ey = u.Y / maxRy;
                float onEllipse = 1f / Mathf.Sqrt(ex * ex + ey * ey);
                outline[i] = c + u * Mathf.Lerp(len, onEllipse, k);
            }
        }
        outline = Clean(outline);

        if (!hasTail || outline.Count < 3) return outline;
        if (Geometry2D.IsPointInPolygon(t, outline.ToArray())) return outline; // target inside the bubble

        // ---- Attach the tail at the outline point nearest the target (tail leaves roughly perpendicular) ----
        int n = outline.Count;
        int hitEdge = -1;
        Vector2 hit = Vector2.Zero;
        float bestD = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = outline[i], b = outline[(i + 1) % n];
            Vector2 ab = b - a;
            float ll = ab.LengthSquared();
            float u = ll > 1e-8f ? Mathf.Clamp((t - a).Dot(ab) / ll, 0f, 1f) : 0f;
            Vector2 p = a + ab * u;
            float dd = p.DistanceSquaredTo(t);
            if (dd < bestD) { bestD = dd; hitEdge = i; hit = p; }
        }
        if (hitEdge < 0) return outline;

        // ---- Arc-length positions along the outline ----
        var cum = new float[n + 1];
        for (int i = 0; i < n; i++)
            cum[i + 1] = cum[i] + outline[i].DistanceTo(outline[(i + 1) % n]);
        float L = cum[n];

        float hw = Mathf.Min(TailHalfWidth, L * 0.2f);
        float sHit = cum[hitEdge] + outline[hitEdge].DistanceTo(hit);
        float s1 = sHit - hw;                 // tail base start (clockwise-first)
        float s2 = sHit + hw;                 // tail base end

        Vector2 PointAt(float s)
        {
            s = ((s % L) + L) % L;
            int seg = 0;
            while (seg < n - 1 && cum[seg + 1] < s) seg++;
            float len = cum[seg + 1] - cum[seg];
            float f = len > 1e-6f ? (s - cum[seg]) / len : 0f;
            return outline[seg].Lerp(outline[(seg + 1) % n], f);
        }

        Vector2 b1 = PointAt(s1), b2 = PointAt(s2);

        // ---- Outline vertices that survive (everything outside the tail base), starting after b2 ----
        var kept = new List<(float off, Vector2 p)>();
        for (int i = 0; i < n; i++)
        {
            float off = (((cum[i] - s2) % L) + L) % L;
            if (off > 0.01f && off < L - 2f * hw - 0.01f)
                kept.Add((off, outline[i]));
        }
        kept.Sort((x, y) => x.off.CompareTo(y.off));

        var pts = new List<Vector2>(kept.Count + 2 * CurveSegments + 3);
        foreach (var kv in kept) pts.Add(kv.p);

        // ---- Tail from b1 to the tip to b2 ----
        pts.Add(b1);
        if (tailStyle == TailStyle.Uniform)
        {
            // Parallel sides: the base is simply slid along to the target, ending in a flat cap centered on it
            Vector2 off = t - (b1 + b2) / 2f;
            pts.Add(b1 + off);
            pts.Add(b2 + off);
            pts.Add(b2);
        }
        else if (tailStyle == TailStyle.Curved)
        {
            Vector2 m = (b1 + b2) / 2f;
            Vector2 td = t - m;
            float len = td.Length();
            Vector2 perp = len > 0.001f ? new Vector2(-td.Y, td.X) / len : Vector2.Zero;
            Vector2 bend = perp * (_tailBend * len);

            void Quad(Vector2 qa, Vector2 ctrl, Vector2 qb)
            {
                for (int i = 1; i <= CurveSegments; i++)
                {
                    float u = (float)i / CurveSegments, v = 1f - u;
                    pts.Add(v * v * qa + 2f * u * v * ctrl + u * u * qb);
                }
            }

            Quad(b1, b1.Lerp(t, 0.5f) + bend, t);
            Quad(t, t.Lerp(b2, 0.5f) + bend, b2);
        }
        else
        {
            pts.Add(t);
            pts.Add(b2);
        }

        return pts;
    }

    // =====================================================================
    //  SHOUT: jagged starburst; the spike nearest the target stretches to it
    // =====================================================================
    private List<Vector2> BuildShout(Rect2 r, Vector2 t, bool stretchToTarget)
    {
        Vector2 c = r.GetCenter();
        float rx = r.Size.X / 2f, ry = r.Size.Y / 2f;
        float depth = Mathf.Min(SpikeDepth, Mathf.Min(rx, ry) * 0.7f);

        var fine = SuperShape(c, rx, ry, Squareness);
        int spikes = Mathf.Max(5, Mathf.RoundToInt(PolyLength(fine) / Mathf.Max(4f, SpikeWidth)));
        int n = spikes * 2;
        var ring = Resample(fine, n);

        var rng = new RandomNumberGenerator { Seed = (ulong)_seed };
        var pts = new List<Vector2>(n);

        for (int i = 0; i < n; i++)
        {
            Vector2 nrm = OutNormal(ring, i, c);
            Vector2 p = ring[i];

            if (i % 2 == 0)   // outer tip: pulled in a random amount so spikes vary in length
                pts.Add(p - nrm * (rng.Randf() * Jitter * depth * 0.6f));
            else              // valley
                pts.Add(p - nrm * (depth * (1f + (rng.Randf() - 0.5f) * Jitter)));
        }

        // Stretch the nearest outer spike out to the target
        Vector2 d = t - c;
        float k = SuperNorm(d, rx, ry, Squareness);
        if (stretchToTarget && k > 1.05f)
        {
            Vector2 edge = c + d / k;
            int best = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i += 2)
            {
                float dist = ring[i].DistanceSquaredTo(edge);
                if (dist < bestDist) { bestDist = dist; best = i; }
            }
            pts[best] = t;
        }

        return pts;
    }

    // =====================================================================
    //  THOUGHT: scalloped cloud + shrinking circles trailing to the target
    // =====================================================================
    private List<Vector2> BuildThought(Rect2 r, Vector2 t, bool withTail, List<(Vector2 center, float radius)> trail)
    {
        Vector2 c = r.GetCenter();
        float bump = Mathf.Max(6f, BumpSize);
        float hFrac = Mathf.Clamp(BumpDepth, 0.1f, 0.5f);
        float inset = bump * hFrac;                       // roughly how far bumps stick out
        float rx = Mathf.Max(2f, r.Size.X / 2f - inset);
        float ry = Mathf.Max(2f, r.Size.Y / 2f - inset);

        var fine = SuperShape(c, rx, ry, Squareness);
        int bumps = Mathf.Max(5, Mathf.RoundToInt(PolyLength(fine) / bump));
        var ring = Resample(fine, bumps);

        var pts = new List<Vector2>();
        const int seg = 8;

        for (int i = 0; i < bumps; i++)
        {
            Vector2 P = ring[i], Q = ring[(i + 1) % bumps];
            Vector2 e = Q - P;
            float L = e.Length();
            if (L < 0.001f) continue;

            Vector2 M = (P + Q) / 2f;
            Vector2 nrm = new Vector2(e.Y, -e.X) / L;
            float sweep = 1f;
            if (nrm.Dot(M - c) < 0f) { nrm = -nrm; sweep = -1f; }

            // Circular arc through P and Q that bulges outward by h
            float h = L * hFrac;
            float R = (L * L / 4f + h * h) / (2f * h);
            Vector2 C = M + nrm * (h - R);
            float phi = Mathf.Asin(Mathf.Min(1f, L / 2f / R));
            float a0 = nrm.Angle();

            for (int s = 0; s < seg; s++)
            {
                float a = a0 + sweep * (2f * s / seg - 1f) * phi;
                pts.Add(C + Vector2.FromAngle(a) * R);
            }
        }

        // Thought trail: shrinking circles from the cloud toward the target
        if (withTail && trail != null && TrailCount > 0)
        {
            Vector2 d = t - c;
            float kk = SuperNorm(d, rx, ry, Squareness);
            if (kk > 1.02f)
            {
                Vector2 dir = (t - (c + d / kk)).Normalized();
                Vector2 start = c + d / kk + dir * inset;          // just outside the bumps
                float D = start.DistanceTo(t);
                if (D >= 6f)
                {
                    float scale = Mathf.Min(1f, D / (TrailRadius * 2.5f * TrailCount));
                    for (int i = 0; i < TrailCount; i++)
                    {
                        float rr = Mathf.Max(1.5f, TrailRadius * Mathf.Pow(0.65f, i) * scale);
                        trail.Add((start + dir * (D * (i + 0.5f) / TrailCount), rr));
                    }
                }
            }
        }

        return pts;
    }

    // =====================================================================
    //  Geometry helpers
    // =====================================================================

    /// Superellipse outline (clockwise on screen). squareness 2 = ellipse.
    private static List<Vector2> SuperShape(Vector2 c, float rx, float ry, float squareness, int count = 360)
    {
        float e = 2f / Mathf.Max(2f, squareness);
        var pts = new List<Vector2>(count);
        for (int i = 0; i < count; i++)
        {
            float a = Mathf.Tau * i / count;
            float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
            float x = Mathf.Sign(cs) * Mathf.Pow(Mathf.Abs(cs), e);
            float y = Mathf.Sign(sn) * Mathf.Pow(Mathf.Abs(sn), e);
            pts.Add(c + new Vector2(rx * x, ry * y));
        }
        return pts;
    }

    /// >1 means the offset d (from the center) lies outside the superellipse.
    private static float SuperNorm(Vector2 d, float rx, float ry, float squareness)
    {
        float n = Mathf.Max(2f, squareness);
        return Mathf.Pow(Mathf.Pow(Mathf.Abs(d.X / rx), n) + Mathf.Pow(Mathf.Abs(d.Y / ry), n), 1f / n);
    }

    private static float PolyLength(List<Vector2> loop)
    {
        float total = 0f;
        for (int i = 0; i < loop.Count; i++)
            total += loop[i].DistanceTo(loop[(i + 1) % loop.Count]);
        return total;
    }

    /// Picks n points spaced evenly along the closed loop's length.
    private static List<Vector2> Resample(List<Vector2> loop, int n)
    {
        int m = loop.Count;
        var cum = new float[m + 1];
        for (int i = 0; i < m; i++)
            cum[i + 1] = cum[i] + loop[i].DistanceTo(loop[(i + 1) % m]);
        float total = cum[m];

        var res = new List<Vector2>(n);
        int seg = 0;
        for (int k = 0; k < n; k++)
        {
            float target = total * k / n;
            while (seg < m - 1 && cum[seg + 1] < target) seg++;
            float len = cum[seg + 1] - cum[seg];
            float f = len > 1e-6f ? (target - cum[seg]) / len : 0f;
            res.Add(loop[seg].Lerp(loop[(seg + 1) % m], f));
        }
        return res;
    }

    private static Vector2 OutNormal(List<Vector2> ring, int i, Vector2 center)
    {
        int n = ring.Count;
        Vector2 tan = ring[(i + 1) % n] - ring[(i - 1 + n) % n];
        Vector2 nrm = new Vector2(tan.Y, -tan.X).Normalized();
        return nrm.Dot(ring[i] - center) < 0f ? -nrm : nrm;
    }
}
