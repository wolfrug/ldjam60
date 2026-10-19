using Godot;
using System;
using System.Collections.Generic;

public partial class ConnectConcepts : Control
{
	[Export] public ItemList nameList;
	[Export] public ItemList iconList;
	[Export] public CheckButton toggleButton;
	// Called when the node enters the scene tree for the first time.

	private int selectedName = -1;
	private int selectedIcon = -1;

	[Export] public Godot.Collections.Dictionary<int, int> connections = new();
	public override void _Ready()
	{
		toggleButton.Toggled += Connect;

		nameList.ItemClicked += SelectName;
		iconList.ItemClicked += SelectIcon;

		toggleButton.Disabled = true;
	}

	private void SelectIcon(long index, Vector2 atPosition, long mouseButtonIndex)
	{
		if (nameList.GetSelectedItems().Length > 0)
		{
			toggleButton.Disabled = false;
		}
		if (connections.TryGetValue(selectedName, out int otherindex))
		{
			if (otherindex == (int)index)
			{
				toggleButton.SetPressedNoSignal(true);
			}
			else
			{
				toggleButton.SetPressedNoSignal(false);
			}
		}
		else
		{
			toggleButton.SetPressedNoSignal(false);
		}
		selectedIcon = (int)index;
	}


	private void SelectName(long index, Vector2 atPosition, long mouseButtonIndex)
	{
		if (iconList.GetSelectedItems().Length > 0)
		{
			toggleButton.Disabled = false;
		}
		if (connections.TryGetValue((int)index, out int otherindex))
		{
			if (otherindex == selectedIcon && otherindex != -1)
			{
				toggleButton.SetPressedNoSignal(true);
			}
			else
			{
				toggleButton.SetPressedNoSignal(false);
			}
		}
		else
		{
			toggleButton.SetPressedNoSignal(false);
		}
		selectedName = (int)index;
	}
	public void Connect(bool toggledOn)
	{
		if (!connections.ContainsKey(selectedName))
		{
			connections.Add(selectedName, -1);
		}
		foreach (KeyValuePair<int, int> kvp in connections)
		{
			if (kvp.Value == selectedIcon)
			{
				iconList.SetItemText(connections[kvp.Key], "???");
				connections[kvp.Key] = -1;
			}
		}
		iconList.SetItemText(connections[selectedName], "???");
		if (toggledOn)
		{
			connections[selectedName] = selectedIcon;
			iconList.SetItemText(selectedIcon, nameList.GetItemText(selectedName));
		}
		else
		{
			iconList.SetItemText(selectedIcon, "???");
			connections[selectedName] = -1;
		}
	}


}
