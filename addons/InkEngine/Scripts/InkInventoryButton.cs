using Godot;
using GodotInk;
using System;
using System.Collections.Generic;
using InkEngine;

public partial class InkInventoryButton : Button
{
	[Export]
	public string onClickedSFX = "ButtonClick";
	[Export]
	public string onHoverSFX = "ButtonHover";

	public string itemName;
	public string itemDescription;

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
		base._Ready();
	}

	public void Init(string name, string description, string iconID, bool enabled, int stack = 1)
	{

		//GD.Print("Initing ink button");
		if (stack > 1)
		{
			Text = stack.ToString();
		}
		else
		{
			Text = "";
		}
		TooltipText = string.Format("[b]{0}[/b]\n\n{1}", name, description);
		SetIcon(iconID);
		Disabled = !enabled;
	}

	private void SetIcon(string iconId)
	{
		Texture2D texture = GlobalVariables.GetTexture2D(iconId);
		if (texture != null)
		{
			Icon = texture;
		}
	}

	public override GodotObject _MakeCustomTooltip(string text)
	{
		var scene = GD.Load<PackedScene>(GlobalVariables.c_inkTooltipScene);
		InkTooltip tooltip = scene.Instantiate<InkTooltip>();
		tooltip.textLabel.Text = text.Trim();
		return tooltip;
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
		if (WeakRef(this) != null)
		{
			Visible = active;
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
