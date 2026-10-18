using Godot;

/// <summary>
/// Attach this script to a Camera2D node.
///
/// Lets you pan over a 2D sprite within horizontal/vertical bounds and
/// speed limits, via:
///   - Mouse drag
///   - Touch drag (single finger)
///   - Controller stick / keyboard (through the Godot Input Map)
///
/// Panning automatically pauses while the mouse is hovering a Control
/// (Button, Panel, etc). Can be enabled/disabled via the exported bool,
/// via a connected signal, or by calling SetPanningEnabled()/TogglePanning().
///
/// SETUP REQUIRED FOR STICK/KEYBOARD PANNING:
/// In Project Settings > Input Map, create 4 actions:
///     pan_left, pan_right, pan_up, pan_down
/// and add both keyboard keys (arrows/WASD) AND a "Joypad Axis" event for
/// each (Left Stick X-/X+ for pan_left/pan_right, Left Stick Y-/Y+ for
/// pan_up/pan_down). This gives you keyboard + controller stick support
/// for free through Input.GetVector.
/// </summary>
[GlobalClass]
public partial class PanCameraController : Camera2D
{
    [ExportGroup("Activation")]
    [Export] public bool PanningEnabled = true;

    [ExportGroup("Bounds")]
    // Optional: the sprite whose texture defines the pannable area.
    [Export] public Sprite2D TargetSprite;
    // If true and TargetSprite has a texture, bounds are derived from its
    // texture size * global scale. If false (or no sprite), ManualMaxPan is used.
    [Export] public bool AutoDetectBoundsFromSprite = true;
    // Max distance (px) the camera may travel from center on each axis,
    // used when AutoDetectBoundsFromSprite is false / no sprite assigned.
    [Export] public Vector2 ManualMaxPan = new Vector2(1000, 1000);
    // Shrinks the effective bounds by half the viewport size (adjusted for
    // zoom) so the camera view never shows past the edges of the image.
    [Export] public bool AdjustForViewportSize = true;

    [ExportGroup("Speed (stick / keyboard)")]
    [Export] public Vector2 MaxPanSpeed = new Vector2(800, 800);
    [Export(PropertyHint.Range, "0,1")] public float StickDeadzone = 0.2f;

    [ExportGroup("Mouse / Touch Drag")]
    [Export] public bool AllowMouseDrag = true;
    [Export] public bool AllowTouchDrag = true;
    [Export] public MouseButton DragMouseButton = MouseButton.Left;
    [Export] public float DragSensitivity = 1.0f;

    [ExportGroup("Edge Panning")]
    // Pan when the mouse gets close to the edge of the screen (or a custom area).
    [Export] public bool EdgePanEnabled = true;
    [Export] public Vector2 EdgePanSpeed = new Vector2(800, 800);
    // If true, margins below are a fraction of the screen/area size.
    // If false, the *Pixels margins are used instead as fixed pixel values.
    [Export] public bool EdgePanMarginIsPercentage = true;
    // Distance from the edge at which panning BEGINS (slowest). E.g. 0.2 =
    // panning starts once the mouse is 80% of the way to that edge.
    [Export(PropertyHint.Range, "0,0.5")] public float EdgePanStartMarginPercent = 0.2f;
    // Distance from the edge at which panning reaches EdgePanSpeed (fastest).
    // E.g. 0.1 = full speed is reached at 90% of the way to that edge.
    // Must be smaller than EdgePanStartMarginPercent.
    [Export(PropertyHint.Range, "0,0.5")] public float EdgePanMaxSpeedMarginPercent = 0.05f;
    [Export] public float EdgePanStartMarginPixels = 150f;
    [Export] public float EdgePanMaxSpeedMarginPixels = 30f;
    // If true, speed ramps linearly from 0 at the start margin up to
    // EdgePanSpeed at the max-speed margin. If false, it's an instant flat
    // EdgePanSpeed anywhere inside the start margin (old behavior).
    [Export] public bool EdgePanSpeedRampsWithProximity = true;
    // By default the edge is the actual game viewport. Enable this and set
    // CustomEdgePanArea to restrict edge detection to a specific rectangle
    // instead (e.g. to exclude a sidebar/toolbar region from triggering pan).
    [Export] public bool UseCustomEdgePanArea = false;
    [Export] public Rect2 CustomEdgePanArea = new Rect2(Vector2.Zero, new Vector2(1920, 1080));
    // If the mouse leaves the window/area entirely while already past an
    // edge, panning keeps going at full speed instead of stopping the
    // instant the cursor exits (matches how it already behaves in
    // fullscreen, where the OS cursor physically can't leave the screen).
    // To avoid runaway panning if the user alt-tabs away, panning still
    // stops whenever the game window loses OS focus.
    [Export] public bool StopEdgePanWhenWindowUnfocused = true;

