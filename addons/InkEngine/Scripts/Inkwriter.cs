using Godot;
using GodotInk;
using System;
using System.Collections.Generic;
using InkEngine;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot.NativeInterop;
using System.Globalization;

public partial class Inkwriter : CanvasLayer
{
	[Export]
	private InkStoryData storyData;

	[Export]
	private VBoxContainer storyTextContainer;

	[Export]
	private Control optionButtonContainer;
	[Export]
	private bool clearOptionsOnSelect = false;

	[Export]
	private Control mainPanel;

	[Export]
	private bool startOnReady = true;
	[Export]
	private bool loadOnReady = true;
	[Export]
	private string startKnot = "start";
	[Export]
	private string customFlow = "";

	[Export]
	private bool continueMaximally = true;

	[Export]
	private bool onlyContinueClickInsideMainPanel = false;

	[Export(PropertyHint.InputName, "show_builtin")] string continueInput = "ui_left";
	private bool awaitingClick = false;

	private bool skippingAwait = false;

	private string fullStoryText = "";

	private List<Button> currentChoices = new List<Button> { };

	protected string saveFilePath = "user://inkjam2026_savegame.save";

	protected const string c_customButtonSceneTag = "SET_CUSTOM_BUTTON:";

	protected const string c_customLabelSceneTag = "SET_CUSTOM_LABEL:";

	//private InkArrayFunctions _arrayFunctions;

	// Called when the node enters the scene tree for the first time.
	private static Inkwriter _instance;
	public static Inkwriter instance
	{
		get
		{
			return _instance;
		}
		set
		{
			_instance = value;
		}
	}
	public override void _Ready()
	{
		if (instance == null)
		{
			instance = this;
			var newchild = GD.Load<PackedScene>("res://addons/InkEngine/Scenes/InkArrayFunctions.tscn");
			InkArrayFunctions newFunc = newchild.Instantiate<InkArrayFunctions>();
			AddChild(newFunc);
			newFunc.Init(Story);
		}

		if (customFlow != "")
		{
			Story.SwitchFlow(customFlow);
		}
		if (loadOnReady)
		{
			LoadGame();
		}
		if (startOnReady)
		{
			Play(startKnot);
		}
		GlobalEvents.SendOnInkWriterReady(new InkEventArgs { });
	}
	public InkStory Story { get { return storyData.Story; } }

	public override void _EnterTree()
	{
		GlobalEvents.OnChoiceSelected += GlobalEvent_OnChoiceSelected;
		GlobalEvents.OnGotoKnot += GlobalEvent_OnGoToKnot;
		GlobalEvents.OnRequestSetVariable += GlobalEvent_OnRequestSetVariable;
		base._EnterTree();
	}
	public override void _ExitTree()
	{
		GlobalEvents.OnChoiceSelected -= GlobalEvent_OnChoiceSelected;
		GlobalEvents.OnGotoKnot -= GlobalEvent_OnGoToKnot;
		GlobalEvents.OnRequestSetVariable -= GlobalEvent_OnRequestSetVariable;
		base._ExitTree();
	}

	public void Play(string knot)
	{
		Story.ChoosePathString(knot);
		ContinueStory();
	}

