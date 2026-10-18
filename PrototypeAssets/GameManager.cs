using Godot;
using System;
using Godot.Collections;
using Ink.Parsed;
using System.Diagnostics.Tracing;
using System.Linq;

public partial class GameManager : Node
{
    [Export] public Dictionary<string, PackedScene> levels = new();
    [Export] public string returnFromLevelKnot = "continuePlay";
    [Export] public Node2D storyBackground;

    private LevelManager _currentLoadedLevel;

    private const string c_loadLevelTag = "LOAD_LEVEL:";
    private const string c_continueLevelTag = "UNPAUSE_LEVEL";
    public override void _Ready()
    {
        base._Ready();
    }
    public override void _EnterTree()
    {
        base._EnterTree();
        GlobalEvents.OnRequestLoadLevel += GlobalEvent_OnRequestLevelLoaded;
        GlobalEvents.OnInteracted += GlobalEvent_OnInteracted;
        GlobalEvents.OnTagsFound += GlobalEvent_OnTagsFound;
    }
    public override void _ExitTree()
    {
        base._ExitTree();
        GlobalEvents.OnRequestLoadLevel -= GlobalEvent_OnRequestLevelLoaded;
        GlobalEvents.OnInteracted -= GlobalEvent_OnInteracted;
        GlobalEvents.OnTagsFound -= GlobalEvent_OnTagsFound;
    }

    public void GlobalEvent_OnTagsFound(InkEventArgs args)
    {
        foreach (string tag in args.inkTags)
        {
            if (tag.Contains(c_loadLevelTag))
            {
                string cleantag = tag.Replace(c_loadLevelTag, "");
                GlobalEvents.SendOnRequestLoadLevel(new LevelEventArgs { levelID = cleantag });
                return;
            }
            if (tag.Contains(c_continueLevelTag))
            {
                GlobalEvents.SendOnLevelContinue(new LevelEventArgs { });
                storyBackground.Visible = false;
                storyBackground.ProcessMode = ProcessModeEnum.Disabled;
                Inkwriter.instance.Active = false;
            }
        }
    }

    public void GlobalEvent_OnInteracted(InteractableEventArgs args)
    {
        if (args.type == InteractableType.EXIT)
        {
            storyBackground.Visible = true;
            storyBackground.ProcessMode = ProcessModeEnum.Inherit;
            Inkwriter.instance.Active = true;
            Inkwriter.instance.Play(args.ID);
        }
        if (args.type == InteractableType.STORY && !Inkwriter.instance.Active)
        {
            storyBackground.Visible = true;
            storyBackground.ProcessMode = ProcessModeEnum.Inherit;
            Inkwriter.instance.Active = true;
            Inkwriter.instance.Play(args.ID);
            GlobalEvents.SendOnLevelPaused(new LevelEventArgs { });
        }
    }

    public void GlobalEvent_OnRequestLevelLoaded(LevelEventArgs args)
    {
        if (_currentLoadedLevel == null || _currentLoadedLevel._ID != args.levelID)
        {
            if (levels.TryGetValue(args.levelID, out PackedScene scene))
            {
                LevelManager level = scene.Instantiate<LevelManager>();
                AddChild(level);
                _currentLoadedLevel = level;
                storyBackground.Visible = false;
                storyBackground.ProcessMode = ProcessModeEnum.Disabled;
                Inkwriter.instance.Active = false;
            }
        }
    }

}
