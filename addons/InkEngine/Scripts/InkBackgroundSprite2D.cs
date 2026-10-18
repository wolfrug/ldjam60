using Godot;
using System;
using GodotInk;
using InkEngine;
using Godot.Collections;
using System.Collections.Generic;
public partial class InkBackgroundSprite2D : Sprite2D
{
    [Export]
    public Camera2D camera;
    [Export]
    public bool use2DCamera = true;

    [Export]
    private Godot.Collections.Dictionary<string, Node2D> presetCameraLocations = new Godot.Collections.Dictionary<string, Node2D> { };


    private const string c_setBackground = "SET_SPRITEBACKGROUND:";
    private const string c_moveCamera = "SET_CAMERA_TARGET:";
    private const string c_setSubTexture = "SET_SPRITESUBTEXTURE:";

    private const char c_delimiter = '^';

    [Export] public InkBackgroundData currentData;

    public Godot.Collections.Dictionary<string, Vector2> presetCoordinates = new Godot.Collections.Dictionary<string, Vector2> { };

    public Godot.Collections.Dictionary<string, Node2D> activeSubTextures = new Godot.Collections.Dictionary<string, Node2D> { };

    [Signal] public delegate void InkCameraStartPanEventHandler();
    [Signal] public delegate void InkCameraEndPanEventHandler();
    [Signal] public delegate void InkSpriteBackgroundChangedEventHandler();
    public static InkBackgroundSprite2D instance;

    // Called when the node enters the scene tree for the first time.
    public override void _EnterTree()
    {
        if (instance == null)
        {
            instance = this;
        }
        GlobalEvents.OnTagsFound += GlobalEvent_OnTagsFound;
        GlobalEvents.OnInkSpriteBackgroundCameraMove += MoveCamera;
        GlobalEvents.OnInkSpriteBackgroundChanged += SetBackground;
        GlobalEvents.OnInkSpriteBackgroundSubTexChanged += SetSubTexture2D;
        base._EnterTree();
    }
    public override void _ExitTree()
    {
        if (instance == this)
        {
            instance = null;
        }
        GlobalEvents.OnTagsFound -= GlobalEvent_OnTagsFound;
        GlobalEvents.OnInkSpriteBackgroundCameraMove -= MoveCamera;
        GlobalEvents.OnInkSpriteBackgroundChanged -= SetBackground;
        GlobalEvents.OnInkSpriteBackgroundSubTexChanged -= SetSubTexture2D;
        base._ExitTree();
    }
    public override void _Ready()
    {
        if (use2DCamera)
        {
            camera.MakeCurrent();
        }
        UpdatePresetCameraLocationDictionary();
        GlobalVariables.LoadAllBackgroundDatas();
        base._Ready();
    }

    public void UpdatePresetCameraLocationDictionary()
    {
        presetCoordinates.Clear();
        foreach (System.Collections.Generic.KeyValuePair<string, Node2D> kvp in presetCameraLocations)
        {
            presetCoordinates.Add(kvp.Key, kvp.Value.Position);
        }
        if (currentData != null)
        {
            foreach (System.Collections.Generic.KeyValuePair<string, Vector2> POI in currentData.POIs)
            {
                presetCoordinates.Add(POI.Key, GetPOIPosition(POI.Key));
            }
        }
    }

    private Vector2 GetPOIPosition(string POI)
    { // We have to make sure it's not Centered, because that fucks up the POI location
        if (currentData != null && currentData.TryGetPoi(POI, out Vector2 loc))
        {
            if (Centered)
            {
                Vector2 local = loc - currentData.Texture.GetSize() / 2 + Offset;
                Vector2 world = ToGlobal(local);
                return world;
            }
            else
            {
                return loc;
            }
        }
        else
        {
            return Vector2.Zero;
        }
    }

