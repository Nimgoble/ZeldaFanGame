//http://www.whydoidoit.com
//Copyright (C) 2012 Mike Talbot
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using Serialization;
using System.Reflection;

/// <summary>
/// Declares a class that serializes a derivation of Component
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ComponentSerializerFor : Attribute
{
	public Type SerializesType;
	
	public ComponentSerializerFor(Type serializesType)
	{
		SerializesType = serializesType;
	}
}

[AttributeUsage(AttributeTargets.Class)]
public class SuspendLevelSerialization : Attribute
{
}

public interface IComponentSerializer
{
	/// <summary>
	/// Serialize the specified component to a byte array
	/// </summary>
	/// <param name='component'>
	/// Component to be serialized
	/// </param>
	byte[] Serialize(Component component);
	/// <summary>
	/// Deserialize the specified data into the instance.
	/// </summary>
	/// <param name='data'>
	/// The data that represents the component, produced by Serialize
	/// </param>
	/// <param name='instance'>
	/// The instance to target
	/// </param>
	void Deserialize(byte[] data, Component instance);
}



public class LevelSerializer 
{
	public delegate void StoreQuery(GameObject go, ref bool store);
	public static Dictionary<string, GameObject> AllPrefabs = new Dictionary<string, GameObject>();
	public static HashSet<string> IgnoreTypes = new HashSet<string>();
	public static Dictionary<Type, IComponentSerializer> CustomSerializers = new Dictionary<Type, IComponentSerializer>();
	
	/// <summary>
	/// The name of the player.
	/// </summary>
	public static string PlayerName = string.Empty;

	
	/// <summary>
	/// Occurs when the level was deserialized
	/// </summary>
	public static event Action Deserialized = delegate {};
	/// <summary>
	/// Occurs when the level was serialized.
	/// </summary>
	public static event Action GameSaved = delegate {};
	/// <summary>
	/// Occurs when suspending serialization.
	/// </summary>
	public static event Action SuspendingSerialization = delegate {};
	/// <summary>
	/// Occurs when resuming serialization.
	/// </summary>
	public static event Action ResumingSerialization = delegate {};
	
	public static bool SaveResumeInformation = true;
	
	public class SerializationSuspendedException : Exception
	{
		public SerializationSuspendedException() : base("Serialization was suspended: " + _suspensionCount + " times")
		{
		}
	}
	
	internal static void InvokeDeserialized()
	{
		_suspensionCount = 0;
		if (Deserialized != null)
		{
			Deserialized();
		}
		foreach (var go in GameObject.FindObjectsOfType(typeof(GameObject)).Cast<GameObject>())
		{
			go.SendMessage("OnDeserialized", null, SendMessageOptions.DontRequireReceiver);
		}
	}
	
	public enum SerializationModes
	{
		SerializeWhenFree,
		CacheSerialization
	}
	
	public static event StoreQuery Store;
	
	private static int _suspensionCount;
	private static SaveEntry _cachedState;
	
	/// <summary>
	/// The serialization caching mode
	/// </summary>
	public static SerializationModes SerializationMode = SerializationModes.CacheSerialization;
	
	public static bool CanResume
	{
		get
		{
			return !string.IsNullOrEmpty(PlayerPrefs.GetString(PlayerName + "__RESUME__"));
		}
	}
	
	/// <summary>
	/// Resume for a stored game state that wasn't directly saved
	/// </summary>
	public static void Resume()
	{
		var data = PlayerPrefs.GetString(PlayerName + "__RESUME__");
		if(!string.IsNullOrEmpty(data))
		{
			var se = UnitySerializer.Deserialize<SaveEntry>(Convert.FromBase64String(data));
			se.Load();
		}
	}
	
	/// <summary>
	/// Create a resumption checkpoint
	/// </summary>
	public static void Checkpoint()
	{
		SaveGame("Resume", false, PerformSaveCheckPoint);
	}
	
	private static void PerformSaveCheckPoint(string name, bool urgent)
	{
		
		var newGame = CreateSaveEntry(name, urgent);
		PlayerPrefs.SetString(PlayerName + "__RESUME__", Convert.ToBase64String(UnitySerializer.Serialize(newGame)));
	}	
	
