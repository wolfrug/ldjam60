using System.Collections;
using System.Collections.Generic;
using Godot;
using GodotInk;
using Ink.Runtime;
using InkEngine;

[Tool]
public partial class InkArrayFunctions : Node
{

	public void Init(InkStory story)
	{
		story.BindExternalFunction("EXT_AddToList", (string arg0, string arg1) => { return AddString(arg0, arg1); }, false);
		story.BindExternalFunction("EXT_RemoveFromList", (string arg0, string arg1) => { return RemoveString(arg0, arg1); }, false);
		story.BindExternalFunction("EXT_AddToDictionary", (string arg0, string arg1, string arg2) => { return AddStringDictionary(arg0, arg1, arg2); }, false);
		story.BindExternalFunction("EXT_RemoveFromDictionary", (string arg0, string arg1) => { return RemoveStringDictionary(arg0, arg1); }, false);
		story.BindExternalFunction("EXT_HasValue", (string arg0, string arg1) => { return ContainsValue(arg0, arg1); }, false);
		story.BindExternalFunction("EXT_GetValue", (string arg0, string arg1) => { return GetValue(arg0, arg1); }, false);
		story.BindExternalFunction("EXT_GetValueInt", (string arg0, string arg1) => { return GetValueInt(arg0, arg1); }, false);
		story.BindExternalFunction("EXT_GetValueFloat", (string arg0, string arg1) => { return GetValueFloat(arg0, arg1); }, false);
		story.BindExternalFunction("EXT_Count", (string arg0) => { return Count(arg0); }, false);
	}

	/*public void EventListener(InkDialogueLine line, InkTextVariable variable)
	{
		switch (variable.variableName)
		{
			case "ADD_TO_ARRAY":
				{
					string currentArray = m_data.InkStory.variablesState[variable.VariableArguments[1]] as string;
					string newArray = AddString(variable.VariableArguments[0], currentArray);
					m_data.InkStory.variablesState[variable.VariableArguments[1]] = newArray;
					break;
				}
			case "REMOVE_FROM_ARRAY":
				{
					string currentArray = m_data.InkStory.variablesState[variable.VariableArguments[1]] as string;
					string newArray = RemoveString(variable.VariableArguments[0], currentArray);
					m_data.InkStory.variablesState[variable.VariableArguments[1]] = newArray;
					break;
				}
			case "ADD_TO_DICTIONARY":
				{
					string currentArray = m_data.InkStory.variablesState[variable.VariableArguments[2]] as string;
					string newArray = AddStringDictionary(variable.VariableArguments[0], variable.VariableArguments[1], currentArray);
					m_data.InkStory.variablesState[variable.VariableArguments[1]] = newArray;
					break;
				}
			case "REMOVE_FROM_DICTIONARY":
				{
					string currentArray = m_data.InkStory.variablesState[variable.VariableArguments[1]] as string;
					string newArray = RemoveStringDictionary(variable.VariableArguments[0], currentArray);
					m_data.InkStory.variablesState[variable.VariableArguments[1]] = newArray;
					break;
				}
			case "HAS_VALUE":
				{
					int returnBool = (int)m_data.InkStory.variablesState[variable.VariableArguments[2]];
					string currentArray = m_data.InkStory.variablesState[variable.VariableArguments[1]] as string;
					returnBool = ContainsValue(variable.VariableArguments[0], currentArray) ? 1 : 0;
					m_data.InkStory.variablesState[variable.VariableArguments[2]] = returnBool;
					break;
				}
		}
	}
	*/

	public string AddString(string stringToAdd, string inkarray)
	{
		return InkArrays.SerializeStrings<string>(new List<string> { stringToAdd }, inkarray); ;
	}
	public string RemoveString(string stringToRemove, string inkarray)
	{

		List<string> givenArray = InkArrays.DeSerializeString(inkarray);
		if (givenArray.Contains(stringToRemove))
		{
			givenArray.Remove(stringToRemove);
		}
		return InkArrays.SerializeStrings<string>(givenArray);
	}
	public string AddStringDictionary(string stringKey, string stringVal, string inkarray)
	{
		if (!InkArrays.HasValue(stringKey, inkarray))
		{
			return InkArrays.SerializeProtoDictionary<string>(new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(stringKey, stringVal) }, inkarray);
		}
		else
		{
			List<KeyValuePair<string, string>> currentDict = InkArrays.DeSerializeProtoDictionary(inkarray);
			foreach (KeyValuePair<string, string> kvp in new List<KeyValuePair<string, string>>(currentDict))
			{
				if (kvp.Key == stringKey)
				{
					currentDict.Remove(kvp);
					currentDict.Add(new KeyValuePair<string, string>(stringKey, stringVal));
					break;
				}

			}
			return InkArrays.SerializeProtoDictionary<string>(currentDict);
		}
	}
	public string RemoveStringDictionary(string stringKey, string inkarray)
	{

		if (!InkArrays.HasValue(stringKey, inkarray))
		{
			return inkarray;
		}
		List<KeyValuePair<string, string>> currentDict = InkArrays.DeSerializeProtoDictionary(inkarray);
		foreach (KeyValuePair<string, string> kvp in new List<KeyValuePair<string, string>>(currentDict))
		{
			if (kvp.Key == stringKey)
			{
				currentDict.Remove(kvp);
				break;
			}

		}
		return InkArrays.SerializeProtoDictionary<string>(currentDict);
	}
	public bool ContainsValue(string key, string inkarray)
	{
		return InkArrays.HasValue(key, inkarray);
	}
	public string GetValue(string key, string inkarray)
	{
		GD.Print("Received call to return value of key " + key + " from array " + inkarray);
		if (InkArrays.IsProtoDictionary(inkarray))
		{
			return InkArrays.GetStringByKey(key, inkarray);
		}
		else
		{
			int index = -1;
			int.TryParse(key, out index);
			List<string> list = InkArrays.DeSerializeString(inkarray);
			GD.Print("Got index: " + index + " and array count: " + list.Count);
			if (index >= 0 && index < list.Count && list.Count > 0)
			{
				GD.Print("Returning " + list[index]);
				return list[index];
			}
		}
		return "";
	}
	public int GetValueInt(string key, string inkarray)
	{
		string value = GetValue(key, inkarray);
		if (int.TryParse(value, out int result))
		{
			return result;
		}
		else
		{
			return 0;
		}
	}
	public float GetValueFloat(string key, string inkarray)
	{
		string value = GetValue(key, inkarray);
		if (float.TryParse(value, out float result))
		{
			return result;
		}
		else
		{
			return 0;
		}
	}
	public int Count(string inkarray)
	{
		return InkArrays.Count(inkarray);
	}
}
