using Godot;
using GodotInk;
using System;
using InkEngine;
using System.Collections.Generic;

public partial class InkLabel : Control
{
	[Export]
	protected bool fadeInText = true;
	[Export]
	protected bool writeInText = false;
	[Export]
	protected float fadeInSpeed = 0.5f;
	[Export]
	protected RichTextLabel textLabel;
	[Export]
	protected PanelContainer container;

	protected bool isFloating = false; // Whether or not this container is off somewhere floating
	protected Vector2 floatPosition = Vector2.Zero;

	protected const string c_changeThemeTag = "SET_THEME:";

	protected const string c_setWorldPosTag = "SET_POS:";

	protected const char c_delimiter = '^';

	protected const string c_initAudio = "InkContinue";

	protected List<string> attachedTags = new List<string> { };
	public virtual void Init(string text, List<string> tags = default)
	{
		Text = text;
		if ((fadeInText || writeInText) && WeakRef(this) != null)
		{
			AnimateLabel();
		}
		attachedTags = tags;
		GD.Print("Attached tags count: " + attachedTags.Count);
		if (attachedTags != null && attachedTags.Count > 0)
		{
			foreach (string tag in attachedTags)
			{
				GD.Print("Label: querying tag " + tag);
				if (tag.Contains(c_changeThemeTag))
				{
					SetTheme(tag);
				}
				if (tag.Contains(c_setWorldPosTag))
				{
					SetWorldSpacePosition(tag);
				}
			}
		}
		GlobalEvents.SendOnPlaySFX(new InkAudioEventArgs { audioPath = c_initAudio });
	}
	public override void _ExitTree()
	{
		base._ExitTree();
		if (_tween != null)
		{
			_tween.Kill();
		}
	}
	public string Text
	{
		get
		{
			return textLabel.Text;
		}
		set
		{
			textLabel.Text = value;
		}
	}
	public List<string> Tags
	{
		get
		{
			return attachedTags;
		}
		set
		{
			attachedTags = value;
		}
	}
	public virtual void SetTheme(string tag)
	{
		GD.Print("Setting theme on label to " + tag);
		string cleanedTag = tag.Replace(c_changeThemeTag, "");
		StyleBox newTheme = GlobalVariables.GetStyleBox(cleanedTag);
		if (newTheme != null)
		{
			container.AddThemeStyleboxOverride("Panel", newTheme);
			container.Set("theme_override_styles/panel", newTheme);
		}
	}
	public virtual void SetWorldSpacePosition(string tag)
	{ // #SET_POS:face OR #SET_POS:344,200 OR #SET_POS:face^true (for top level true/false)
		string cleanedTag = tag.Replace(c_setWorldPosTag, "");
		string[] stringParts = cleanedTag.Split(c_delimiter);
		GD.Print("Received SET WORLD SPACE POSITION coords, with parts: " + cleanedTag);
		string[] coordinates = stringParts[0].Split(',');
		bool toplevel = false;
		if (stringParts.Length > 1)
		{
			toplevel = stringParts[1] == "true";
		}

		Vector2 coords = Vector2.Zero;
		if (coordinates.Length == 1)
		{
			coords = InkBackgroundSprite2D.instance.presetCoordinates[coordinates[0]];
		}
		else
		{
			coords = new Vector2(coordinates[0].ToFloat(), coordinates[1].ToFloat());
		}
		floatPosition = coords;
		GD.Print("Correct float position is: " + coords);
		OffsetTransformEnabled = true;
		OffsetTransformVisualOnly = false;
		TopLevel = toplevel;
		isFloating = true;
	}
	public virtual void AnimateLabel()
	{

		if (fadeInText)
		{
			textLabel.SelfModulate = new Color(textLabel.Modulate, 0f);
			Tween tween = textLabel.CreateTween();
			tween.TweenProperty(textLabel, "self_modulate:a", 1f, 0.55f).SetTrans(Tween.TransitionType.Sine);
		}
		if (writeInText)
		{
			float fadeInTime = textLabel.Text.Length / (fadeInSpeed * 100f);
			textLabel.VisibleRatio = 0f;
			Tween tween2 = textLabel.CreateTween();
			tween2.TweenProperty(textLabel, "visible_ratio", 1f, fadeInTime).SetTrans(Tween.TransitionType.Sine);
		}
	}
	public override void _Ready()
	{
		if (fadeInText)
		{
			AnimateLabel();
		}
		GlobalEvents.SendOnUIShow(new UIEventArgs { targetNode = this });
		base._Ready();
	}
	public override void _Process(double delta)
	{
		if (isFloating)
		{
			// World -> screen (Camera2D position/zoom/rotation included)
			Vector2 screen = GetViewport().GetCanvasTransform() * floatPosition;

			// Screen -> this control's canvas. If the control is in the default
			// world canvas the two transforms cancel and you get floatPosition back;
			// under a CanvasLayer you get proper UI coordinates.
			Vector2 inMyCanvas = GetCanvasTransform().AffineInverse() * screen;

			// Centered horizontally, sitting just above the point
			Vector2 desiredTopLeft = inMyCanvas
									 - new Vector2(Size.X / 2f, Size.Y);

			// Layout (offset-free) origin, correct in either VisualOnly mode
			Transform2D xf = GetGlobalTransform();
			Vector2 layoutOrigin = OffsetTransformVisualOnly
				? xf.Origin
				: xf.Origin - xf.BasisXform(OffsetTransformPosition);

			// Offset position is in the control's local space, so undo the
			// parent's rotation/scale
			OffsetTransformPosition = xf.BasisXformInv(desiredTopLeft - layoutOrigin);
		}
	}

	protected bool active_ = true;
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
		//GD.Print("We are setting ink writer to active: " + active);
		Visible = active;
	}

	public override GodotObject _MakeCustomTooltip(string text)
	{
		var scene = GD.Load<PackedScene>(GlobalVariables.c_inkTooltipScene);
		InkTooltip tooltip = scene.Instantiate<InkTooltip>();
		tooltip.textLabel.Text = text.Trim();
		return tooltip;
	}

	Tween _tween;
	void FadeIn(bool fadeIn)
	{
		if (_tween != null)
		{
			_tween.Kill();
		}
		_tween = GetTree().CreateTween();
		_tween.SetTrans(Tween.TransitionType.Sine);
		_tween.SetEase(Tween.EaseType.InOut);
		float goal = fadeIn ? 1f : 0f;
		float current = Modulate.A;
		if (fadeIn)
		{
			SetActive(true);
		}
		_tween.TweenProperty(GetNode(GetPath()), "modulate:a", goal, 0.25f).SetTrans(Tween.TransitionType.Sine);
		if (!fadeIn)
		{
			_tween.TweenCallback(Callable.From(() => SetActive(fadeIn)));
		}
	}
}