	/// <summary>
	/// Suspends the serialization. Must resume as many times as you suspend
	/// </summary>
	public static void SuspendSerialization()
	{
		if(_suspensionCount==0)
		{
			SuspendingSerialization();
			if(SerializationMode == SerializationModes.CacheSerialization)
			{
				_cachedState = CreateSaveEntry("resume", true);
				if(SaveResumeInformation)
				{
					PlayerPrefs.SetString(PlayerName + "__RESUME__", Convert.ToBase64String(UnitySerializer.Serialize(_cachedState)));
				}
				
			}
		}
		_suspensionCount++;
	
	}
	
	/// <summary>
	/// Resumes the serialization. Must be balanced with calls to SuspendSerialization
	/// </summary>
	public static void ResumeSerialization()
	{
		_suspensionCount--;
		if(_suspensionCount == 0)
		{
			ResumingSerialization();
		}
	}
	
	/// <summary>
	/// Gets a value indicating whether this instance is suspended.
	/// </summary>
	/// <value>
	/// <c>true</c> if this instance is suspended; otherwise, <c>false</c>.
	/// </value>
	public static bool IsSuspended
	{
		get
		{
			return _suspensionCount > 0;
		}
	}
	
	/// <summary>
	/// Gets the serialization suspension count.
	/// </summary>
	/// <value>
	/// The suspension count.
	/// </value>
	public static int SuspensionCount
	{
		get
		{
			return _suspensionCount;
		}
	}
	
	/// <summary>
	/// Ignores the type of component when saving games.
	/// </summary>
	/// <param name='typename'>
	/// Typename of the component to ignore
	/// </param>
	public static void IgnoreType(string typename)
	{
		IgnoreTypes.Add(typename);
	}
	
	public static void UnIgnoreType(string typename)
	{
		IgnoreTypes.Remove(typename);
	}
	
	/// <summary>
	/// Ignores the type of component when saving games.
	/// </summary>
	/// <param name='tp'>
	/// The type of the component to ignore
	/// </param>
	public static void IgnoreType(Type tp)
	{
		IgnoreTypes.Add(tp.FullName);
	}
	
	/// <summary>
	/// Creates a saved game for the current position
	/// </summary>
	/// <returns>
	/// The new save entry.
	/// </returns>
	/// <param name='name'>
	/// A name for the save entry
	/// </param>
	public static SaveEntry CreateSaveEntry(string name, bool urgent)
	{
		return new SaveEntry() {
		
			Name = name,
			When = DateTime.Now,
			Level = Application.loadedLevelName,
			Data = SerializeLevel(urgent)
		};
	}
	
	/// <summary>
	/// A saved game entry
	/// </summary>
	public class SaveEntry
	{
		/// <summary>
		/// The name provided for the saved game.
		/// </summary>
		public string Name;
		/// <summary>
		/// The time that the game was saved
		/// </summary>
		public DateTime When;
		/// <summary>
		/// The name of the unity scene
		/// </summary>
		public string Level;
		/// <summary>
		/// The data about the saved game
		/// </summary>
		public string Data;
		/// <summary>
		/// Gets the caption.
		/// </summary>
		/// <value>
		/// The caption which is a combination of the name, the level and the time that the 
		/// game was saved
		/// </value>
		public string Caption
		{
			get
			{
				return string.Format ("{0} - {1} - {2:g}", Name, Level, When);
			}
		}
		
		/// <summary>
		/// Load this saved game
		/// </summary>
		public void Load()
		{
			LevelSerializer.LoadSavedLevel(Data);
		}
		
		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="LevelSerializer.SaveEntry"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents the current <see cref="LevelSerializer.SaveEntry"/>.
		/// </returns>
		public override string ToString ()
		{
			return Convert.ToBase64String(UnitySerializer.Serialize(this));
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="LevelSerializer.SaveEntry"/> class.
		/// </summary>
		/// <param name='contents'>
		/// The string representing the data of the saved game (use .ToString())
		/// </param>
		public SaveEntry(string contents)
		{
			UnitySerializer.DeserializeInto(Convert.FromBase64String(contents), this);
		}
		
		public SaveEntry()
		{
		}
		

		
	}
	
	/// <summary>
	/// Checks for the ability to serialize
	/// </summary>
	public class SerializationHelper : MonoBehaviour
	{
		public string gameName;
		public Action<string, bool> perform;
		
		void Update()
		{
			//Check to see if we are still suspended
			if(IsSuspended == false)
			{
				if(perform != null)
				{
					perform(gameName, false);
				}
				GameObject.DestroyImmediate(gameObject);
			}
		}
	}
	
