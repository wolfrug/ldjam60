using Godot;
using System;
using InkEngine;

public partial class InkBackground : TextureRect
{
	private const string c_setBackground = "SET_BACKGROUND:";
	// Called when the node enters the scene tree for the first time.
	public override void _EnterTree()
	{
		GlobalEvents.OnTagsFound += GlobalEvent_OnTagsFound;
		base._EnterTree();
	}
	public override void _ExitTree()
	{
		GlobalEvents.OnTagsFound -= GlobalEvent_OnTagsFound;
		base._ExitTree();
	}

	void GlobalEvent_OnTagsFound(InkEventArgs args)
	{
		//GD.Print("Looking for background tags");
		foreach (string tag in args.inkTags)
		{
			//GD.Print("looking for " + c_setBackground + " in tag " + tag);
			if (tag.Contains(c_setBackground))
			{
				SetBackground(tag);
			}
		}
	}

	Tween _tween;
	void FadeTexture(Texture2D tex)
	{
		if (_tween != null) { _tween.Kill(); }
		;
		_tween = CreateTween();
		//_tween.SetParallel(true); // lets each child's tween run concurrently, each with its own SetDelay below
		_tween.SetTrans(Tween.TransitionType.Sine);
		_tween.SetEase(Tween.EaseType.InOut);
		var mat = Material as ShaderMaterial;
		float current = mat.GetShaderParameter("progress").AsSingle();
		if (Texture != null)
		{
			_tween.TweenMethod(
				Callable.From<float>(v => mat.SetShaderParameter("progress", v)),
				current,
				0f,
				1.5f
			);
		}
		_tween.TweenCallback(Callable.From(() => SetTexture2D(tex)));
		if (tex != null)
		{
			_tween.TweenMethod(
			   Callable.From<float>(v => mat.SetShaderParameter("progress", v)),
			   0f,
			   1f,
			   1.5f
		   );
		}
		else
		{
			GD.Print("Faded out and removed texture from ink Background!");
		}
	}

	void SetBackground(string tag)
	{
		GD.Print("Setting background according to tag " + tag);
		string id = tag.Replace(c_setBackground, "");
		Texture2D tex = GlobalVariables.GetBackgroundTexture2D(id);
		if (tex != Texture)
		{

			FadeTexture(tex);

			/*
            {
                Tween tween = GetTree().CreateTween();
                tween.TweenProperty(GetNode(GetPath()), "modulate:a", 0f, 0.25f).SetTrans(Tween.TransitionType.Sine);
                tween.TweenCallback(Callable.From(() => SetTexture2D(tex)));
                tween.TweenProperty(GetNode(GetPath()), "modulate:a", 1f, 0.25f).SetTrans(Tween.TransitionType.Sine);
            }
            else
            {
                GD.Print("No Background found: nulling");
                Tween tween = GetTree().CreateTween();
                tween.TweenProperty(GetNode(GetPath()), "modulate:a", 0f, 0.25f).SetTrans(Tween.TransitionType.Sine);
            }*/
		}
	}
	public void SetTexture2D(Texture2D texture)
	{
		Texture = texture;
	}

}