	private async void ContinueStory()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); // We always wait one frame here for...reasons.
		fullStoryText = "";
		if (continueMaximally || !Active) // We also continue maximally if the writer is hidden
		{
			while (Story.CanContinue)
			{
				string storyText = Story.Continue();
				storyText = storyText.Trim();
				if (Story.CurrentTags.Count > 0)
				{
					GlobalEvents.SendOnTagsFound(new InkEventArgs { inkTags = Story.CurrentTags });
				}
				Task waitTask = ParseTags();
				await waitTask;
				InkLabel content = AddText(storyText, Story.CurrentTags as List<string>);
				storyTextContainer.AddChild(content);
				content.Init(storyText, Story.CurrentTags as List<string>);
				GlobalEvents.SendOnLabelCreated(new InkEventArgs { inkTextLabel = content, inkTags = Story.CurrentTags, inktext = storyText });
				//GD.Print("Tag count for label with text: " + storyText + " : " + story.GetCurrentTags().Count);

				fullStoryText += storyText;
			}
		}
		else
		{
			if (Story.CanContinue)
			{
				string storyText = Story.Continue();
				storyText = storyText.Trim();
				if (Story.CurrentTags.Count > 0)
				{
					GlobalEvents.SendOnTagsFound(new InkEventArgs { inkTags = Story.CurrentTags });
				}
				Task waitTask = ParseTags();
				await waitTask;
				fullStoryText += storyText;
				if (!string.IsNullOrWhiteSpace(storyText.Trim()))
				{
					InkLabel content = AddText(storyText, Story.CurrentTags as List<string>);
					storyTextContainer.AddChild(content);
					content.Init(storyText, Story.CurrentTags as List<string>);
					GlobalEvents.SendOnLabelCreated(new InkEventArgs { inkTextLabel = content, inkTags = Story.CurrentTags, inktext = storyText });
					GD.Print("Tag count for label with text: " + storyText + " : " + Story.GetCurrentTags().Count);
					if (!skippingAwait)
					{
						awaitingClick = true;
					}
					else
					{
						skippingAwait = false;
						ContinueStory();
						return;
					}
				}
				else
				{
					ContinueStory();
					return;
				}
			}
		}
		GlobalEvents.SendOnContinue(new InkEventArgs { inktext = fullStoryText });
		//storyText = story.ContinueMaximally();

		if (!Story.CanContinue)
		{
			GatherChoices();
		}
	}

	private async Task ParseTags()
	{
		foreach (string tag in Story.CurrentTags)
		{
			if (tag.Contains("wait"))
			{
				float waitTime = float.Parse(tag.Replace("wait.", ""), System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
				waitTime *= 1000; // convert to milliseconds
				await Task.Delay((int)waitTime);
			}
			if (tag.Contains("clear"))
			{
				ClearAll();
			}
			if (tag.Contains("saveGame"))
			{
				SaveGame();
			}
			if (tag.Contains("loadGame"))
			{
				LoadGame();
			}
			if (tag.Contains("quitGame"))
			{
				QuitGame();
			}
			if (tag.Contains("hideWriter"))
			{
				Active = false;
			}
			if (tag.Contains("showWriter"))
			{
				Active = true;
			}
			if (tag.Contains("continue"))
			{
				skippingAwait = true;
			}
		}
	}

	private void GatherChoices()
	{
		awaitingClick = false;
		List<(InkChoice, Button)> allChoices = new List<(InkChoice, Button)> { };
		List<string> allTags = new List<string> { };
		GD.Print("Ink Writer: Gathering choices count is: " + Story.CurrentChoices.Count);
		foreach (InkChoice choice in Story.CurrentChoices)
		{
			Button addedButton = AddButton(choice);
			allChoices.Add((choice, addedButton));
			allTags.AddRange(choice.Tags);
		}
		GlobalEvents.SendOnContinueFinished(new InkEventArgs { inkChoices = allChoices, inkTags = allTags });
	}

	private Button AddButton(InkChoice choice)
	{
		InkButton button = GetButton(choice);
		button.Init(choice);
		button.Pressed += delegate
		{
			GlobalEvents.SendOnChoiceSelected(new InkEventArgs { inktext = choice.Text, inkTags = choice.Tags as List<string>, inkchoice = choice, inkChoiceButton = button });
		};
		currentChoices.Add(button);
		if (optionButtonContainer != null)
		{
			button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			button.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			optionButtonContainer.AddChild(button);
		}
		else
		{
			storyTextContainer.AddChild(button);
		}
		return button;
	}

	private InkButton GetButton(InkChoice choice, string ButtonID = "Default")
	{
		foreach (string tag in choice.Tags)
		{
			if (tag.Contains(c_customButtonSceneTag))
			{
				ButtonID = tag.Replace(c_customButtonSceneTag, "").Trim();
				GD.Print("Loading custom button with ID: " + ButtonID);
			}
		}
		string buttonPath = GlobalVariables.GetInkButtonPath(ButtonID);

		var scene = GD.Load<PackedScene>(buttonPath);
		InkButton button = scene.Instantiate<InkButton>();

		return button;
	}

	private InkLabel AddText(string text, List<string> tags = default)
	{
		string LabelID = "Default";
		foreach (string tag in tags)
		{
			if (tag.Contains(c_customLabelSceneTag))
			{
				LabelID = tag.Replace(c_customLabelSceneTag, "").Trim();
				GD.Print("Loading custom label with ID: " + LabelID);
			}
		}
		var scene = GD.Load<PackedScene>(GlobalVariables.GetInkLabelPath(LabelID));
		InkLabel label = scene.Instantiate<InkLabel>();
		return label;
	}

	private void GlobalEvent_OnChoiceSelected(InkEventArgs args)
	{
		ChooseChoice(args.inkchoice, args.inkChoiceButton);
	}

	private void ChooseChoice(InkChoice choice, Button button)
	{
		currentChoices.RemoveAll((x) => x == null);
		if (currentChoices.Count > 0)
		{
			foreach (Button btn in currentChoices)
			{
				if (WeakRef(btn) != null && btn != button)
				{
					btn.QueueFree();
				}
			}
			// We only do this if the button is actually part of this writer
			if (button != null && (button.GetParent() == storyTextContainer || button.GetParent() == optionButtonContainer))
			{
				if (clearOptionsOnSelect)
				{
					button.QueueFree();
				}
				else
				{
					button.Disabled = true;
				}
			}
		}
		currentChoices.Clear();
		foreach (InkChoice availableChoice in Story.CurrentChoices)
		{
			if (availableChoice.Index == choice.Index)
			{
				Story.ChooseChoiceIndex(choice.Index);
				ContinueStory();
				break;
			}
		}
	}

	private void GlobalEvent_OnGoToKnot(InkEventArgs args)
	{
		Play(args.inktext);
	}
	private void GlobalEvent_OnRequestSetVariable(InkEventArgs args)
	{
		GD.Print("Ink Writer: Received request to set variable, setting variable " + args.targetVariable + " to " + args.newValue);
		Story.StoreVariable(args.targetVariable, args.newValue);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed(continueInput))
		{
			if (onlyContinueClickInsideMainPanel)
			{
				if (GetViewport().GuiGetHoveredControl() == null || !mainPanel.IsAncestorOf(GetViewport().GuiGetHoveredControl()))
				{
					return;
				}
			}
			if (awaitingClick && !continueMaximally)
			{
				GD.Print("Continuing on click");
				awaitingClick = false;
				ContinueStory();
			}
		}
	}

	public void ClearAll()
	{
		foreach (Node child in storyTextContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	public void SaveGame()
	{
		string json = Story.SaveState();
		using var saveFile = FileAccess.Open(saveFilePath, FileAccess.ModeFlags.Write);
		saveFile.StoreLine(json);
	}
	public void LoadGame()
	{
		if (!FileAccess.FileExists(saveFilePath))
		{
			return; // Error! We don't have a save to load.
		}
		using var saveFile = FileAccess.Open(saveFilePath, FileAccess.ModeFlags.Read);
		var jsonString = saveFile.GetLine();
		Story.LoadState(jsonString);
	}
	public void QuitGame()
	{
		GetTree().Quit();
	}


	private bool active_ = true;
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
				if (value)
				{
					GlobalEvents.SendOnShowWriter(new InkEventArgs { });
				}
				else
				{
					GlobalEvents.SendOnHideWriter(new InkEventArgs { });
				}
			}
			active_ = value;
		}
	}
	void SetActive(bool active)
	{
		GD.Print("We are setting ink writer to active: " + active);
		mainPanel.Visible = active;
	}
	void ShowChildren(bool show)
	{
		foreach (Node child in storyTextContainer.GetChildren())
		{
			child.Set("Active", show);
		}
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