    void GlobalEvent_OnTagsFound(InkEventArgs args)
    {
        //GD.Print("Looking for background tags");
        foreach (string tag in args.inkTags)
        {
            //GD.Print("looking for " + c_setBackground + " in tag " + tag);
            if (tag.Contains(c_setBackground))
            {
                SetBackgroundParse(tag);
            }
            if (tag.Contains(c_moveCamera))
            {
                MoveCameraParse(tag);
            }
            if (tag.Contains(c_setSubTexture))
            {
                SetSubTexture2DParse(tag);
            }
        }
    }

    public void MoveCameraParse(string tag)
    {// Use: #SET_CAMERA_TARGET:45,-74^0.5^1 -> coordinates, zoom value, time
        string id = tag.Replace(c_moveCamera, "");
        string[] stringParts = id.Split(c_delimiter);
        GD.Print("Received SET CAMERA TARGET coords, with parts: " + id);
        string[] coordinates = stringParts[0].Split(',');
        Vector2 coords = Vector2.Zero;
        string poi = "";
        if (coordinates.Length == 1)
        {
            if (presetCoordinates.TryGetValue(coordinates[0], out Vector2 outcoords))
            {
                coords = outcoords;
                poi = coordinates[0];
            }
            else
            { // No coordinates - we zero the camera
                coords = Vector2.Zero;
            }
        }
        else
        {
            coords = new Vector2(coordinates[0].ToFloat(), coordinates[1].ToFloat());
        }
        GD.Print("Getting zoom value out of string " + stringParts[1]);
        float zoomValue = stringParts[1].ToFloat();
        float time = stringParts[2].ToFloat();
        GlobalEvents.SendOnInkSpriteBackgroundCameraMove(new InkSpriteBackgroundEventArgs
        {
            targetCoords = coords,
            camTime = time,
            camZoom = zoomValue,
            targetCamera = camera,
            targetPOI = poi,
            targetNode = this
        });
    }
    Tween _cameraTween;
    public void MoveCamera(InkSpriteBackgroundEventArgs args)
    {// Use: #SET_CAMERA_TARGET:45,-74^0.5^1 -> coordinates, zoom value, time
        camera.MakeCurrent();
        Vector2 coords = args.targetCoords;
        float zoomValue = args.camZoom;
        float time = args.camTime;
        
        _cameraTween?.Kill();

        _cameraTween = GetTree().CreateTween();
        EmitSignal(SignalName.InkCameraStartPan);
        _cameraTween.TweenProperty(camera, PropertyName.Position.ToString(), coords, time).SetTrans(Tween.TransitionType.Sine);
        _cameraTween.SetParallel(true);
        _cameraTween.TweenProperty(camera, "zoom", new Vector2(zoomValue, zoomValue), time).SetTrans(Tween.TransitionType.Sine);
        _cameraTween.Chain().TweenCallback(Callable.From(() => EmitSignal(SignalName.InkCameraEndPan)));
    }

    Tween _tween;
    void FadeTexture(Texture2D tex)
    {

        if (_tween != null) { _tween.Kill(); }
        ;
        _tween = CreateTween();
        //_tween.SetParallel(true); // lets each child's tween run concurrently, each with its own SetDelay below
        _tween.SetTrans(Tween.TransitionType.Sine);
        _tween.SetEase(Tween.EaseType.InOut);

        _tween.TweenProperty(GetNode(GetPath()), "modulate:a", 0f, 0.25f);
        _tween.TweenCallback(Callable.From(() => SetTexture2D(tex)));
        if (tex != null)
        {
            _tween.TweenProperty(GetNode(GetPath()), "modulate:a", 1f, 0.25f);
        }
        else
        {
            GD.Print("Faded out and removed texture from ink Background!");
        }
    }

    void SetBackgroundParse(string tag)
    {
        string id = tag.Replace(c_setBackground, "");
        Texture2D tex = null;
        InkBackgroundData data = null;
        GlobalVariables.InkBackgroundDatas.TryGetValue(id, out data);

        if (data != null)
        {
            tex = data.Texture;
        }
        else
        {
            tex = GlobalVariables.GetBackgroundTexture2D(id);
        }
        GlobalEvents.SendOnInkSpriteBackgroundChanged(new InkSpriteBackgroundEventArgs
        {
            targetNode = this,
            newTexture = tex,
            newData = data,
        });
    }

