using UnityEngine;
using System.Collections;
using System.Linq;

[DontStore]
[AddComponentMenu("Storage/Prefab Identifier")]
[ExecuteInEditMode]
public class PrefabIdentifier : StoreInformation
{
	protected override void Awake ()
	{
		base.Awake();
		foreach (var c in GetComponents<UniqueIdentifier>().Where(t=>t.GetType() == typeof(UniqueIdentifier) || 
			(t.GetType() == typeof(PrefabIdentifier) && t != this) ||
			t.GetType() == typeof(StoreInformation)
			))
			DestroyImmediate (c);
	}	
}

