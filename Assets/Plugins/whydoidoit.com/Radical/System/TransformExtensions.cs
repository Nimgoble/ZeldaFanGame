using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public static class TransformExtensions 
{
	
	
	public static T FirstAncestorOfType<T>(this GameObject gameObject) where T : Component
	{
		var t = gameObject.transform.parent;
		T component = null;
		while (t != null && (component = t.GetComponent<T>()) == null)
		{
			t = t.parent;
		}
		return component;
	}
	
	public static T LastAncestorOfType<T>(this GameObject gameObject) where T : class
	{
		var t = gameObject.transform.parent;
		T component = null;
		while (t != null)
		{
			var c = t.gameObject.FindImplementor<T>();
			if (c != null)
			{
				component = c;
			}
			t = t.parent;
		}
		return component;
	}
	
	
}

