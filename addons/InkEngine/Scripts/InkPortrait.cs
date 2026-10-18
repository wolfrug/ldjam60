using Godot;
using System;
using GodotInk;
using InkEngine;


public partial class InkPortrait : TextureRect
{

    [Export]
    public RichTextLabel nameText;
    [Export] public string c_setPortrait = "SET_PORTRAIT:";
    private const char c_delimimiter = '^';
    // Called when the node enters the scene tree for the first time.
    public override void _EnterTree()
    {
        SetTexture2D(null);
        SetText("");
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
            if (tag.Contains(c_setPortrait))
            {
                SetPortrait(tag);
            }
        }
    }

    void SetPortrait(string tag)
    {
        GD.Print("Setting portrait according to tag " + tag);
        string cleanedText = tag.Replace(c_setPortrait, "");
        string[] stringParts = cleanedText.Split(c_delimimiter);
        string id = stringParts[0];
        string name = "";
        if (stringParts.Length > 1)
        {
            name = stringParts[1];
        }

        Texture2D tex = GlobalVariables.GetPortraitTexture2D(id);
        if (tex != Texture)
        {
            if (tex != null)
            {
                Tween tween = GetTree().CreateTween();
                tween.TweenProperty(GetNode(GetPath()), "modulate:a", 0f, 0.25f).SetTrans(Tween.TransitionType.Sine);
                tween.TweenCallback(Callable.From(() => SetTexture2D(tex)));
                tween.TweenProperty(GetNode(GetPath()), "modulate:a", 1f, 0.25f).SetTrans(Tween.TransitionType.Sine);
                if (stringParts.Length > 1)
                {
                    SetText(stringParts[1]);
                }
            }
            else
            {
                GD.Print("No Portrait found: nulling");
                Tween tween = GetTree().CreateTween();
                tween.TweenProperty(GetNode(GetPath()), "modulate:a", 0f, 0.25f).SetTrans(Tween.TransitionType.Sine);
            }
        }
        SetText(name);
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