    void SetBackground(InkSpriteBackgroundEventArgs args)
    {
        if (use2DCamera)
        {
            camera.MakeCurrent();
        }
        Texture2D tex = args.newTexture;
        currentData = args.newData;
        UpdatePresetCameraLocationDictionary();
        if (tex != Texture)
        {
            FadeTexture(tex);
        }
        /*
        {
            Tween tween = GetTree().CreateTween();
            tween.TweenProperty(GetNode(GetPath()), "modulate:a", 0f, 0.25f).SetTrans(Tween.TransitionType.Sine);
            tween.TweenCallback(Callable.From(() => SetTexture2D(tex)));
            tween.TweenProperty(GetNode(GetPath()), "modulate:a", 1f, 0.25f).SetTrans(Tween.TransitionType.Sine);
        }
        else
        {
            GD.Print("No Background found: nulling");
            Tween tween = GetTree().CreateTween();
            tween.TweenProperty(GetNode(GetPath()), "modulate:a", 0f, 0.25f).SetTrans(Tween.TransitionType.Sine);
        }*/
    }
    public void SetTexture2D(Texture2D texture)
    {
        QueueFreeAllSubTextures(); // Remove all of 'em here!
        Texture = texture;
    }
    public void SetSubTexture2DParse(string tag)
    { // #SET_SPRITESUBTEXTURE:test OR #SET_SPRITESUBTEXTURE:test^false OR #SET_SPRITESUBTEXTURE:test^true^0,25 OR #SET_SPRITESUBTEXTURE:test^true^poi
        string cleanTag = tag.Replace(c_setSubTexture, "");
        string[] stringParts = cleanTag.Split(c_delimiter);
        string ID = stringParts[0];
        bool show = true;
        string targetpoi = "";
        Vector2 coords = GlobalPosition;
        if (stringParts.Length > 1)
        {
            show = stringParts[1] == "true";
        }
        if (stringParts.Length > 2)
        {
            string[] coordinates = stringParts[2].Split(',');
            if (coordinates.Length == 1)
            {
                coords = presetCoordinates[coordinates[0]];
                targetpoi = coordinates[0];
            }
            else
            {
                coords = new Vector2(coordinates[0].ToFloat(), coordinates[1].ToFloat());
            }
        }
        GlobalEvents.SendOnInkSpriteBackgroundSubTexChanged(new InkSpriteBackgroundEventArgs
        {
            targetNode = this,
            newID = ID,
            targetCoords = coords,
            isVisible = show,
            targetPOI = targetpoi,
        });
    }
    public void SetSubTexture2D(InkSpriteBackgroundEventArgs args)
    { // #SET_SPRITESUBTEXTURE:test OR #SET_SPRITESUBTEXTURE:test^false OR #SET_SPRITESUBTEXTURE:test^true^0,25 OR #SET_SPRITESUBTEXTURE:test^true^poi
        string ID = args.newID;
        bool show = args.isVisible;
        Vector2 coords = args.targetCoords;

        // Check if it's already active
        if (activeSubTextures.TryGetValue(ID, out Node2D sprite))
        { // We fade it in or out and set its coords
            sprite.GlobalPosition = coords;
            if (show)
            {
                ShowSubTexture2D(ID);
            }
            else
            {
                HideSubTexture2D(ID);
            }
        } // Else we spawn a new one, if the ID exists & we're supposed to show it
        else if (currentData.TryGetSubTexture(ID, out Resource subTex) && show)
        {
            // Found! We spawn a new child Sprite2D and add it to ourselves
            if (subTex is Texture2D)
            {
                Sprite2D newSprite = new Sprite2D();
                newSprite.Texture = subTex as Texture2D;
                AddChild(newSprite);
                activeSubTextures.Add(ID, newSprite);
                newSprite.GlobalPosition = coords;
            }
            else
            {
                Node2D newSprite = (subTex as PackedScene).Instantiate<Node2D>();
                AddChild(newSprite);
                activeSubTextures.Add(ID, newSprite);
                newSprite.GlobalPosition = coords;
            }
            ShowSubTexture2D(ID);
        }
        else
        {
            GD.PrintErr("InkBackgroundSprite2D: Cannot find subtexture in scene " + currentData.ID + " with ID " + ID);
        }
    }
    public void HideSubTexture2D(string ID)
    {
        // Try to find it in the current scene
        if (activeSubTextures.TryGetValue(ID, out Node2D subTexSprite))
        {
            // Found! We fade it out if it's a sprite2D
            if (subTexSprite is Sprite2D)
            {
                FadeSubTextureSprite(subTexSprite as Sprite2D, false);
            }
            else
            {
                subTexSprite.Visible = false;
            }
        }
    }
    public void ShowSubTexture2D(string ID)
    {
        // Try to find it in the current scene
        if (activeSubTextures.TryGetValue(ID, out Node2D subTexSprite))
        {
            // Found! We fade it out if it's a sprite2D
            if (subTexSprite is Sprite2D)
            {
                FadeSubTextureSprite(subTexSprite as Sprite2D, true);
            }
            else
            {
                Visible = true;
            }
        }
    }
    public void QueueFreeAllSubTextures()
    {
        foreach (KeyValuePair<string, Node2D> kvp in activeSubTextures)
        {
            kvp.Value.QueueFree();
        }
        activeSubTextures.Clear();
    }
    public void FadeSubTextureSprite(Sprite2D target, bool fadeIn)
    {
        Tween tween = target.CreateTween();
        if (fadeIn)
        {
            target.Modulate = new Color(1f, 1f, 1f, 0f);
            target.Visible = true;
            tween.TweenProperty(target, "modulate:a", 1f, 0.25f).SetTrans(Tween.TransitionType.Sine);
        }
        else
        {
            target.Modulate = new Color(1f, 1f, 1f, 1f);
            tween.TweenProperty(target, "modulate:a", 0f, 0.25f).SetTrans(Tween.TransitionType.Sine);
            tween.TweenCallback(Callable.From(() => target.Visible = false));
        }
    }
}

