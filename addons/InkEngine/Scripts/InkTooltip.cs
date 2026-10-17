using Godot;
using System;

public partial class InkTooltip : Node
{
	[Export] public RichTextLabel textLabel;

	public void SetTooltipText(string text)
	{
		textLabel.Text = text;
	}

}
