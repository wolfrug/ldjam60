using Godot;
using GodotInk;
using System;
using System.Collections.Generic;
using InkEngine;

public partial class InkButton : Button
{
	[Export]
	public string onClickedSFX = "ButtonClick";
	[Export]
	public string onHoverSFX = "ButtonHover";
	protected const string c_tagIcon = "SET_ICON:";
	protected const string c_disabledIcon = "DISABLED";
	protected const string c_changeThemeTag = "SET_THEME:";
	private const string c_setWorldPosTag = "SET_POS:";
	private const char c_delimiter = '^';
	public Action OnPressedDelegate;

	private bool isFloating = false; // Whether or not this container is off somewhere floating
	private Vector2 floatPosition = Vector2.Zero;

	public override void _Ready()
	{
		Pressed += delegate
		{
			GlobalEvents.SendOnPlaySFX(new InkAudioEventArgs { audioPath = onClickedSFX });
		};
		MouseEntered += delegate
		{
			if (!Disabled)
			{
				GlobalEvents.SendOnPlaySFX(new InkAudioEventArgs { audioPath = onHoverSFX });
			}
		};
		GlobalEvents.SendOnUIShow(new UIEventArgs { targetNode = this });
		base._Ready();
	}
	public override void _ExitTree()
	{
		base._ExitTree();
		if (_tween != null)
		{
			_tween.Kill();
		}
	}


	public virtual void Init(InkChoice choice)
	{
		//GD.Print("Initing ink button");
		Text = choice.Text;
		ParseTags(choice);
		if (choice.Tags.Count > 0)
		{
			GlobalEvents.SendOnTagsFound(new InkEventArgs { inkTags = choice.Tags, inkchoice = choice, inkChoiceButton = this });
		}
	}
	protected virtual void ParseTags(InkChoice choice)
	{
		foreach (string tag in choice.Tags)
		{
			if (tag.Contains(c_tagIcon))
			{
				SetIcon(tag);
			}
			if (tag.Contains(c_disabledIcon))
			{
				Disabled = true;
			}
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

	protected virtual void SetIcon(string tag)
	{
		GD.Print("Setting icon with tag " + tag);
		string iconId = tag.Replace(c_tagIcon, "");
		Texture2D texture = GlobalVariables.GetIconTexture2D(iconId);
		if (texture != null)
		{
			Icon = texture;
		}
		else
		{
			GD.Print("No icon with ID " + tag + " found - nulling");
			Icon = null;
		}
	}
	public virtual void SetTheme(string tag)
	{
		GD.Print("Setting theme on label to " + tag);
		string cleanedTag = tag.Replace(c_changeThemeTag, "");
		StyleBox newTheme = GlobalVariables.GetStyleBox(cleanedTag);
		if (newTheme != null)
		{
			AddThemeStyleboxOverride("Normal", newTheme);
			Set("theme_override_styles/normal", newTheme);
		}
	}
	public virtual void SetWorldSpacePosition(string tag)
	{ // #SET_POS:face OR #SET_POS:344^200
		string cleanedTag = tag.Replace(c_setWorldPosTag, "");
		string[] stringParts = cleanedTag.Split(c_delimiter);
		GD.Print("Received SET WORLD SPACE POSITION coords, with parts: " + cleanedTag);
		string[] coordinates = stringParts[0].Split(',');

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
		isFloating = true;
	}
	public virtual void SendPressedSignal()
	{
		EmitSignal(SignalName.Pressed);
	}
	public virtual void RemoveCustomAction()
	{
		if (OnPressedDelegate != null)
		{
			Pressed -= OnPressedDelegate;
			OnPressedDelegate = null;
		}
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
				active_ = value;
			}
		}
	}
	void SetActive(bool active)
	{
		//GD.Print("We are setting ink writer to active: " + active);
		Visible = active;
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