    [ExportGroup("UI Interaction")]
    [Export] public bool PauseWhenOverUI = true;

    [ExportGroup("Zoom")]
    [Export] public bool ZoomEnabled = true;
    // In Godot 4, Zoom > 1 is zoomed IN, Zoom < 1 is zoomed OUT.
    [Export] public float MinZoom = 0.5f; // most zoomed OUT
    [Export] public float MaxZoom = 2.0f; // most zoomed IN
    [Export] public float ZoomStep = 0.1f;
    // If true, each wheel notch multiplies zoom by (1 ± ZoomStep) - feels
    // more even across a wide zoom range. If false, each notch adds/subtracts
    // a flat ZoomStep amount.
    [Export] public bool ZoomStepIsMultiplicative = true;
    // How fast the camera eases toward the target zoom, per second. 0 = snap instantly.
    [Export] public float ZoomSmoothingSpeed = 10f;
    // If true, zooming keeps the point currently under the mouse cursor in
    // place (like most map/image viewers). If false, it zooms toward the
    // center of the screen. Assumes the camera isn't rotated.
    [Export] public bool ZoomTowardMouseCursor = true;
    // If true, the camera can never zoom out further than the point where
    // the pannable bounds exactly fill the viewport - i.e. you'll never see
    // empty space beyond the edges of the image, even if MinZoom would
    // otherwise allow it. This is computed dynamically from the current
    // viewport size and bounds, so it also adapts to window resizing.
    [Export] public bool ClampZoomToImageBounds = false;

    [ExportGroup("Force-Pan / Reset")]
    [Export] public bool SmoothForcePan = true;
    [Export] public float ForcePanDuration = 0.35f;

    // Emitted whenever panning is enabled/disabled (useful for syncing UI).
    [Signal] public delegate void PanningStateChangedEventHandler(bool enabled);

    private Vector2 _center;
    private Vector2 _maxPan;
    private bool _dragging;
    private int _dragTouchIndex = -1;
    private Tween _forcePanTween;
    private float _targetZoom;

    private bool _defaultPanningOn;
    private bool _defaultZoomOn;

    public override void _Ready()
    {
        _center = TargetSprite != null ? TargetSprite.GlobalPosition : GlobalPosition;
        RecalculateBounds();
        _targetZoom = Zoom.X;
        TargetSprite.TextureChanged += RecalculateBounds;
        _defaultPanningOn = PanningEnabled;
        _defaultZoomOn = ZoomEnabled;
    }

    // Call this again at runtime if you swap TargetSprite's texture,
    // change ManualMaxPan, or move the sprite and want a fresh center.
    public void RecalculateBounds()
    {
        if (AutoDetectBoundsFromSprite && TargetSprite != null && TargetSprite.Texture != null)
        {
            Vector2 textureSize = TargetSprite.Texture.GetSize();
            Vector2 scale = TargetSprite.GlobalScale;
            _maxPan = new Vector2(
                Mathf.Abs(textureSize.X * scale.X) / 2f,
                Mathf.Abs(textureSize.Y * scale.Y) / 2f
            );
        }
        else
        {
            _maxPan = ManualMaxPan;
        }
        Position = ClampToBounds(Position);
    }

