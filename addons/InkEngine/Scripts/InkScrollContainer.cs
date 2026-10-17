using Godot;
using System;
using System.Threading.Tasks;
using InkEngine;

public partial class InkScrollContainer : ScrollContainer
{
	private bool isScrolling = false;
	// Called when the node enters the scene tree for the first time.
	public override void _EnterTree()
	{
		GlobalEvents.OnContinue += GlobalEvent_OnContinue;
		base._EnterTree();
	}
	public override void _ExitTree()
	{
		GlobalEvents.OnContinue -= GlobalEvent_OnContinue;
		base._ExitTree();
	}

	private void GlobalEvent_OnContinue(InkEventArgs args)
	{
		//GD.Print("Scrolling");
		//GD.Print("VScrollBar Value: " + GetVScrollBar().Value + " VScrollbar MaxValue: " + GetVScrollBar().MaxValue);
		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(GetNode(GetPath()), "scroll_vertical", GetVScrollBar().MaxValue, 0.5f).SetTrans(Tween.TransitionType.Sine);
	}

}
