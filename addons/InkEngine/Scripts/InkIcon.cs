using Godot;
using System;
using GodotInk;
using InkEngine;


public partial class InkIcon : TextureRect
{
    [Export]
    public bool nullSelfOnInit = true;
    [Export]
    public RichTextLabel nameText;
    [Export]
    public string c_tagListener = "SET_ICON:Default";
    private const char c_delimimiter = '^';
    // Called when the node enters the scene tree for the first time.
    public override void _EnterTree()
    {
        if (nullSelfOnInit)
        {
            SetTexture2D(null);
            SetText("");
        }
        GlobalEvents.OnTagsFound += GlobalEvent_OnTagsFound;
        base._EnterTree();
    }
    public override void _ExitTree()
    {
        GlobalEvents.OnTagsFound -= GlobalEvent_OnTagsFound;
        base._ExitTree();
    }

    void GlobalEvent_OnTagsFound(InkEventArgs args)
    {
        //GD.Print("Looking for background tags");
        foreach (string tag in args.inkTags)
        {
            //GD.Print("looking for " + c_setPortrait + " in tag " + tag);
            if (tag.Contains(c_tagListener))
            {
                SetIcon(tag);
            }
        }
    }

    public void SetIcon(string tag)
    {
        GD.Print("Setting icon according to tag " + tag);
        string cleanedText = tag.Replace(c_tagListener, "");
        string[] stringParts = cleanedText.Split(c_delimimiter);
        string id = stringParts[0];

        Texture2D tex = GlobalVariables.GetPortraitTexture2D(id);
        SetIcon(tex, stringParts.Length > 1 ? stringParts[1] : "");
    }
    public void SetIcon(Texture2D tex, string text = "")
    {
        if (tex != Texture)
        {
            if (tex != null)
            {
                Tween tween = GetTree().CreateTween();
                tween.TweenProperty(GetNode(GetPath()), "modulate:a", 0f, 0.25f).SetTrans(Tween.TransitionType.Sine);
                tween.TweenCallback(Callable.From(() => SetTexture2D(tex)));
                tween.TweenProperty(GetNode(GetPath()), "modulate:a", 1f, 0.25f).SetTrans(Tween.TransitionType.Sine);
                if (text != "")
                {
                    SetText(text);
                }
            }
            else
            {
                GD.Print("No Portrait found: nulling");
                Tween tween = GetTree().CreateTween();
                tween.TweenProperty(GetNode(GetPath()), "modulate:a", 0f, 0.25f).SetTrans(Tween.TransitionType.Sine);
            }
        }
    }
    public void SetTexture2D(Texture2D texture)
    {
        Texture = texture;
    }
    public void SetText(string text)
    {
        nameText.Text = text;
    }

}