    public override void _Process(double delta)
    {
        ApplyZoom(delta);

        if (!PanningEnabled)
            return;

        bool overUI = PauseWhenOverUI && GetViewport().GuiGetHoveredControl() != null;
        if (overUI)
            return;

        Vector2 totalVelocity = Vector2.Zero;

        Vector2 inputDir = Input.GetVector("pan_left", "pan_right", "pan_up", "pan_down", StickDeadzone);
        if (inputDir != Vector2.Zero)
            totalVelocity += new Vector2(inputDir.X * MaxPanSpeed.X, inputDir.Y * MaxPanSpeed.Y);

        // Edge panning is suppressed while actively drag-panning, so the two
        // input methods don't fight each other.
        if (EdgePanEnabled && !_dragging)
            totalVelocity += GetEdgePanVelocity();

        if (totalVelocity != Vector2.Zero)
            Position = ClampToBounds(Position + totalVelocity * (float)delta);
    }

    // Computes a pan velocity based on how close the mouse is to the edges
    // of the screen (or CustomEdgePanArea), scaled by EdgePanSpeed.
    private Vector2 GetEdgePanVelocity()
    {
        if (StopEdgePanWhenWindowUnfocused && !GetWindow().HasFocus())
            return Vector2.Zero;

        Rect2 area = UseCustomEdgePanArea
            ? CustomEdgePanArea
            : new Rect2(Vector2.Zero, GetViewportRect().Size);

        if (area.Size.X <= 0f || area.Size.Y <= 0f)
            return Vector2.Zero;

        Vector2 mousePos = GetViewport().GetMousePosition();
        Vector2 local = mousePos - area.Position;

        // Clamp (rather than bail out) if the mouse is outside the area -
        // this way, if the cursor leaves the window/area while already past
        // an edge, panning keeps going at full intensity instead of
        // stopping the instant it exits.
        local.X = Mathf.Clamp(local.X, 0f, area.Size.X);
        local.Y = Mathf.Clamp(local.Y, 0f, area.Size.Y);

        float startMarginX = EdgePanMarginIsPercentage ? area.Size.X * EdgePanStartMarginPercent : EdgePanStartMarginPixels;
        float startMarginY = EdgePanMarginIsPercentage ? area.Size.Y * EdgePanStartMarginPercent : EdgePanStartMarginPixels;
        float maxMarginX = EdgePanMarginIsPercentage ? area.Size.X * EdgePanMaxSpeedMarginPercent : EdgePanMaxSpeedMarginPixels;
        float maxMarginY = EdgePanMarginIsPercentage ? area.Size.Y * EdgePanMaxSpeedMarginPercent : EdgePanMaxSpeedMarginPixels;

        startMarginX = Mathf.Min(startMarginX, area.Size.X / 2f);
        startMarginY = Mathf.Min(startMarginY, area.Size.Y / 2f);
        maxMarginX = Mathf.Clamp(maxMarginX, 0f, startMarginX);
        maxMarginY = Mathf.Clamp(maxMarginY, 0f, startMarginY);

        float axisX = GetEdgeAxis(local.X, area.Size.X, startMarginX, maxMarginX);
        float axisY = GetEdgeAxis(local.Y, area.Size.Y, startMarginY, maxMarginY);

        return new Vector2(axisX * EdgePanSpeed.X, axisY * EdgePanSpeed.Y);
    }

    // Returns a value in [-1, 1] for one axis: negative near the "start" edge
    // (left/top), positive near the "end" edge (right/bottom), 0 outside the
    // start margin. Between startMargin and maxSpeedMargin, intensity ramps
    // linearly (if EdgePanSpeedRampsWithProximity); inside maxSpeedMargin
    // it's already at full intensity.
    private float GetEdgeAxis(float localPos, float size, float startMargin, float maxSpeedMargin)
    {
        if (startMargin <= 0f)
            return 0f;

        float distFromStart = localPos;
        float distFromEnd = size - localPos;

        if (distFromStart < startMargin)
            return -GetProximityFactor(distFromStart, startMargin, maxSpeedMargin);

        if (distFromEnd < startMargin)
            return GetProximityFactor(distFromEnd, startMargin, maxSpeedMargin);

        return 0f;
    }

