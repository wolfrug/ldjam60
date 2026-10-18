using Godot;
using System;

public enum InteractableType
{
    NONE = 0000,
    PICKUP = 1000,
    STORY = 2000,
    EXIT = 3000,
}
public partial class Interactable2D : Area2D
{
    [Export] public string _ID;

    [Export] public InteractableType _type = InteractableType.NONE;
    [Export] public int _value = 1;
    [Export] protected Node2D interactPrompt;
    [Export] protected Node2D interactedWithFlag;
    [Export] protected bool _active = true;
    private bool readyToInteract = false;
    public override void _Ready()
    {
        BodyEntered += OnBodyEnter;
        BodyExited += OnBodyExit;
        ShowInteractPrompt(false);
        if (interactedWithFlag != null)
        {
            interactedWithFlag.Visible = false;
        }
        Active = _active;
    }
    public override void _EnterTree()
    {
        base._EnterTree();
        GlobalEvents.OnLevelContinue += GlobalEvent_OnLevelContinue;
        GlobalEvents.OnLevelPaused += GlobalEvent_OnLevelPaused;
    }
    public override void _ExitTree()
    {
        base._ExitTree();
        GlobalEvents.OnLevelContinue -= GlobalEvent_OnLevelContinue;
        GlobalEvents.OnLevelPaused -= GlobalEvent_OnLevelPaused;
    }
    public void GlobalEvent_OnLevelPaused(LevelEventArgs args)
    {
        Interactable = false;
    }
    public void GlobalEvent_OnLevelContinue(LevelEventArgs args)
    {
        Interactable = true;
    }
    void ShowInteractPrompt(bool show)
    {
        if (interactPrompt != null)
        {
            interactPrompt.Visible = show;
            readyToInteract = show;
        }
    }
    public void OnBodyEnter(Node2D node2D)
    {
        if (node2D is PlayerTopDown)
        {
            ShowInteractPrompt(true);
        }
    }
    public void OnBodyExit(Node2D node2D)
    {
        if (node2D is PlayerTopDown)
        {
            ShowInteractPrompt(false);
        }
    }
    public override void _Process(double delta)
    {
        if (readyToInteract && Active && Interactable)
        {
            if (Input.IsActionPressed("ink_continue"))
            {
                GD.Print("Interacted");
                GlobalEvents.SendOnInteracted(new InteractableEventArgs { targetNode2d = this, ID = _ID, type = _type, value = _value });
                if (_type == InteractableType.PICKUP)
                {
                    Active = false;
                    GlobalEvents.SendOnPlaySFX(new InkAudioEventArgs{audioPath = "Pickup"});
                }
                readyToInteract = false; // even if it can be interacted with again, force moving in/out
                if (interactedWithFlag != null)
                {
                    interactedWithFlag.Visible = true;
                }
            }
        }
    }
    public bool Active
    {
        get
        {
            return _active;
        }
        set
        {
            if (!value)
            {
                _active = false;
                Visible = false;
                ProcessMode = ProcessModeEnum.Disabled;
                ShowInteractPrompt(false);
            }
            else
            {
                _active = true;
                Visible = true;
                ProcessMode = ProcessModeEnum.Inherit;
            }
        }
    }
    public bool Interactable { get; set; } = true;

}
public static partial class GlobalEvents
{

    public delegate void WorldInteractionEvent(InteractableEventArgs eventArgs);

    public static event WorldInteractionEvent OnInteracted;

    public static void SendOnInteracted(InteractableEventArgs eventArgs)
    {
        OnInteracted?.Invoke(eventArgs);
    }
}

// Other event types
public class InteractableEventArgs : EventArgs
{
    public Interactable2D targetNode2d;
    public string ID;
    public InteractableType type;
    public int value;
}
