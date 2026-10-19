using Godot;
using System;
using GodotInk;
using InkEngine;
using System.Collections.Generic;

[GlobalClass, Tool]
public partial class InkStoryData : Resource
{
    [Export]
    private InkStory inkFile;

    private InkStory story;

    [ExportToolButton("Run")]
    public Callable ToolButton => Callable.From(SaveInkRes);

    private const string storyResourcePath = "res://main_story.tres";

    public InkStory Story
    {
        get
        {
            if (story == null)
            {
                SaveInkRes();
            }
            return story;
        }
    }

    public void SaveInkRes()
    {
        if (inkFile != null)
        {
            var rawstory = ResourceLoader.Load<GodotInk.InkStory>(inkFile.ResourcePath);
            var err = ResourceSaver.Save(rawstory, storyResourcePath);
            GD.Print($"save result: {err}");
            story = ResourceLoader.Load<GodotInk.InkStory>(storyResourcePath);
        }
        else
        {
            story = ResourceLoader.Load<GodotInk.InkStory>(storyResourcePath);
            GD.PrintErr("No Ink File set in InkStoryData!");
        }
    }
}
public static partial class GlobalEvents
{
    public delegate void InkDialogueEvent(InkEventArgs eventArgs);

    public static event InkDialogueEvent OnInkWriterReady;
    public static event InkDialogueEvent OnContinue;
    public static event InkDialogueEvent OnContinueFinished;
    public static event InkDialogueEvent OnChoiceSelected;
    public static event InkDialogueEvent OnTagsFound;
    public static event InkDialogueEvent OnLabelCreated;
    public static event InkDialogueEvent OnGotoKnot;
    public static event InkDialogueEvent OnShowWriter;
    public static event InkDialogueEvent OnHideWriter;
    public static event InkDialogueEvent OnRequestSetVariable;
    public static event InkDialogueEvent OnRequestInkFunction;
    public static event InkDialogueEvent OnRunInkFunction;


    public static void SendOnInkWriterReady(InkEventArgs args)
    {
        OnInkWriterReady?.Invoke(args);
    }
    public static void SendOnContinue(InkEventArgs eventArgs)
    {
        OnContinue?.Invoke(eventArgs);
    }
    public static void SendOnContinueFinished(InkEventArgs eventArgs)
    {
        OnContinueFinished?.Invoke(eventArgs);
    }
    public static void SendOnChoiceSelected(InkEventArgs eventArgs)
    {
        OnChoiceSelected?.Invoke(eventArgs);
    }
    public static void SendOnTagsFound(InkEventArgs eventArgs)
    {
        OnTagsFound?.Invoke(eventArgs);
    }
    public static void SendOnLabelCreated(InkEventArgs eventArgs)
    {
        OnLabelCreated?.Invoke(eventArgs);
    }

    public static void SendOnGoToKnot(InkEventArgs eventArgs)
    {
        OnGotoKnot?.Invoke(eventArgs);
    }
    public static void SendOnShowWriter(InkEventArgs eventArgs)
    {
        OnShowWriter?.Invoke(eventArgs);
    }
    public static void SendOnHideWriter(InkEventArgs eventArgs)
    {
        OnHideWriter?.Invoke(eventArgs);
    }

    public static void SendOnRequestSetVariable(InkEventArgs eventArgs)
    {
        OnRequestSetVariable?.Invoke(eventArgs);
    }
    public static void SendOnRequestInkFunction(InkEventArgs eventArgs)
    {
        OnRequestInkFunction?.Invoke(eventArgs);
    }
    public static void SendOnRunInkFunction(InkEventArgs eventArgs)
    {
        OnRunInkFunction?.Invoke(eventArgs);
    }
}
public class InkEventArgs : EventArgs
{
    public string inktext;
    public InkLabel inkTextLabel;
    public IReadOnlyList<string> inkTags;
    public List<(InkChoice, Button)> inkChoices = new List<(InkChoice, Button)> { }; // These are all the choices, if any, once Continue is done
    public InkChoice inkchoice; // This is the specific choice (and button) being clicked on etc
    public Button inkChoiceButton;
    public string targetVariable; // This is for requesting a variable change
    public Variant newValue; // this is the new value
    public Godot.Collections.Array<Variant> parameters; // this is for the params for the function 

}
