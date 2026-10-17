using Godot;
using Godot.Collections;
using System;
using System.Linq;


namespace InkEngine
{
	public static class GlobalVariables
	{

		public static string c_inkButtonScene = "res://addons/InkEngine/Scenes/InkButton.tscn";
		public static string c_inkLabelScene = "res://addons/InkEngine/Scenes/InkLabel.tscn";
		public static string c_inkInventoryButtonScene = "res://addons/InkEngine/Scenes/InkInventoryButton.tscn";
		public static string c_inkTooltipScene = "res://addons/InkEngine/Scenes/InkTooltip.tscn";

		private static string c_inkResourcesPath = "res://addons/InkEngine/Resources/";
		private static string c_backgroundDataPath = "res://addons/InkEngine/Resources/InkBackgroundData/";
		private static string c_audioDataPath = "res://addons/InkEngine/Resources/InkAudioData/";

		//private static string c_characterDataPath = "res://Scripts/CharacterStateStuff/CharacterData/";

		public static Texture2D GetPortraitTexture2D(string id)
		{
			id = id.Trim();
			string start = "res://addons/InkEngine/Graphics/Portraits/";
			string finalString = start + id;
			if (ResourceLoader.Exists(finalString))
			{
				Texture2D loadedTexture = ResourceLoader.Load(finalString) as Texture2D;
				return loadedTexture;
			}
			else
			{
				GD.Print("No portrait with ID " + id + " found!");
				return null;
			}
		}
		public static Texture2D GetBackgroundTexture2D(string id)
		{
			id = id.Trim();
			string start = "res://addons/InkEngine/Graphics/Backgrounds/";

			string finalString = start + id;
			if (ResourceLoader.Exists(finalString))
			{
				Texture2D loadedTexture = ResourceLoader.Load(finalString) as Texture2D;
				return loadedTexture;
			}
			else
			{
				GD.Print("No background with ID " + id + " found!");
				return null;
			}
		}
		public static Texture2D GetIconTexture2D(string id)
		{
			id = id.Trim();
			string start = "res://addons/InkEngine/Graphics/Icons/";
			string finalString = start + id;
			if (ResourceLoader.Exists(finalString))
			{
				Texture2D loadedTexture = ResourceLoader.Load(finalString) as Texture2D;
				return loadedTexture;
			}
			else
			{
				GD.Print("No icon with ID " + id + " found!");
				return null;
			}
		}
		public static Texture2D GetTexture2D(string id)
		{ // Call: GlobalVariables.GetTexture2D("res://...image path") OR use ID (will look through all possible paths)
			id = id.Trim();
			Texture2D attempt = GetIconTexture2D(id);
			if (attempt != null)
			{
				return attempt;
			}
			attempt = GetPortraitTexture2D(id);
			if (attempt != null)
			{
				return attempt;
			}
			attempt = GetBackgroundTexture2D(id);
			if (attempt != null)
			{
				return attempt;
			}
			if (ResourceLoader.Exists(id))
			{
				return ResourceLoader.Load(id) as Texture2D;
			}
			else
			{
				GD.Print("Could not find texture2d at path " + id);
				return null;
			}
		}
		public static StyleBox GetStyleBox(string id)
		{
			id = id.Trim();
			string start = "res://addons/InkEngine/UI/";
			string finalString = start + id;
			if (ResourceLoader.Exists(finalString))
			{
				StyleBox loadedTheme = ResourceLoader.Load(finalString) as StyleBox;
				return loadedTheme;
			}
			else
			{
				GD.Print("No stylebox with ID " + id + " found!");
				return null;
			}
		}
		public static string RemoveBBCode(string input)
		{
			var regex = new RegEx();
			regex.Compile("\\[.*?\\]");
			string text_without_tags = regex.Sub(input, "", true);
			return text_without_tags;
		}

		public static string GetInkLabelPath(string ID)
		{
			if (InkLabelDatas.TryGetValue(ID, out InkLabelData data))
			{
				return data._nodePath;
			}
			else
			{
				return c_inkLabelScene; // Backup
			}
		}
		public static string GetInkButtonPath(string ID)
		{
			if (InkButtonDatas.TryGetValue(ID, out InkButtonData data))
			{
				return data._nodePath;
			}
			else
			{
				return c_inkButtonScene; // Backup
			}
		}

		// DATAS
		private static Godot.Collections.Dictionary<string, InkLabelData> _labelDatas = new Godot.Collections.Dictionary<string, InkLabelData> { };
		public static Godot.Collections.Dictionary<string, InkLabelData> InkLabelDatas
		{
			get
			{
				if (_labelDatas.Count == 0)
				{
					_labelDatas = InkResourceLoader<InkLabelData>();
				}
				return _labelDatas;
			}
		}
		private static Godot.Collections.Dictionary<string, InkButtonData> _buttonDatas = new Godot.Collections.Dictionary<string, InkButtonData> { };
		public static Godot.Collections.Dictionary<string, InkButtonData> InkButtonDatas
		{
			get
			{
				if (_buttonDatas.Count == 0)
				{
					_buttonDatas = InkResourceLoader<InkButtonData>();
				}
				return _buttonDatas;
			}
		}

