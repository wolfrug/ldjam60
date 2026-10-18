using Godot;
using System;

public partial class InkUINode_Simple : InkUINode
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
		ActivateUI += delegate { Visible = true; };
		DeactivateUI += delegate { Visible = false; };
	}

}
