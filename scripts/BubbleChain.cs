using System.Collections.Generic;
using Godot;

/// <summary>
/// Fuses a list of SpeechBubbles into ONE comic-style outline (overlaps disappear, one border around the
/// whole blob). Each bubble gets a tail pointing at the next bubble in the list; the last one points at
/// the target (the character). Fill <see cref="Bubbles"/> in the order the bubbles should chain.
///
/// Bubbles do NOT need to be children of the chain. Draw order: place the chain BEFORE the bubbles in the
/// scene tree (same canvas layer), so the fused outline is drawn behind their text.
/// A bubble can belong to one chain at a time.
/// </summary>
public partial class BubbleChain : Control
{
    private sealed class Item
    {
        public SpeechBubble B;
        public Transform2D Xf;      // bubble-local -> chain-local
        public Vector2 Center;      // bubble center in chain-local space
        public Vector2[] Base;      // tail-less outline in chain-local space
    }

    private Vector2 _globalTarget;
    private TargetSpace _space = TargetSpace.World;
    private Vector2 _lastLocalTarget = new(float.NaN, float.NaN);
    private Color _fill = Colors.White;
    private Color _border = Colors.Black;
    private float _borderWidth = 2f;
    private float _linkOverlap = 24f;
    private bool _uniformLinks = true;
    private bool _finalTail = true;
    private List<SpeechBubble> _bubbles = new();
    private readonly List<SpeechBubble> _tracked = new();

    [Export] private Godot.Collections.Array<SpeechBubble> startingBubbles = new Godot.Collections.Array<SpeechBubble> { };

    // ---------- The chain ----------
    /// Bubbles in reading order. Add/remove/reorder freely at any time; the chain picks up changes
    /// automatically (at the latest next frame). Hidden bubbles are skipped, so you can add them all up
    /// front and reveal them one by one with Visible = true.
    public List<SpeechBubble> Bubbles
    {
        get => _bubbles;
        set { _bubbles = value ?? new List<SpeechBubble>(); SyncBubbles(); }
    }

    public void AddBubble(SpeechBubble b)
    {
        if (b != null && !_bubbles.Contains(b)) _bubbles.Add(b);
        SyncBubbles();
    }

    public void RemoveBubble(SpeechBubble b)
    {
        _bubbles.Remove(b);
        SyncBubbles();
    }

    public void ClearBubbles()
    {
        _bubbles.Clear();
        SyncBubbles();
    }

    // ---------- Target of the LAST bubble (same options as SpeechBubble) ----------
    [Export] public CanvasItem TargetNode { get; set; }
    [Export] public Vector2 TargetOffset { get; set; } = Vector2.Zero;

    [Export]
    public Vector2 GlobalTarget
    {
        get => _globalTarget;
        set { _globalTarget = value; Invalidate(); }
    }

    [Export]
    public TargetSpace GlobalTargetSpace
    {
        get => _space;
        set { _space = value; Invalidate(); }
    }

    /// Turn off to give the last bubble no tail (e.g. narration or an off-screen speaker).
    [Export]
    public bool FinalTail
    {
        get => _finalTail;
        set { _finalTail = value; Invalidate(); }
    }

    /// How far (px) the tail between two bubbles reaches into the next bubble. Needs to be > 0 so the
    /// tail overlaps the next bubble and fuses with it.
    [Export]
    public float LinkOverlap
    {
        get => _linkOverlap;
        set { _linkOverlap = value; Invalidate(); }
    }

    /// Tails between bubbles keep a uniform width (parallel sides) instead of tapering to a point.
    /// Applies to speech-style bubbles; shout and thought bubbles keep their own spike/trail.
    [Export]
    public bool UniformLinks
    {
        get => _uniformLinks;
        set { _uniformLinks = value; Invalidate(); }
    }

    // ---------- Look (shared by the whole chain) ----------
    [Export]
    public Color FillColor
    {
        get => _fill;
        set { _fill = value; Invalidate(); }
    }

    [Export]
    public Color BorderColor
    {
        get => _border;
        set { _border = value; Invalidate(); }
    }

    [Export]
    public float BorderWidth
    {
        get => _borderWidth;
        set { _borderWidth = value; Invalidate(); }
    }

