using Godot;
using System;
using GodotInk;
using InkEngine;
using System.Collections.Generic;
using System.Text;
using System.Linq;


public partial class InkInventory : CanvasLayer
{
	[Export]
	public Control parent;

	[Export]
	public Control mainPanel;

	[Export] public InkButton cancelButton;

	[Export] private string c_useInventoryTag = "SET_INVENTORY:";
	[Export] private string c_exitInventoryTag = "INVENTORY_CANCEL";
	private const char c_delimimiter = '^';

	private List<Button> currentInventoryButtons = new List<Button> { };

	public override void _EnterTree()
	{
		GlobalEvents.OnContinueFinished += GlobalEvent_OnContinueFinished;
		GlobalEvents.OnChoiceSelected += GlobalEvent_OnChooseChoice;
		ClearInventory();
		Active = false;
		base._EnterTree();
	}
	public override void _ExitTree()
	{
		GlobalEvents.OnContinueFinished -= GlobalEvent_OnContinueFinished;
		GlobalEvents.OnChoiceSelected -= GlobalEvent_OnChooseChoice;
		base._ExitTree();
	}

	private void ClearInventory()
	{
		foreach (Node n in parent.GetChildren())
		{
			n.QueueFree();
		}
	}

	private void GlobalEvent_OnContinueFinished(InkEventArgs args)
	{
		GD.Print("InkInventory: Received on continue finished with " + args.inkChoices.Count + " choices counted");
		bool foundTags = false;
		if (args.inkChoices.Count > 0)
		{
			foreach ((InkChoice, Button) kvp in args.inkChoices)
			{
				foreach (string tag in kvp.Item1.Tags)
				{
					if (tag.Contains(c_useInventoryTag))
					{
						if (!foundTags)
						{
							ClearInventory();
							foundTags = true;
						}
						GD.Print("Found a target tag: " + tag);
						if (tag.Contains(c_exitInventoryTag))
						{
							InitExitButton(kvp.Item1, kvp.Item2);
						}
						else
						{
							InitButton(kvp.Item1, kvp.Item2);
						}
					}
				}
			}
			if (foundTags)
			{
				Active = true;
			}
		}
	}

	private void InitButton(InkChoice choice, Button button)
	{
		var scene = GD.Load<PackedScene>(GlobalVariables.c_inkInventoryButtonScene);
		InkInventoryButton inventorybutton = scene.Instantiate<InkInventoryButton>();
		InitInventoryButton(choice, inventorybutton);
		inventorybutton.Pressed += delegate
		{
			GlobalEvents.SendOnChoiceSelected(new InkEventArgs { inktext = choice.Text, inkTags = choice.Tags as List<string>, inkchoice = choice, inkChoiceButton = inventorybutton });
		};
		parent.AddChild(inventorybutton);
		currentInventoryButtons.Add(inventorybutton);
		// destroy og button
		button.QueueFree();
		GD.Print("Added new button and removed old button with text " + inventorybutton.Text);
	}
	Action exitButtonAction;
	private void InitExitButton(InkChoice choice, Button button)
	{
		if (exitButtonAction != null)
		{
			cancelButton.Pressed -= exitButtonAction;
		}
		exitButtonAction = () =>
			{
				GlobalEvents.SendOnChoiceSelected(new InkEventArgs { inktext = choice.Text, inkTags = choice.Tags as List<string>, inkchoice = choice, inkChoiceButton = cancelButton });
			};
		cancelButton.Pressed += exitButtonAction;
		cancelButton.Disabled = button.Disabled;
		//cancelButton.Visible = true;
		button.QueueFree();
	}
	private void InitInventoryButton(InkChoice choice, InkInventoryButton button)
	{
		GD.Print("Initing inventory button with variables: " + String.Join(',', choice.Tags.Select(v => v == null ? "null" : v.ToString())));
		foreach (string tag in choice.Tags)
		{
			if (tag.Contains(c_useInventoryTag))
			{
				string cleanedText = tag.Replace(c_useInventoryTag, "");
				
				string[] stringParts = cleanedText.Split(c_delimimiter);
				string description = stringParts[0];
				string iconid = stringParts[1];
				bool enabled = stringParts[2].Contains("true") || stringParts[2].Contains("True");
				int stack = 1;
				int.TryParse(stringParts[3], out stack);
				button.Init(choice.Text, description, iconid, enabled, stack);
			}
		}
	}

	private void GlobalEvent_OnChooseChoice(InkEventArgs args)
	{
		if (currentInventoryButtons.Count > 0 && currentInventoryButtons.Contains(args.inkChoiceButton) || args.inkChoiceButton == cancelButton)
		{
			GD.Print("Clearing inventory buttons");
			foreach (InkInventoryButton btn in currentInventoryButtons)
			{
				if (WeakRef(btn) != null)
				{
					btn.QueueFree();
				}
			}
			currentInventoryButtons.Clear();
			Active = false;
			//cancelButton.Visible = false;
		}
	}

	private bool active_ = false;
	public bool Active
	{
		get
		{
			return active_;
		}
		set
		{
			if (active_ != value)
			{
				FadeIn(value);
				active_ = value;
			}
		}
	}
	void SetActive(bool active)
	{
		GD.Print("We are setting ink inventory to active: " + active);
		mainPanel.Visible = active;
	}
	void ShowChildren(bool show)
	{
		foreach (Node child in parent.GetChildren())
		{
			if (WeakRef(child) != null)
			{
				(child as InkInventoryButton).Active = show;
			}
		}
		cancelButton.Visible = show;
	}

	Tween _tween;
	void FadeIn(bool fadeIn)
	{
		if (_tween != null)
		{
			_tween.Kill();
		}
		_tween = mainPanel.CreateTween();
		_tween.SetTrans(Tween.TransitionType.Sine);
		_tween.SetEase(Tween.EaseType.InOut);
		
		float current = mainPanel.Modulate.A;
		float goal = fadeIn ? 1f : 0f;
		if (fadeIn)
		{
			SetActive(true);
			foreach (Node child in parent.GetChildren())
			{
				if (WeakRef(child) != null)
				{
					(child as Control).Visible = false;
				}
			}
			ShowChildren(false);
		}
		else
		{
			ShowChildren(false);
		}
		_tween.TweenProperty(mainPanel, "modulate:a", goal, 0.25f);
		if (!fadeIn)
		{
			_tween.TweenCallback(Callable.From(() => SetActive(fadeIn)));
		}
		else
		{
			_tween.TweenCallback(Callable.From(() => ShowChildren(fadeIn)));
		}
	}

}