	/// <summary>
	/// The max games that will be stored.
	/// </summary>
	public static int MaxGames = 20;
	/// <summary>
	/// The saved games.
	/// </summary>
	public static Lookup<string, List<SaveEntry>> SavedGames = new Index<string, List<SaveEntry>>();
	
	/// <summary>
	/// Saves the game.
	/// </summary>
	/// <param name='name'>
	/// The name to use for the game
	/// </param>
	/// <param name='urgent'>
	/// An urgent save will store the current state, even if suspended.  In this case it is likely that clean up will be necessary by handing Deserialized messages or responding to the LevelSerializer.Deserialized event
	/// </param>
	
	public static void SaveGame(string name)
	{
		SaveGame(name, false, null);
	}
	
	public static void SaveGame(string name, bool urgent, Action<string, bool> perform)
	{
		perform = perform ?? PerformSave;
		//See if we need to serialize later
		if(!urgent && ( IsSuspended && SerializationMode == SerializationModes.SerializeWhenFree))
		{
			//Are we already waiting for serialization to occur
			if(GameObject.Find("/SerializationHelper") != null)
			{
				return;
			}
			//Create a helper
			var go = new GameObject("SerializationHelper");
			var helper = go.AddComponent(typeof(SerializationHelper)) as SerializationHelper;
			helper.gameName = name;
			helper.perform = perform;
			return;
		}
		
		if(perform != null)
		{
			perform(name, urgent);
		}
		
	}
	
	private static void PerformSave(string name, bool urgent)
	{
		
		var newGame = CreateSaveEntry(name, urgent);
		SavedGames[PlayerName].Insert(0, newGame);
		
		
		while(SavedGames.Count > MaxGames)
		{
			SavedGames[PlayerName].RemoveAt(SavedGames.Count-1);
		}
		
		SaveDataToPlayerPrefs();

		PlayerPrefs.SetString(PlayerName + "__RESUME__", Convert.ToBase64String(UnitySerializer.Serialize(newGame)));
		
		GameSaved();
		
	}
	
	
	/// <summary>
	/// Saves the stored game data to player prefs.
	/// </summary>
	public static void SaveDataToPlayerPrefs ()
	{
		PlayerPrefs.SetString ("_Save_Game_Data_", Convert.ToBase64String (UnitySerializer.Serialize (SavedGames)));
		
	}
	
	class CompareGameObjects : IEqualityComparer<GameObject>
	{
		#region IEqualityComparer[GameObject] implementation
		public bool Equals (GameObject x, GameObject y)
		{
			return x.GetComponent<PrefabIdentifier> ().ClassId.CompareTo(y.GetComponent<PrefabIdentifier> ().ClassId)==0;
		}

		public int GetHashCode (GameObject obj)
		{
			return obj.GetComponent<PrefabIdentifier> ().ClassId.GetHashCode();
		}
		#endregion
		
		public static CompareGameObjects Instance = new CompareGameObjects();
	}
	
	public static bool IsDeserializing;
	
	//Load a list of all of the prefabs in the resources directory
	static LevelSerializer ()
	{
		IgnoreType (typeof(UniqueIdentifier));
		IgnoreType (typeof(Camera));
		
		UnitySerializer.AddPrivateType(typeof(AnimationClip));
		
		
		foreach(var asm in AppDomain.CurrentDomain.GetAssemblies())
		{
			UnitySerializer.ScanAllTypesForAttribute ((tp, attr) => {
				CustomSerializers [((ComponentSerializerFor)attr).SerializesType] = Activator.CreateInstance (tp) as IComponentSerializer;
			}, asm, typeof(ComponentSerializerFor));
		}
		
		AllPrefabs = 
			Resources.FindObjectsOfTypeAll (typeof(GameObject)).Cast<GameObject> ()
			.Where (go => go.GetComponent<PrefabIdentifier> () != null)
			.Distinct (CompareGameObjects.Instance)
			.ToDictionary(go=>go.GetComponent<PrefabIdentifier>().ClassId, go=> go);
		
		try
		{
			var stored = PlayerPrefs.GetString("_Save_Game_Data_");
			if(!string.IsNullOrEmpty(stored))
			{
				SavedGames = UnitySerializer.Deserialize<Lookup<string, List<SaveEntry>>>(Convert.FromBase64String(stored));
			}
			if(SavedGames == null)
			{
				SavedGames = new Index<string, List<SaveEntry>>();
			}
		}
		catch
		{
			SavedGames = new Index<string, List<SaveEntry>>();
		}
		
	
	
		
	}
	