public static partial class GlobalEvents
{
    // Sprite2D Background Events
    public delegate void InkSpriteBackgroundEvent(InkSpriteBackgroundEventArgs eventArgs);

    public static event InkSpriteBackgroundEvent OnInkSpriteBackgroundChanged;
    public static event InkSpriteBackgroundEvent OnInkSpriteBackgroundSubTexChanged;
    public static event InkSpriteBackgroundEvent OnInkSpriteBackgroundCameraMove;

    public static void SendOnInkSpriteBackgroundChanged(InkSpriteBackgroundEventArgs eventArgs)
    {
        OnInkSpriteBackgroundChanged?.Invoke(eventArgs);
    }
    public static void SendOnInkSpriteBackgroundSubTexChanged(InkSpriteBackgroundEventArgs eventArgs)
    {
        OnInkSpriteBackgroundSubTexChanged?.Invoke(eventArgs);
    }
    public static void SendOnInkSpriteBackgroundCameraMove(InkSpriteBackgroundEventArgs eventArgs)
    {
        OnInkSpriteBackgroundCameraMove?.Invoke(eventArgs);
    }

}

public class InkSpriteBackgroundEventArgs : EventArgs
{
    public InkBackgroundSprite2D targetNode;
    public InkBackgroundData newData;
    public Texture2D newTexture;
    public string newID;
    public bool isVisible;
    public Camera2D targetCamera;
    public Vector2 targetCoords;
    public float camZoom;
    public float camTime;
    public string targetPOI;
}