    public void Invalidate() => QueueRedraw();

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        foreach (SpeechBubble bubble in startingBubbles)
        {
            AddBubble(bubble);
        }
        SetNotifyTransform(true);
    }

    public override void _ExitTree()
    {
        foreach (var b in _tracked)
            Release(b);
        _tracked.Clear();   // re-adopted automatically if the chain re-enters the tree
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTransformChanged)
            QueueRedraw();
    }

    public override void _Process(double delta)
    {
        SyncBubbles();

        // Catches target movement, camera movement, parent moves, etc.
        Vector2 local = GetTargetLocal();
        if (!local.IsEqualApprox(_lastLocalTarget))
        {
            _lastLocalTarget = local;
            QueueRedraw();
        }
    }

    // =====================================================================
    //  Keeping the list and the bubbles in sync
    // =====================================================================
    private void SyncBubbles()
    {
        _bubbles.RemoveAll(b => b == null || !IsInstanceValid(b));

        bool same = _bubbles.Count == _tracked.Count;
        for (int i = 0; same && i < _bubbles.Count; i++)
            same = ReferenceEquals(_bubbles[i], _tracked[i]);
        if (same) return;

        foreach (var old in _tracked)
            if (!_bubbles.Contains(old)) Release(old);

        foreach (var b in _bubbles)
            Adopt(b);

        _tracked.Clear();
        _tracked.AddRange(_bubbles);
        Invalidate();
    }

    private void Adopt(SpeechBubble b)
    {
        b.Chain = this;
        b.Chained = true;    // the bubble stops drawing its own outline
    }

    private void Release(SpeechBubble b)
    {
        if (b == null || !IsInstanceValid(b)) return;
        if (b.Chain == this)
        {
            b.Chained = false;   // draws itself again, independent
            b.Chain = null;
        }
    }

    private Vector2 GetTargetLocal()
    {
        Vector2 viewportPos;

        if (TargetNode != null && IsInstanceValid(TargetNode))
            viewportPos = TargetNode.GetGlobalTransformWithCanvas() * TargetOffset;
        else if (_space == TargetSpace.World)
            viewportPos = GetViewport().GetCanvasTransform() * _globalTarget;
        else
            viewportPos = _globalTarget;

        return GetGlobalTransformWithCanvas().AffineInverse() * viewportPos;
    }

    // =====================================================================
    //  Drawing
    // =====================================================================
    public override void _Draw()
    {
        Transform2D toChain = GetGlobalTransformWithCanvas().AffineInverse();

        // Visible bubbles with a usable size, in list order, with their tail-less outlines
        var items = new List<Item>();
        foreach (var b in _bubbles)
        {
            if (!b.Visible || b.Size.X < 8f || b.Size.Y < 8f) continue;

            Transform2D xf = toChain * b.GetGlobalTransformWithCanvas();
            if (Mathf.Abs(xf.Determinant()) < 1e-6f) continue;      // e.g. scale 0 at the start of a pop-in

            var baseOutline = ToChainSpace(b.BuildOutline(Vector2.Zero, false, _borderWidth), xf);
            if (baseOutline == null) continue;

            var dup = false;
            foreach (var it in items) if (it.B == b) { dup = true; break; }
            if (dup) continue;

            items.Add(new Item { B = b, Xf = xf, Center = xf * (b.Size / 2f), Base = baseOutline });
        }

        int n = items.Count;
        if (n == 0) return;

        Vector2 finalTarget = GetTargetLocal();

        var polys = new List<Vector2[]>();
        var trails = new List<(Vector2 center, float radius)>();

        for (int i = 0; i < n; i++)
        {
            var it = items[i];
            bool isLast = i == n - 1;
            bool withTail = !isLast || _finalTail;

            // Each bubble points at the next one (aiming a little way INTO it); the last one at the target
            Vector2 target = isLast ? finalTarget : LinkTarget(it.Center, items[i + 1]);

            var trail = withTail ? new List<(Vector2 center, float radius)>() : null;
            TailStyle? linkStyle = (!isLast && _uniformLinks) ? TailStyle.Uniform : null;
            var local = it.B.BuildOutline(it.Xf.AffineInverse() * target, withTail, _borderWidth, trail, linkStyle);
            var pts = ToChainSpace(local, it.Xf);
            if (pts == null) continue;
            polys.Add(pts);

            if (trail != null)
            {
                float sc = Mathf.Abs(it.Xf.Scale.X);
                foreach (var (c, rad) in trail)
                    trails.Add((it.Xf * c, rad * sc));
            }
        }

        if (polys.Count == 0) return;

        // Fuse everything into one (or, if some bubbles are apart, several) outlines
        var shapes = MergeAll(polys);

        bool ok = true;
        foreach (var shape in shapes)
        {
            if (!SpeechBubble.CanFill(SpeechBubble.Clean(new List<Vector2>(shape))))
            {
                ok = false;
                break;
            }
        }
        if (!ok) shapes = polys;   // fallback: draw bubbles separately rather than risk a triangulation error

        foreach (var shape in shapes)
            FillStroke(SpeechBubble.Clean(new List<Vector2>(shape)));

        foreach (var (center, radius) in trails)
        {
            DrawCircle(center, radius, _fill);
            DrawArc(center, radius, 0f, Mathf.Tau, 24, _border, _borderWidth, true);
        }
    }

    private static Vector2[] ToChainSpace(List<Vector2> local, Transform2D xf)
    {
        if (local == null || local.Count < 3) return null;
        var pts = new Vector2[local.Count];
        for (int i = 0; i < pts.Length; i++)
            pts[i] = xf * local[i];
        return pts;
    }

    /// Point a little way inside the next bubble, along the line from this bubble's center to the next one's.
    private Vector2 LinkTarget(Vector2 from, Item next)
    {
        var poly = next.Base;
        float bestSq = float.MaxValue;
        Vector2 entry = Vector2.Zero;
        bool found = false;

        for (int j = 0; j < poly.Length; j++)
        {
            if (SegmentHit(from, next.Center, poly[j], poly[(j + 1) % poly.Length], out Vector2 p))
            {
                float d = from.DistanceSquaredTo(p);
                if (d < bestSq) { bestSq = d; entry = p; found = true; }
            }
        }

        if (!found) return next.Center;     // heavy overlap: the target is inside anyway

        Vector2 toCenter = next.Center - entry;
        float dist = toCenter.Length();
        if (dist < 0.001f) return next.Center;
        return entry + toCenter / dist * Mathf.Min(_linkOverlap, dist);
    }

    private static bool SegmentHit(Vector2 p, Vector2 p2, Vector2 q, Vector2 q2, out Vector2 hit)
    {
        hit = Vector2.Zero;
        Vector2 r = p2 - p, s = q2 - q;
        float denom = r.Cross(s);
        if (Mathf.Abs(denom) < 1e-6f) return false;
        Vector2 qp = q - p;
        float t = qp.Cross(s) / denom;
        float u = qp.Cross(r) / denom;
        if (t < 0f || t > 1f || u < 0f || u > 1f) return false;
        hit = p + r * t;
        return true;
    }

    private void FillStroke(List<Vector2> pts)
    {
        if (pts.Count < 3) return;

        if (SpeechBubble.CanFill(pts))
            DrawColoredPolygon(pts.ToArray(), _fill);

        var closed = new List<Vector2>(pts) { pts[0] };
        DrawPolyline(closed.ToArray(), _border, _borderWidth, true);
    }

    // =====================================================================
    //  Polygon merging
    // =====================================================================

    /// Unions all polygons. Overlapping/touching ones fuse; separate groups stay separate shapes.
    private static List<Vector2[]> MergeAll(List<Vector2[]> polys)
    {
        var shapes = new List<Vector2[]>();

        foreach (var poly in polys)
        {
            var current = poly;

            bool changed;
            do
            {
                changed = false;
                for (int i = shapes.Count - 1; i >= 0; i--)
                {
                    var outers = OuterOnly(Geometry2D.MergePolygons(current, shapes[i]));
                    if (outers.Count == 1)       // they overlapped and fused into one
                    {
                        current = outers[0];
                        shapes.RemoveAt(i);
                        changed = true;
                    }
                }
            } while (changed);   // the grown shape may now touch shapes it missed before

            shapes.Add(current);
        }

        return shapes;
    }

    /// Drops holes (polygons lying inside another result polygon) so the blob stays solid.
    private static List<Vector2[]> OuterOnly(Godot.Collections.Array<Vector2[]> result)
    {
        var outers = new List<Vector2[]>();
        for (int i = 0; i < result.Count; i++)
        {
            var candidate = result[i];
            if (candidate.Length < 3) continue;

            bool inside = false;
            for (int j = 0; j < result.Count && !inside; j++)
            {
                if (i == j || result[j].Length < 3) continue;
                inside = Geometry2D.IsPointInPolygon(candidate[0], result[j]);
            }
            if (!inside) outers.Add(candidate);
        }
        return outers;
    }
}
