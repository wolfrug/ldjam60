using Godot;
using System;
using GodotInk;
using InkEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public partial class InkUI : CanvasLayer
{
	[Export]
	public Godot.Collections.Dictionary<string, InkButton> customButtons = new Godot.Collections.Dictionary<string, InkButton> { };
	[Export]
	public Godot.Collections.Dictionary<string, InkLabel> customLabels = new Godot.Collections.Dictionary<string, InkLabel> { };

	[Export]
	public Godot.Collections.Dictionary<string, InkUINode> customActivatorNodes = new Godot.Collections.Dictionary<string, InkUINode> { };

	[Export]
	public Godot.Collections.Array<string> customTagTriggers = new Godot.Collections.Array<string>{};
	private List<(InkButton, Action)> activatedButtons = new List<(InkButton, Action)> { };

	protected const char c_delimiter = '^';

	protected const string c_disableAllButtons = "INK_UI_DISABLE_ALL_BUTTONS";
	protected const string c_disableAllLabels = "INK_UI_DISABLE_ALL_LABELS";
	protected const string c_deactivateAllNodes = "INK_UI_DISABLE_ALL_NODES";

	[Signal] public delegate void CustomTagEventReceivedEventHandler();

	public override void _EnterTree()
	{
		GlobalEvents.OnContinueFinished += GlobalEvent_OnContinueFinished;
		GlobalEvents.OnChoiceSelected += GlobalEvent_OnChoiceSelected;
		GlobalEvents.OnLabelCreated += GlobalEvent_OnLabelCreated;
		GlobalEvents.OnTagsFound += GlobalEvent_OnTagsFound;
		//DisableAllButtons();
		base._EnterTree();
	}
	public override void _ExitTree()
	{
		GlobalEvents.OnContinueFinished -= GlobalEvent_OnContinueFinished;
		GlobalEvents.OnChoiceSelected -= GlobalEvent_OnChoiceSelected;
		GlobalEvents.OnLabelCreated -= GlobalEvent_OnLabelCreated;
		GlobalEvents.OnTagsFound -= GlobalEvent_OnTagsFound;
		base._ExitTree();
	}

	protected virtual void GlobalEvent_OnTagsFound(InkEventArgs args)
	{
		if (args.inkTags.Contains(c_deactivateAllNodes))
		{
			DisableAllNodes();
		}
		if (args.inkTags.Contains(c_disableAllButtons))
		{
			DisableAllButtons();
		}
		if (args.inkTags.Contains(c_disableAllLabels))
		{
			DisableAllLabels();
		}
		foreach (string inktag in args.inkTags)
		{
			if (HasTag(inktag, out string tag))
			{
				SetNodeActive(tag);
			}
		}
	}

	protected virtual void GlobalEvent_OnContinueFinished(InkEventArgs args)
	{
		GD.Print("InkUI (" + Name + "): Received on continue finished with " + args.inkChoices.Count + " choices counted");
		if (args.inkChoices.Count > 0)
		{
			foreach (string inktag in args.inkTags)
			{
				if (HasTag(inktag, out string outtag))
				{
					GD.Print("InkUI found custom button with tag " + inktag);

					foreach ((InkChoice, Button) kvp in args.inkChoices)
					{
						foreach (string tag in kvp.Item1.Tags)
						{
							if (outtag == tag)
							{
								InitButton(kvp.Item1, kvp.Item2, tag);
							}
						}
					}
				}
			}
		}
	}
	protected virtual void GlobalEvent_OnLabelCreated(InkEventArgs args)
	{
		foreach (string inktag in args.inkTags)
		{
			if (HasTag(inktag, out string tag))
			{
				InitLabel(args.inkTextLabel, tag);
			}
		}
	}

	protected virtual void GlobalEvent_OnChoiceSelected(InkEventArgs args)
	{
		GD.Print("InkUI: received on choice selected from button with text " + args.inktext);
		if (activatedButtons.Count > 0)
		{
			GD.Print("InkUI: Disabling all custom buttons");
			DisableAllButtons();
		}
	}

	protected virtual void SetNodeActive(string tag)
	{
		string[] stringParts = tag.Split(c_delimiter);
		string checkTag = stringParts[0];
		if (customActivatorNodes.ContainsKey(checkTag))
		{
			bool activeState = stringParts[1] == "true" ? true : false;
			InkUINode targetNode = customActivatorNodes[checkTag];
			if (activeState)
			{
				GD.Print("InkUI: Turning on target node with id " + checkTag);
				targetNode.Activate(tag);
			}
			else
			{
				GD.Print("InkUI: Turning off node with id " + checkTag);
				targetNode.Deactivate(tag);
			}
		} else if (customTagTriggers.Contains(checkTag))
		{
			CustomTagReceived(tag);
		}
	}

	protected virtual void CustomTagReceived(string tag)
	{
		string[] stringParts = tag.Split(c_delimiter);
		EmitSignal(SignalName.CustomTagEventReceived, stringParts);
	}

	protected virtual bool HasTag(string tag, out string outTag)
	{
		string[] stringParts = tag.Split(c_delimiter);
		string checkTag = stringParts[0];
		if (customButtons.ContainsKey(checkTag))
		{
			outTag = tag;
			return true;
		}
		if (customLabels.ContainsKey(checkTag))
		{
			outTag = tag;
			return true;
		}
		if (customActivatorNodes.ContainsKey(checkTag))
		{
			outTag = tag;
			return true;
		}
		if (customTagTriggers.Contains(checkTag))
		{
			outTag = tag;
			return true;
		}
		outTag = "";
		return false;
	}

	public virtual void DisableAllButtons()
	{
		foreach ((InkButton, Action) act in activatedButtons)
		{
			act.Item1.RemoveCustomAction();
		}
		foreach (KeyValuePair<string, InkButton> kvp in customButtons)
		{
			if (WeakRef(kvp.Value) == null)
			{
				GD.PrintErr("Ink UI: Custom button with ID " + kvp.Key + " is null!");
				continue;
			}
			kvp.Value.Disabled = true;
		}
		activatedButtons.Clear();
	}
	public virtual void DisableAllLabels()
	{
		foreach (KeyValuePair<string, InkLabel> kvp in customLabels)
		{
			kvp.Value.Text = "";
		}
	}
	public virtual void DisableAllNodes()
	{
		foreach (KeyValuePair<string, InkUINode> kvp in customActivatorNodes)
		{
			kvp.Value.Deactivate("");
		}
	}

	protected virtual void InitButton(InkChoice choice, Button button, string tag)
	{
		string[] stringParts = tag.Split(c_delimiter);
		string checkTag = stringParts[0];
		if (customButtons.TryGetValue(checkTag, out InkButton replacementButton))
		{
			GD.Print("Initializing Ink UI button with tag " + tag);
			replacementButton.Init(choice);
			replacementButton.Disabled = button.Disabled;
			//replacementButton.Text = choice.Text;
			//replacementButton.Icon = button.Icon;
			//replacementButton.Disabled = button.Disabled;
			Action delegateAction = () =>
			{
				GlobalEvents.SendOnChoiceSelected(new InkEventArgs { inktext = choice.Text, inkTags = choice.Tags as List<string>, inkchoice = choice, inkChoiceButton = replacementButton });
				DisableAllButtons();
			};
			replacementButton.OnPressedDelegate = delegateAction;
			replacementButton.Pressed += delegateAction;
			// destroy og button
			button.QueueFree();
			activatedButtons.Add((replacementButton, delegateAction));
			GD.Print("Added new button and removed old button with text " + replacementButton.Text);
		}
	}
	protected virtual void InitLabel(InkLabel label, string tag)
	{
		if (customLabels.TryGetValue(tag, out InkLabel replacementLabel))
		{
			replacementLabel.Init(label.Text, label.Tags);
			label.QueueFree();
		}
	}
}