	public static void RegisterAssembly()
	{
		UnitySerializer.ScanAllTypesForAttribute ((tp, attr) => {
			CustomSerializers [((ComponentSerializerFor)attr).SerializesType] = Activator.CreateInstance (tp) as IComponentSerializer;
		}, Assembly.GetCallingAssembly (), typeof(ComponentSerializerFor));
		
	}
	
	/// <summary>
	/// Adds the prefab path.
	/// </summary>
	/// <param name='path'>
	/// A resource path that contains prefabs to be created for the game
	/// </param>
	public static void AddPrefabPath(string path)
	{
		foreach(var pair in Resources.LoadAll(path, typeof(GameObject))
			.Cast<GameObject>()
			.Where(go=>go.GetComponent<UniqueIdentifier>() != null)
			.ToDictionary(go=>go.GetComponent<UniqueIdentifier>().ClassId, go=> go))
		{
			AllPrefabs.Add(pair.Key, pair.Value);
		}
	}
	
	public class StoredData
	{
		public string Type;
		public string ClassId;
		public string Name;
		public byte[] Data;
	}
	
	public class StoredItem
	{
		public override string ToString ()
		{
			return string.Format("{0}  child of {2} - ({1})", Name, ClassId, ParentName);
		}
		public string Name;
		public string ClassId;
		public string ParentName;
		public bool Active;
		public List<string> ChildIds = new List<string>();
	
		[DoNotSerialize]
		public GameObject GameObject;
	}
	
	public class LevelData
	{
		//The name of the level that was saved
		public string Name;
		//A set of all of the unique object names on the level
		public List<StoredItem> StoredObjectNames;
		//The data that was saved for the level
		public List<StoredData> StoredItems;
	}
	
	/// <summary>
	/// Serializes the level to a string
	/// </summary>
	/// <returns>
	/// The level data as a string
	/// </returns>
	/// <exception cref='SerizationSuspendedException'>
	/// Is thrown when the serization was suspended
	/// </exception>
	public static string SerializeLevel ()
	{
		return SerializeLevel(false);
	}
	
