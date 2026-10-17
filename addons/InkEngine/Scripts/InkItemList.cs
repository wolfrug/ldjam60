using Godot;
using System;
using System.Collections.Generic;
using GodotInk;
using InkEngine;

public partial class InkItemList : ItemList
{
	[Export]
	private string inventoryStackVariable;
	public Dictionary<string, int> InventoryDictionary = new Dictionary<string, int> { };

	[Export] public string c_curItemVar = "curItem";
	[Export] public string c_itemsInventory = "items";
	[Export] public string c_cancelItemName = "None";
	[Export] public string c_nameFormat = "{0} ({1})";
	[Export] public string[] c_disabledStates = { "" };
	private const string c_getdisplaynamefunction = "GetDisplayName";
	private const string c_getdescriptionFunction = "GetDescription";
	private const string c_updatetaskfunction = "UpdateTask";
	public string currentSelectedItem = "";

	public override void _EnterTree()
	{
		base._EnterTree();
		GlobalEvents.OnInkWriterReady += GlobalEvents_OnInkWriterReady;
	}
	public override void _ExitTree()
	{
		base._ExitTree();
		GlobalEvents.OnInkWriterReady -= GlobalEvents_OnInkWriterReady;
	}
	public override void _Ready()
	{

		ItemClicked += SetSelectedItem;
	}

	private void GlobalEvents_OnInkWriterReady(InkEventArgs eventArgs)
	{
		Inkwriter.instance.Story.ObserveVariable(inventoryStackVariable, Callable.From((string varname, Variant newval) => VariableChanged((string)newval)));
	}

	public void UpdateTask(string taskId)
	{
		Inkwriter.instance.Story.runtimeStory.EvaluateFunction(c_updatetaskfunction, out string textOutput, new object[] { taskId });
	}

	public void VariableChanged(string newval)
	{
		GD.Print("New value is: " + newval);
		List<KeyValuePair<string, string>> dict = InkArrays.DeSerializeProtoDictionary(newval);

		InventoryDictionary.Clear();
		Clear();
		bool hasSelectedItem = false;
		if (dict.Count == 0)
		{
			SetSelectedItem(-1);
		}
		foreach (KeyValuePair<string, string> kvp in dict)
		{
			if (InventoryDictionary.TryGetValue(kvp.Key, out int index))
			{
				EditInventoryItem(kvp.Key, index, FormatName(kvp.Key, kvp.Value), null, CheckDisabled(kvp.Value));
			}
			else
			{
				NewInventoryItem(kvp.Key, FormatName(kvp.Key, kvp.Value), null, CheckDisabled(kvp.Value));
			}
			if (kvp.Key == currentSelectedItem)
			{
				Select(InventoryDictionary[kvp.Key]);
				hasSelectedItem = true;
			}
		}
		NewInventoryItem(c_cancelItemName, GetDisplayName(c_cancelItemName));
		if (!hasSelectedItem)
		{
			SetSelectedItem(-1);
		}

	}

	public virtual string FormatName(string id, string value)
	{
		string displayName = GetDisplayName(id);
		string valueNoBBCode = GlobalVariables.RemoveBBCode(value);
		return string.Format(c_nameFormat, displayName, valueNoBBCode);

	}
	public virtual bool CheckDisabled(string item)
	{
		if (c_disabledStates.Length == 0) { return false; }
		;

		foreach (string v in c_disabledStates)
		{
			if (item == v)
			{
				return true;
			}
		}
		return false;
	}

	public virtual string GetDisplayName(string id)
	{
		if (id == "")
		{
			id = c_cancelItemName;
		}
		Ink.Runtime.InkList list = new Ink.Runtime.InkList(c_itemsInventory, Inkwriter.instance.Story.runtimeStory);
		//list.SetInitialOriginName("items");
		list.AddItem(id);
		var returnValue = Inkwriter.instance.Story.runtimeStory.EvaluateFunction(c_getdisplaynamefunction, out string textOutput, new object[] { list });
		string noBBCode = GlobalVariables.RemoveBBCode((string)returnValue);
		return noBBCode;
	}
	public virtual string GetDescription(string id)
	{
		Ink.Runtime.InkList list = new Ink.Runtime.InkList(c_itemsInventory, Inkwriter.instance.Story.runtimeStory);
		//list.SetInitialOriginName("items");
		list.AddItem(id);
		var returnValue = Inkwriter.instance.Story.runtimeStory.EvaluateFunction(c_getdescriptionFunction, out string textOutput, new object[] { list });
		GD.Print("InkItemList: received GetDescription of item with id " + id + ": " + (string)returnValue);
		return (string)returnValue;
	}

	public override GodotObject _MakeCustomTooltip(string text)
	{
		var scene = GD.Load<PackedScene>(GlobalVariables.c_inkTooltipScene);
		InkTooltip tooltip = scene.Instantiate<InkTooltip>();
		tooltip.SetTooltipText(text);
		//tooltip.textLabel.Text = ""; // null previous text
		//tooltip.textLabel.AppendText(text);
		return tooltip;
	}

	public virtual void NewInventoryItem(string id, string itemName, Texture2D icon = null, bool disabled = false)
	{
		if (!InventoryDictionary.ContainsKey(id))
		{
			int index = AddItem(itemName, icon, true);
			SetItemDisabled(index, disabled);
			SetItemTooltip(index, GetDescription(id));
			InventoryDictionary.Add(id, index);
		}
	}
	public virtual void EditInventoryItem(string id, int index, string itemName, Texture2D icon = null, bool disabled = false)
	{
		SetItemText(index, itemName);
		SetItemIcon(index, icon);
		SetItemSelectable(index, true);
		SetItemDisabled(index, disabled);
	}
	public virtual void SetSelectedItem(long index, Vector2 atPosition = default, long mouseButtonIndex = default)
	{
		string id = GetIdByIndex((int)index);
		Inkwriter.instance.Story.StoreVariable(c_curItemVar, id);
		currentSelectedItem = id;
		GD.Print("Selected item is: " + id);
	}
	public string GetIdByIndex(int index)
	{
		if (InventoryDictionary.ContainsValue(index))
		{
			foreach (string keyVar in InventoryDictionary.Keys)
			{
				if (InventoryDictionary[keyVar] == index)
				{
					return keyVar;
				}
			}
		}
		return c_cancelItemName;
	}
}