    // dist: distance from the relevant edge. Returns 0 at startMargin,
    // ramping up to 1 at maxSpeedMargin (and staying at 1 closer than that).
    private float GetProximityFactor(float dist, float startMargin, float maxSpeedMargin)
    {
        if (!EdgePanSpeedRampsWithProximity)
            return 1f;

        if (dist <= maxSpeedMargin)
            return 1f;

        float range = startMargin - maxSpeedMargin;
        if (range <= 0.0001f)
            return 1f;

        return 1f - ((dist - maxSpeedMargin) / range);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        bool overUI = PauseWhenOverUI && GetViewport().GuiGetHoveredControl() != null;

        if (ZoomEnabled && !overUI && @event is InputEventMouseButton wheel && wheel.Pressed)
        {
            if (wheel.ButtonIndex == MouseButton.WheelUp)
                AdjustTargetZoom(1);  // zoom in
            else if (wheel.ButtonIndex == MouseButton.WheelDown)
                AdjustTargetZoom(-1); // zoom out
        }

        if (@event is InputEventMouseButton mb && mb.ButtonIndex == DragMouseButton)
        {
            if (mb.Pressed)
            {
                if (AllowMouseDrag && PanningEnabled && !overUI)
                    _dragging = true;
            }
            else
            {
                _dragging = false;
            }
            return;
        }

        if (@event is InputEventMouseMotion mm)
        {
            if (_dragging && PanningEnabled && !overUI)
                ApplyDragDelta(mm.Relative);
            return;
        }

        if (@event is InputEventScreenTouch touch)
        {
            if (touch.Pressed)
            {
                if (AllowTouchDrag && PanningEnabled && !overUI && _dragTouchIndex == -1)
                    _dragTouchIndex = touch.Index;
            }
            else if (touch.Index == _dragTouchIndex)
            {
                _dragTouchIndex = -1;
            }
            return;
        }

        if (@event is InputEventScreenDrag drag && drag.Index == _dragTouchIndex)
        {
            if (PanningEnabled && !overUI)
                ApplyDragDelta(drag.Relative);
        }
    }

    private void ApplyDragDelta(Vector2 screenDelta)
    {
        // Zoom > 1 = zoomed in (each screen pixel covers fewer world units),
        // so world movement scales inversely with zoom.
        Vector2 worldDelta = screenDelta / Zoom;
        Position = ClampToBounds(Position - worldDelta * DragSensitivity);
    }

    private Vector2 GetEffectiveMaxPan()
    {
        if (!AdjustForViewportSize)
            return _maxPan;

        Vector2 viewportHalf = GetViewportRect().Size / (2f * Zoom);
        return new Vector2(
            Mathf.Max(_maxPan.X - viewportHalf.X, 0f),
            Mathf.Max(_maxPan.Y - viewportHalf.Y, 0f)
        );
    }

    private Vector2 ClampToBounds(Vector2 pos)
    {
        Vector2 effectiveMax = GetEffectiveMaxPan();
        float minX = _center.X - effectiveMax.X;
        float maxX = _center.X + effectiveMax.X;
        float minY = _center.Y - effectiveMax.Y;
        float maxY = _center.Y + effectiveMax.Y;
        return new Vector2(Mathf.Clamp(pos.X, minX, maxX), Mathf.Clamp(pos.Y, minY, maxY));
    }

    // direction: +1 to zoom in (wheel up), -1 to zoom out (wheel down).
    private void AdjustTargetZoom(int direction)
    {
        _targetZoom = ZoomStepIsMultiplicative
            ? _targetZoom * (1f + direction * ZoomStep)
            : _targetZoom + direction * ZoomStep;

        _targetZoom = Mathf.Clamp(_targetZoom, GetEffectiveMinZoom(), MaxZoom);
    }

    // Returns MinZoom, or - if ClampZoomToImageBounds is on - the higher of
    // MinZoom and the zoom level at which the pannable bounds exactly cover
    // the viewport (so you can never zoom out far enough to see past them).
    private float GetEffectiveMinZoom()
    {
        if (!ClampZoomToImageBounds || _maxPan.X <= 0f || _maxPan.Y <= 0f)
            return MinZoom;

        Vector2 viewportSize = GetViewportRect().Size;
        float requiredZoomX = viewportSize.X / (2f * _maxPan.X);
        float requiredZoomY = viewportSize.Y / (2f * _maxPan.Y);
        float requiredMinZoom = Mathf.Max(requiredZoomX, requiredZoomY);

        // Never push the floor above MaxZoom - if the bounds are smaller
        // than the viewport even at max zoom-in, showing space beyond them
        // is unavoidable; better to fall back gracefully than break the clamp.
        return Mathf.Min(Mathf.Max(MinZoom, requiredMinZoom), MaxZoom);
    }

