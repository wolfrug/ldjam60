using Godot;
using System;
using GodotInk;
using InkEngine;

public partial class InkUINode : Control
{
	[Signal] public delegate void ActivateUIEventHandler(string tag);
	[Signal] public delegate void DeactivateUIEventHandler(string tag);
	public bool IsActive { get; set; } = true;
	public virtual void Activate(string tag)
	{
		// Insert here all the code for initialization
		GlobalEvents.SendOnUIShow(new UIEventArgs { targetNode = this });
		EmitSignal(SignalName.ActivateUI, tag);
		IsActive = true;
	}
	public virtual void Deactivate(string tag)
	{
		// Insert here all the code for deinitialization
		GlobalEvents.SendOnUIHide(new UIEventArgs { targetNode = this });
		EmitSignal(SignalName.DeactivateUI, tag);
		IsActive = false;
	}
}
