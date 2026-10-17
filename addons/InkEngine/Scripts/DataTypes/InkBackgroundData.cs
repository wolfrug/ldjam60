using System.ComponentModel;
using System.Data;
using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass, Tool]
public partial class InkBackgroundData : InkResource
{
    [Export] public Texture2D Texture { get; set; }

    [Export] public Dictionary<string, Resource> SubTextures {get; set; } = new();

    // Coordinates are in texture pixels, origin = top-left of the image.
    [Export] public Dictionary<string, Vector2> POIs { get; set; } = new();

    public bool TryGetPoi(string id, out Vector2 pos) => POIs.TryGetValue(id, out pos);

    public bool TryGetSubTexture(string id, out Resource tex) => SubTextures.TryGetValue(id, out tex);

    [ExportToolButton("Auto-Set ID")]
    public Callable ToolButton => Callable.From(SetID);

    private void SetID()
    {
        _ID = Texture.ResourcePath.Split('/').LastOrDefault();
        ResourceSaver.Save(this);
    }

    public string ID
    {
        get
        {
            if (_ID == "")
            {
                SetID();
            }
            return _ID;
        }
    }

    // Public + PascalCase so the editor's undo/redo can call them by name.
    public void SetPoi(string id, Vector2 pos)
    {
        POIs[id] = pos;
        EmitChanged();
        NotifyPropertyListChanged();
    }

    public void RemovePoi(string id)
    {
        POIs.Remove(id);
        EmitChanged();
        NotifyPropertyListChanged();
    }
}