    private void ApplyZoom(double delta)
    {
        if (!ZoomEnabled)
            return;

        // Re-clamp in case ClampZoomToImageBounds is on and the viewport was
        // resized (or the bounds changed) since the target was last set.
        _targetZoom = Mathf.Clamp(_targetZoom, GetEffectiveMinZoom(), MaxZoom);

        Vector2 currentZoom = Zoom;
        Vector2 targetZoomVec = new Vector2(_targetZoom, _targetZoom);

        if (currentZoom.IsEqualApprox(targetZoomVec))
            return;

        Vector2 newZoom = ZoomSmoothingSpeed > 0f
            ? currentZoom.Lerp(targetZoomVec, Mathf.Clamp((float)delta * ZoomSmoothingSpeed, 0f, 1f))
            : targetZoomVec;

        if (ZoomTowardMouseCursor)
        {
            Vector2 mouseScreenPos = GetViewport().GetMousePosition();
            Vector2 viewportHalf = GetViewportRect().Size / 2f;
            // World point currently under the cursor, using the OLD zoom.
            Vector2 worldPosUnderCursor = Position + (mouseScreenPos - viewportHalf) / currentZoom;

            Zoom = newZoom;
            // Re-derive position so that same world point stays under the cursor with the NEW zoom.
            Position = worldPosUnderCursor - (mouseScreenPos - viewportHalf) / newZoom;
        }
        else
        {
            Zoom = newZoom;
        }

        Position = ClampToBounds(Position);
    }

    // ---------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------

    // Enable/disable panning. Connect a signal (e.g. a Button's "toggled"
    // or "pressed" signal) to this method to activate/deactivate via signal.
    public void SetPanningEnabled(bool enabled)
    {
        PanningEnabled = enabled;
        if (!enabled)
        {
            _dragging = false;
            _dragTouchIndex = -1;
        }
        EmitSignal(SignalName.PanningStateChanged, enabled);
    }

    public void TogglePanning() => SetPanningEnabled(!PanningEnabled);

    public void SetControlsOn()
    {
        SetZoom(Zoom.X, false);
        PanTo(Position, false);
        SetPanningEnabled(_defaultPanningOn);
        ZoomEnabled = _defaultZoomOn;
    }
    public void SetControlsOff()
    {
        SetPanningEnabled(false);
        ZoomEnabled = false;
    }

    // Force-pans the camera to an arbitrary world-space position (clamped
    // to bounds). Set immediate = true to snap instantly instead of
    // smoothing, regardless of the SmoothForcePan export.
    public void PanTo(Vector2 targetPosition, bool immediate = false)
    {
        Vector2 clamped = ClampToBounds(targetPosition);

        _forcePanTween?.Kill();

        if (immediate || !SmoothForcePan)
        {
            Position = clamped;
        }
        else
        {
            _forcePanTween = GetTree().CreateTween();
            _forcePanTween
                .TweenProperty(this, "position", clamped, ForcePanDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
        }
    }

    // Convenience: pan relative to the current position.
    public void PanBy(Vector2 offset, bool immediate = false) => PanTo(Position + offset, immediate);

    // Resets the camera to the center of the image/bounds.
    public void ResetToCenter(bool immediate = false) => PanTo(_center, immediate);

    // Sets the zoom target (clamped to MinZoom/MaxZoom). With immediate =
    // true, snaps right away instead of easing via ZoomSmoothingSpeed.
    // Zooms toward screen center, not the mouse cursor, since this is a
    // programmatic call rather than a wheel event.
    public void SetZoom(float targetZoom, bool immediate = false)
    {
        _targetZoom = Mathf.Clamp(targetZoom, GetEffectiveMinZoom(), MaxZoom);
        if (immediate)
            Zoom = new Vector2(_targetZoom, _targetZoom);
    }
}
