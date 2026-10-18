using Godot;
using System;
using Godot.Collections;
using System.Linq;

public partial class LevelManager : Node
{
    public string _ID;
    [Export] public Node2D tilemaps;
    [Export] public PlayerTopDown _player;
    [Export] public Array<EnemyNavigationAgent2D> _enemies = new();
    [Export] public Array<Interactable2D> _interactables = new();
    [Export] public Array<Interactable2D> _interactableExits = new();
    public override void _Ready()
    {
        base._Ready();
        foreach (Node child in tilemaps.GetChildren())
        {
            if (child is Interactable2D)
            {
                Interactable2D childInt = child as Interactable2D;
                if (childInt._type == InteractableType.EXIT && !_interactableExits.Contains(childInt))
                {
                    _interactableExits.Add(childInt);
                    childInt.Active = false;
                }
                if (childInt._type != InteractableType.EXIT && !_interactables.Contains(childInt))
                {
                    _interactables.Add(childInt);
                }
            }
            if (child is EnemyNavigationAgent2D)
            {
                if (!_enemies.Contains(child as EnemyNavigationAgent2D))
                {
                    _enemies.Add(child as EnemyNavigationAgent2D);
                }
            }
            if (child is PlayerTopDown && _player == null)
            {
                _player = child as PlayerTopDown;
            }
        }
        GlobalEvents.SendOnLevelLoaded(new LevelEventArgs { manager = this });
        GlobalEvents.SendOnPlayAmbience(new InkAudioEventArgs{audioPath = "AmbienceRuin"});
    }
    public override void _EnterTree()
    {
        base._EnterTree();
        GlobalEvents.OnInteracted += GlobalEvents_Interacted;
        GlobalEvents.OnLevelPaused += GlobalEvents_OnLevelPaused;
        GlobalEvents.OnLevelContinue += GlobalEvents_OnLevelContinue;
    }
    public override void _ExitTree()
    {
        base._ExitTree();
        GlobalEvents.OnInteracted -= GlobalEvents_Interacted;
        GlobalEvents.OnLevelPaused -= GlobalEvents_OnLevelPaused;
        GlobalEvents.OnLevelContinue -= GlobalEvents_OnLevelContinue;
    }

    public void GlobalEvents_OnLevelContinue(LevelEventArgs args)
    {
        ShowLevel(true);
    }
    public void GlobalEvents_OnLevelPaused(LevelEventArgs args)
    {
        ShowLevel(false);
    }

    public void GlobalEvents_Interacted(InteractableEventArgs args)
    {
        if (_interactables.Contains(args.targetNode2d))
        {
            _interactables.Remove(args.targetNode2d);
        }
        if (_interactables.Count == 0)
        {
            foreach (Interactable2D exit in _interactableExits)
            {
                exit.Active = true;
            }
        }
        if (args.type == InteractableType.EXIT)
        {
            GD.Print("Level exit reached!");
            GlobalEvents.SendOnLevelEnded(new LevelEventArgs { manager = this, interactableTrigger = args.targetNode2d });
            QueueFree();
        }
    }

    public void ShowLevel(bool show)
    {
        tilemaps.Visible = show;
    }
}

public static partial class GlobalEvents
{
    public delegate void LevelEvent(LevelEventArgs eventArgs);

    public static event LevelEvent OnRequestLoadLevel;
    public static event LevelEvent OnLevelLoaded;
    public static event LevelEvent OnLevelEnded;

    public static event LevelEvent OnLevelPaused;
    public static event LevelEvent OnLevelContinue;

    public static void SendOnRequestLoadLevel(LevelEventArgs eventArgs)
    {
        OnRequestLoadLevel?.Invoke(eventArgs);
    }
    public static void SendOnLevelLoaded(LevelEventArgs eventArgs)
    {
        OnLevelLoaded?.Invoke(eventArgs);
    }
    public static void SendOnLevelEnded(LevelEventArgs eventArgs)
    {
        OnLevelEnded?.Invoke(eventArgs);
    }
    public static void SendOnLevelPaused(LevelEventArgs eventArgs)
    {
        OnLevelPaused?.Invoke(eventArgs);
    }
    public static void SendOnLevelContinue(LevelEventArgs eventArgs)
    {
        OnLevelContinue?.Invoke(eventArgs);
    }
}

// Other event types
public class LevelEventArgs : EventArgs
{
    public LevelManager manager;
    public Interactable2D interactableTrigger;
    public string levelID;
}
