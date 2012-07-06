using UnityEngine;
using System.Collections;
using System.Linq;
using System;

#if UNITY_EDITOR

using UnityEditor;

[CustomEditor(typeof(StoreInformation))]
public class EditStoreInformation : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		var t = target as StoreInformation;
	
		
		if (!t.StoreAllComponents)
		{
			GUILayout.Label("  Store which components");
			var cs = t.GetComponents<Component>().Where(c => !c.GetType().IsDefined(typeof(DontStoreAttribute), false) && (c.hideFlags & HideFlags.HideInInspector) == 0);
			foreach (var c in cs)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("      >       ");
				if (GUILayout.Toggle(t.Components.ContainsKey(c.GetType().FullName), ObjectNames.NicifyVariableName(c.GetType().Name)))
				{
					t.Components[c.GetType().FullName] = true;
					EditorUtility.SetDirty(target);
					
				}
				else
				{
					t.Components.Remove(c.GetType().FullName);
					EditorUtility.SetDirty(target);
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}
		}
		
	}
}

#endif