	/// <summary>
	/// Serializes the level to a string
	/// </summary>
	/// <returns>
	/// The level data as a string
	/// </returns>
	/// <exception cref='SerizationSuspendedException'>
	/// Is thrown when the serization was suspended
	/// </exception>
	public static string SerializeLevel (bool urgent = false)
	{
		LevelData ld;
		
		if (IsSuspended && !urgent)
		{
			if (SerializationMode == SerializationModes.CacheSerialization)
			{
				return _cachedState.Data;
			} else
			{
				throw new SerializationSuspendedException ();
			}
		}

		//using(new Radical.Logging())
		{
			//First we need to know the name of the last level loaded
			using (new UnitySerializer.SerializationScope())
			{
				ld = new LevelData ()
				{
				//The level to reload
					Name = Application.loadedLevelName
				};
				//All of the currently active uniquely identified objects
				ld.StoredObjectNames = UniqueIdentifier
					.AllIdentifiers
					.Select (i => i.gameObject)
		            .Where (go =>
						{
							if (Store == null)
							{
								return true;
							}
							var result = true;
							Store (go, ref result);
							return result;
						})
	                 .Select (n =>
						{
							var si = new StoredItem ()
							{
								Active = n.active,
				               Name = n.GetComponent<UniqueIdentifier>().Id,
				               ParentName = (n.transform.parent == null  || n.transform.parent.GetComponent<UniqueIdentifier>()==null)  ? null : (n.transform.parent.GetComponent<UniqueIdentifier>().Id),
				               ClassId = n.GetComponent<PrefabIdentifier> () != null ?
								   n.GetComponent<PrefabIdentifier> ().ClassId :
								   string.Empty
							};
							var pf = n.GetComponent<PrefabIdentifier>();
							if(pf != null)
							{
								si.ChildIds = n.GetComponentsInChildren<UniqueIdentifier>().Select(c=>c.Id).ToList();
							}
							return si;
						})
					.ToList ();

				//All of the data for the items to be stored

				ld.StoredItems = UniqueIdentifier
					.AllIdentifiers
					.Where (i => i != null)
					.Select (i => i.gameObject)
					.Distinct ()
			        .Where (go =>
						{
							if (Store == null)
							{
								return true;
							}
							var result = true;
							Store (go, ref result);

							return result;
						})
				    .Where (o => o.GetComponent<StoreInformation> () != null || o.GetComponent<PrefabIdentifier> () != null)
		            .SelectMany (o => o.GetComponents<Component> ())
			        .Where (c => c!=null && !IgnoreTypes.Any (tp => c.GetType ().FullName == tp || (Type.GetType (tp) != null && Type.GetType (tp).IsAssignableFrom (c.GetType ()))) && c.GetType () != typeof(Behaviour))
					.Select (c => new
						{
							Identifier = (StoreInformation)c.gameObject.GetComponent (typeof(StoreInformation)),
							Component = c
						})
				    .Where (cp => !cp.Component.GetType ().IsDefined (typeof(DoNotSerialize), false) &&
							(cp.Identifier.StoreAllComponents || cp.Identifier.Components.ContainsKey (cp.Component.GetType ().FullName)))
		            .OrderBy (cp => cp.Component.GetComponent<StoreInformation>().Id)
					.ThenBy (cp => cp.Component.GetType ().AssemblyQualifiedName)
				    .Select (cp =>
						{
							Radical.Log ("<{0} : {1} - {2}>", cp.Component.gameObject.GetFullName (), cp.Component.GetType ().Name, cp.Component.GetComponent<UniqueIdentifier>().Id);
							Radical.IndentLog ();
							var sd = new StoredData ()
						        {
								        Type = cp.Component.GetType ().AssemblyQualifiedName,
								       	ClassId = cp.Identifier.ClassId,
										Name = cp.Component.GetComponent<UniqueIdentifier>().Id
						        };

							if (CustomSerializers.ContainsKey (cp.Component.GetType ()))
							{
								sd.Data = CustomSerializers [cp.Component.GetType ()].Serialize (cp.Component);
							} else
							{
								sd.Data = UnitySerializer.SerializeForDeserializeInto (cp.Component);
							}
							Radical.OutdentLog ();
							Radical.Log ("</{0} : {1}>", cp.Component.gameObject.GetFullName (), cp.Component.GetType ().Name);

							return sd;

						})
					.ToList ();

			}
		}

		
		return Convert.ToBase64String (
			UnitySerializer.Serialize (ld)
			);
		
	}
	
	/// <summary>
	/// Loads the saved level.
	/// </summary>
	/// <param name='data'>
	/// The data describing the level to load
	/// </param>
	public static void LoadSavedLevel (string data)
	{
		IsDeserializing = true;
		var ld = UnitySerializer.Deserialize<LevelData> (Convert.FromBase64String (data));

		UniqueIdentifier.ClearAllNames ();
		SaveGameManager.Loaded();
		var go = new GameObject ();
		GameObject.DontDestroyOnLoad (go);
		var loader = go.AddComponent<LevelLoader> ();
		loader.Data = ld;
		
		Application.LoadLevel (ld.Name);

		

	}
	
}


[ComponentSerializerFor(typeof(Animation))]
public class SerializeAnimations : IComponentSerializer
{
	public class StoredState
	{
		public string name;
		public byte[] data;
	}
	
	#region IComponentSerializer implementation
	public byte[] Serialize (Component component)
	{
		return UnitySerializer.Serialize( ((Animation)component).Cast<AnimationState>().Select(a=> new StoredState() { data =  UnitySerializer.SerializeForDeserializeInto(a), name = a.name}).ToList() );
	}

	public void Deserialize (byte[] data, Component instance)
	{
		var animation = (Animation)instance;
		animation.Stop();
		var list = UnitySerializer.Deserialize<List<StoredState>>(data);
		foreach(var entry in list)
		{
			UnitySerializer.DeserializeInto(entry.data, animation[entry.name]);
		
		}
		
		
	}
	#endregion
}

public static class FieldSerializer
{
	public static void SerializeFields(Dictionary<string, object> storage, object obj, params string[] names)
	{
		var tp = obj.GetType();
		foreach(var name in names)
		{
			var fld = tp.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetField);
			if(fld != null)
			{
				storage[name] = fld.GetValue(obj);
			}
		}
	}
	
	public static void DeserializeFields(Dictionary<string, object> storage, object obj)
	{
		var tp = obj.GetType();
		foreach(var p in storage)
		{
			var fld = tp.GetField(p.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetField);
			fld.SetValue(obj, p.Value);
		}
	}
	
}