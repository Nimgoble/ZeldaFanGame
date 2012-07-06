// /* ------------------
//       ${Name} 
//       (c)3Radical 2012
//           by Mike Talbot 
//     ------------------- */
// 
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Serialization;

[ExecuteInEditMode]
[AddComponentMenu("Storage/Save Game Manager")]
[DontStore]
public class SaveGameManager : MonoBehaviour
{
	private static SaveGameManager instance;
	public static SaveGameManager Instance
	{
		get
		{
			if(instance == null)
			{
				instance = GameObject.FindObjectsOfType(typeof(GameObject))
					.Cast<GameObject>()
					.Where(g=>g.GetComponent<SaveGameManager>() != null)
					.Select(g=>g.GetComponent<SaveGameManager>())
					.FirstOrDefault();
				if(instance!=null)
					instance.GetAllReferences();
			}
			
			return instance;
			 
		}
		set
		{
			instance = value;
		}
	}
	
	public static bool hasRun;
	
	public static void Loaded()
	{
		_cached = null;
		Instance.Reference.Clear();
	}

	[Serializable]
	public class StoredEntry
	{
		public GameObject gameObject;
		public string Id = Guid.NewGuid().ToString();
	}

	public List<StoredEntry> Reference = new List<StoredEntry>();
	
	private static List<StoredEntry> _cached = new List<StoredEntry>();
	private static List<Action> _initActions = new List<Action>();


	public GameObject GetById(string id)
	{
		if(id==null)
			return null;
		return  Reference.Where(r=>r.Id==id && r.gameObject != null).Select(r=>r.gameObject).FirstOrDefault();
		
	}
	
	
	
	public void SetId(GameObject gameObject, string id)
	{
		
		var rr = Reference.FirstOrDefault(r=>r.gameObject == gameObject) ?? Reference.FirstOrDefault(r=>r.Id == id);
		if(rr != null)
		{
			rr.Id = id;
			rr.gameObject = gameObject;
		} else
		{
			rr =new StoredEntry { gameObject = gameObject, Id = id };
			Reference.Add(rr);
		}
	}

	public static string GetId(GameObject gameObject)
	{
		var Reference = Instance.Reference;
		var entry = Reference.FirstOrDefault(r=>r.gameObject == gameObject);
		if(entry != null)
			return entry.Id;
		if(Application.isLoadingLevel && !Application.isPlaying)
		{
			return null;
		}
		Reference.Add(entry = new StoredEntry { gameObject = gameObject});
		return entry.Id;
	}
	
	public static void Initialize(Action a)
	{
		if(Instance != null)
		{
			a();
		}
		else
		{
			_initActions.Add(a);
		}
	}
	
	private static Index<string, Index<string, List<object>>> _assetStore = new Index<string, Index<string, List<object>>>();
	
	public static Index<string, Index<string, List<object>>> assetStore
	{
		get
		{
			if(_assetStore.Count == 0)
			{
				Instance.GetAllReferences();
			}
			ProcessReferences();
			return _assetStore;
		}
	}
	
	static void ProcessReferences()
	{
		foreach(var l in _assetStore.Values.ToList())
		{
			foreach(var k in l.Keys.ToList())
			{
				l[k] = l[k].Where(c=>c!=null).ToList();
				if(l[k].Count==0)
				{
					l.Remove(k);
				}
			}
		}
	}
	
	public static void RefreshReferences()
	{
		if(Instance != null)
			Instance.GetAllReferences();
	}
	
	void GetAllReferences()
	{
		var assets = Resources.FindObjectsOfTypeAll(typeof(AnimationClip))
			.Concat(Resources.FindObjectsOfTypeAll(typeof(AudioClip)))
			.Concat(Resources.FindObjectsOfTypeAll(typeof(Mesh)))
			.Concat(Resources.FindObjectsOfTypeAll(typeof(Material)))
			.Concat(Resources.FindObjectsOfTypeAll(typeof(Texture)))
			.Where(g=>g!=null && !string.IsNullOrEmpty(g.name) )
			.Distinct()
			.ToList();
		_assetStore.Clear();
		foreach(var a in assets)
		{
			_assetStore[a.GetType().Name][a.name].Add(a);
		}
		
	}
	
	public class AssetReference
	{
		public string name;
		public string type;
		public int index;
	}
	
	public static AssetReference GetAssetId(object obj)
	{
		var item = obj as UnityEngine.Object;
		
		var index = assetStore[obj.GetType().Name][item.name].IndexOf(obj);
		if(index != -1)
		{
			return new AssetReference { name = item.name, type = obj.GetType().Name, index = index};
		}
		return new AssetReference { index = -1};
	}

	
	public static object GetAsset(AssetReference id) 
	{
		if(id.index == -1)
			return null;
		try
		{
			return assetStore[id.type][id.name][id.index];
		}
		catch
		{
			return null;
		}
	}
	

	
	void Awake()
	{
		GetAllReferences();
		Instance = this;
		if(Application.isPlaying && !hasRun)
		{
			_cached = Reference;
			hasRun = true;
		}
		else if(!Application.isPlaying ) {
			hasRun = false;
			if(_cached != null && _cached.Count > 0)
				Reference = _cached.Where(a=>a.gameObject != null).ToList();
		}
		if(_initActions.Count > 0)
		{
			foreach(var a in _initActions)
			{
				a();
			}
			_initActions.Clear();
		}
	}
}


