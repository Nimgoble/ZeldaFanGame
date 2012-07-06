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
using System.Reflection;

[Deferred]
[AddComponentMenu("System/Resumable Coroutine Support")]
public class RadicalRoutineHelper : MonoBehaviour, IDeserialized
{
	static RadicalRoutineHelper _current;
	public static RadicalRoutineHelper Current
	{
		get
		{
			if(_current == null || _current.GetComponent<StoreInformation>()==null)
			{
				throw new Exception("Something must have a RadicalRoutineHelper on it. It should be stored");
			}
			return _current;
		}
	}
	
	void Awake()
	{
		if (_current == null)
		{
			_current = this;
		}
	}
	
	public List<RadicalRoutine> Running = new List<RadicalRoutine>();
	
	public void Run(RadicalRoutine routine)
	{
		Running.Add(routine);
		StartCoroutine(routine.enumerator);
	}
	public void Finished(RadicalRoutine routine)
	{
		Running.Remove(routine);
		if(!string.IsNullOrEmpty(routine.Method) && routine.Target != null)
		{
			try
			{
				var mi = routine.Target.GetType().GetMethod(routine.Method, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static);
				if(mi != null)
				{
					mi.Invoke(routine.Target, new object [] {});
				}
			}
			catch
			{
			}
		}
	}
	
	#region IDeserialized implementation
	void IDeserialized.Deserialized ()
	{
		Loom.QueueOnMainThread(()=>{
			foreach(var routine in Running)
			{
				StartCoroutine(routine.enumerator);
			}
		});
	}

	
	#endregion
}