		private static Godot.Collections.Dictionary<string, InkAudioData> _audioDatas = new Godot.Collections.Dictionary<string, InkAudioData> { };
		public static Godot.Collections.Dictionary<string, InkAudioData> InkAudioData
		{
			get
			{
				if (_audioDatas.Count == 0)
				{
					_audioDatas = InkResourceLoader<InkAudioData>(c_audioDataPath);
				}
				return _audioDatas;
			}
		}

		public static Dictionary<string, T> InkResourceLoader<[MustBeVariant] T>(string resourcePath = "") where T : class
		{
			string targetPath = resourcePath == "" ? c_inkResourcesPath : resourcePath;
			GD.Print("GlobalVariables: Attempting to load all Datas of type " + typeof(T) + " in path " + targetPath);
			string[] allContents = ResourceLoader.ListDirectory(targetPath);
			Dictionary<string, T> returnDict = new Dictionary<string, T> { };
			foreach (string path in allContents)
			{
				if (ResourceLoader.Exists(targetPath + path))
				{
					var resource = ResourceLoader.Load(targetPath + path);
					if (resource is T)
					{
						if (returnDict.ContainsKey((resource as InkResource)._ID))
						{
							GD.PrintErr("GlobalVariables: Duplicate key found: " + (resource as InkResource)._ID + " for resource at path " + targetPath + path + " - this is not supported!");
						}
						returnDict.Add((resource as InkResource)._ID, resource as T);
						GD.Print("GlobalVariables: Loaded " + (resource as InkResource)._ID + " into datas");
					}
				}
			}
			return returnDict;
		}


		private static Godot.Collections.Dictionary<string, InkBackgroundData> _allBackgroundDatas = new Godot.Collections.Dictionary<string, InkBackgroundData> { };
		public static Godot.Collections.Dictionary<string, InkBackgroundData> InkBackgroundDatas
		{
			get
			{
				if (_allBackgroundDatas.Count == 0)
				{
					LoadAllBackgroundDatas();
				}
				return _allBackgroundDatas;
			}
		}

		public static void LoadAllBackgroundDatas()
		{
			_allBackgroundDatas.Clear();
			_allBackgroundDatas = InkResourceLoader<InkBackgroundData>(c_backgroundDataPath);
			/*GD.Print("GlobalVariables: Attempting to load all Background Datas in path " + c_backgroundDataPath);
			_allBackgroundDatas.Clear();
			string[] allContents = ResourceLoader.ListDirectory(c_backgroundDataPath);
			foreach (string path in allContents)
			{
				if (ResourceLoader.Exists(c_backgroundDataPath + path))
				{
					var resource = ResourceLoader.Load(c_backgroundDataPath + path);
					if (resource is InkBackgroundData)
					{
						if (_allBackgroundDatas.ContainsKey((resource as InkBackgroundData).ID))
						{
							GD.PrintErr("GlobalVariables: Duplicate key found: " + (resource as InkBackgroundData).ID + " for resource " + c_backgroundDataPath + path + " - this is not supported!");
						}
						_allBackgroundDatas.Add((resource as InkBackgroundData).ID, resource as InkBackgroundData);
						GD.Print("GlobalVariables: Loaded " + c_backgroundDataPath + path + " using ID " + (resource as InkBackgroundData).ID + " into all background datas");
					}
				}
			}*/
		}

		/*
				private static Godot.Collections.Dictionary<string, CharacterData> _allCharacterdatas = new Godot.Collections.Dictionary<string, CharacterData> { };

				public static Godot.Collections.Dictionary<string, CharacterData> AllCharacterdatas
				{
					get
					{
						if (_allCharacterdatas.Count == 0)
						{
							LoadAllCharacterDatas();
						}
						return _allCharacterdatas;
					}
				}

				private static void LoadAllCharacterDatas()
				{
					string[] allContents = ResourceLoader.ListDirectory(c_characterDataPath);
					foreach (string path in allContents)
					{
						if (ResourceLoader.Exists(c_characterDataPath + path))
						{
							var resource = ResourceLoader.Load(c_characterDataPath + path);
							if (resource is CharacterData)
							{
								_allCharacterdatas.Add((resource as CharacterData).Id, resource as CharacterData);
								GD.Print("Loaded " + (resource as CharacterData).Id + " into all character types");
							}
						}
					}
				}
				*/
	}
}
