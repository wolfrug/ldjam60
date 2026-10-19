using Godot;
using System;
using Godot.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;

public partial class GameManager : Node
{
    [Export] public Godot.Collections.Dictionary<string, PackedScene> levels = new();
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
        GlobalEvents.OnDamaged += GlobalEvent_OnDamaged;
    }
    public override void _ExitTree()
    {
        base._ExitTree();
        GlobalEvents.OnRequestLoadLevel -= GlobalEvent_OnRequestLevelLoaded;
        GlobalEvents.OnInteracted -= GlobalEvent_OnInteracted;
        GlobalEvents.OnTagsFound -= GlobalEvent_OnTagsFound;
        GlobalEvents.OnDamaged -= GlobalEvent_OnDamaged;
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
            HandleEndLevel(args.ID);
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
                level._ID = args.levelID;
                _currentLoadedLevel = level;
                storyBackground.Visible = false;
                storyBackground.ProcessMode = ProcessModeEnum.Disabled;
                Inkwriter.instance.Active = false;
            }
        }
    }
    public void GlobalEvent_OnDamaged(AgentEventArgs args)
    {
        if (args.player != null && args.healthLeft <= 0)
        {
            // Player died!
            ResetCurrentLevelWithDelay();
        }
    }
    public void HandleEndLevel(string targetknot)
    {
        storyBackground.Visible = true;
        storyBackground.ProcessMode = ProcessModeEnum.Inherit;
        Inkwriter.instance.Active = true;
        Inkwriter.instance.Play(targetknot);
        foreach (KeyValuePair<string, int> kvp in _currentLoadedLevel._cachedPickups)
        {
            GlobalEvents.SendOnRequestInkFunction(new InkEventArgs { targetVariable = "Pickup", parameters = new Array<Variant> { kvp.Key, kvp.Value } });
        }
        ;
        GD.Print("Level exit reached!");
        GlobalEvents.SendOnLevelEnded(new LevelEventArgs { });
        _currentLoadedLevel.QueueFree();
        _currentLoadedLevel = null;
    }
    public async void ResetCurrentLevelWithDelay(double delay = 2000)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(delay));
        string currentlevelID = _currentLoadedLevel._ID;
        _currentLoadedLevel.QueueFree();
        _currentLoadedLevel = null;
        GlobalEvents.SendOnRequestLoadLevel(new LevelEventArgs { levelID = currentlevelID });
    }

}
