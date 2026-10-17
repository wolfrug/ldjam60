using Godot;

public partial class InkBackgroundEditor : Node2D
{
    // Drag your cinema.tres here in the inspector. This is also the file that gets saved.
    [Export] public InkBackgroundData Data { get; set; }

    private const float HitRadius = 12f; // in screen pixels

    private Camera2D _cam;
    private LineEdit _idEdit;
    private Label _status;
    private string _selected;
    private bool _dragging;
    private bool _panning;

    public override void _Ready()
    {
        _cam = new Camera2D();
        AddChild(_cam);
        _cam.MakeCurrent();

        BuildUi();

        if (Data?.Texture == null)
        {
            SetStatus("Assign a BackgroundData with a Texture to this node.");
            return;
        }

        // Fit the image in the window to start with.
        Vector2 tex = Data.Texture.GetSize();
        Vector2 win = GetViewportRect().Size;
        float z = Mathf.Min(win.X / tex.X, win.Y / tex.Y) * 0.9f;
        _cam.Zoom = new Vector2(z, z);
        _cam.Position = tex / 2;
        SetStatus("Ctrl+Click: add | Drag: move | Right-click/Del: delete | MMB: pan | Wheel: zoom | Ctrl+S: save");
    }

    // ---------- UI ----------

    private void BuildUi()
    {
        var layer = new CanvasLayer();
        AddChild(layer);

        var panel = new PanelContainer();
        layer.AddChild(panel);

        var box = new HBoxContainer();
        panel.AddChild(box);

        box.AddChild(new Label { Text = "ID:" });
        _idEdit = new LineEdit { CustomMinimumSize = new Vector2(160, 0), PlaceholderText = "select a POI" };
        _idEdit.TextSubmitted += _ => RenameSelected();
        box.AddChild(_idEdit);

        var rename = new Button { Text = "Rename" };
        rename.Pressed += RenameSelected;
        box.AddChild(rename);

        var delete = new Button { Text = "Delete" };
        delete.Pressed += DeleteSelected;
        box.AddChild(delete);

        var save = new Button { Text = "Save" };
        save.Pressed += Save;
        box.AddChild(save);

        _status = new Label();
        box.AddChild(_status);
    }

    private void SetStatus(string text) => _status.Text = "   " + text;

    // ---------- drawing ----------

    public override void _Draw()
    {
        if (Data?.Texture == null) return;
        DrawTexture(Data.Texture, Vector2.Zero);

        float s = 1f / _cam.Zoom.X; // keeps markers a constant size on screen
        var font = ThemeDB.FallbackFont;

        foreach (var (id, pos) in Data.POIs)
        {
            bool sel = id == _selected;
            DrawSetTransform(pos, 0, new Vector2(s, s));
            DrawCircle(Vector2.Zero, 7, sel ? Colors.Yellow : Colors.Red);
            DrawArc(Vector2.Zero, 7, 0, Mathf.Tau, 24, Colors.Black, 1.5f);
            DrawString(font, new Vector2(10, -8), $"{id} ({pos.X:0}, {pos.Y:0})",
                HorizontalAlignment.Left, -1, 14, Colors.White);
        }
        DrawSetTransform(Vector2.Zero);
    }

    // ---------- input ----------

    private Vector2 ScreenToWorld(Vector2 screen) =>
        _cam.Position + (screen - GetViewportRect().Size / 2) / _cam.Zoom.X;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Data == null) return;

        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelDown)
            {
                if (!mb.Pressed) return;
                Vector2 before = ScreenToWorld(mb.Position);
                float f = mb.ButtonIndex == MouseButton.WheelUp ? 1.1f : 1 / 1.1f;
                float z = Mathf.Clamp(_cam.Zoom.X * f, 0.05f, 50f);
                _cam.Zoom = new Vector2(z, z);
                _cam.Position += before - ScreenToWorld(mb.Position); // zoom toward cursor
                QueueRedraw();
            }
            else if (mb.ButtonIndex == MouseButton.Middle)
            {
                _panning = mb.Pressed;
            }
            else if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    GetViewport().GuiReleaseFocus(); // so keys like Delete reach us, not the LineEdit
                    string hit = FindHit(mb.Position);
                    if (hit != null) { Select(hit); _dragging = true; }
                    else if (mb.CtrlPressed) AddPoi(ScreenToWorld(mb.Position));
                    else Select(null);
                }
                else _dragging = false;
            }
            else if (mb.ButtonIndex == MouseButton.Right && mb.Pressed)
            {
                string hit = FindHit(mb.Position);
                if (hit != null) { Select(hit); DeleteSelected(); }
            }
        }
        else if (@event is InputEventMouseMotion mm)
        {
            if (_panning)
            {
                _cam.Position -= mm.Relative / _cam.Zoom.X;
                QueueRedraw();
            }
            else if (_dragging && _selected != null)
            {
                Data.POIs[_selected] = ScreenToWorld(mm.Position).Round();
                QueueRedraw();
            }
        }
        else if (@event is InputEventKey { Pressed: true } key)
        {
            if (key.Keycode == Key.Delete) DeleteSelected();
            else if (key.Keycode == Key.S && key.CtrlPressed) Save();
        }
    }

    // ---------- operations ----------

    private string FindHit(Vector2 screenPos)
    {
        Vector2 world = ScreenToWorld(screenPos);
        float maxDist = HitRadius / _cam.Zoom.X;
        string best = null;
        float bestDist = maxDist;
        foreach (var (id, pos) in Data.POIs)
        {
            float d = pos.DistanceTo(world);
            if (d <= bestDist) { bestDist = d; best = id; }
        }
        return best;
    }

    private void Select(string id)
    {
        _selected = id;
        _idEdit.Text = id ?? "";
        QueueRedraw();
    }

    private void AddPoi(Vector2 pos)
    {
        int i = Data.POIs.Count + 1;
        string id = $"poi_{i}";
        while (Data.POIs.ContainsKey(id)) id = $"poi_{++i}";
        Data.POIs[id] = pos.Round();
        Select(id);
    }

    private void DeleteSelected()
    {
        if (_selected == null) return;
        Data.POIs.Remove(_selected);
        Select(null);
    }

    private void RenameSelected()
    {
        if (_selected == null) return;
        string newId = _idEdit.Text.Trim();
        if (newId == "" || newId == _selected || Data.POIs.ContainsKey(newId))
        {
            _idEdit.Text = _selected;
            return;
        }
        Vector2 pos = Data.POIs[_selected];
        Data.POIs.Remove(_selected);
        Data.POIs[newId] = pos;
        Select(newId);
    }

    private void Save()
    {
        if (Data == null || string.IsNullOrEmpty(Data.ResourcePath))
        {
            SetStatus("Cannot save: Data has no file path (is it saved as a .tres?).");
            return;
        }
        var err = ResourceSaver.Save(Data, Data.ResourcePath);
        SetStatus(err == Error.Ok ? $"Saved to {Data.ResourcePath}" : $"Save failed: {err}");
    }
}