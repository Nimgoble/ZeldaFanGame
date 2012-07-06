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
using Serialization;
using System.Collections.Generic;
using System.Reflection;

public class CoroutineReturn
{
	public virtual bool finished { get; set; }

	public virtual bool cancel { get; set; }
}

[SerializeAll]
public class WaitForAnimation : CoroutineReturn
{
	private GameObject _go;
	private string _name;
	private float _time;
	private float _weight;
	[DoNotSerialize]
	private int startFrame;

	public string name
	{
		get
		{
			return _name;
		}
	}
	
	public WaitForAnimation()
	{
	}
	
	public WaitForAnimation(GameObject go, string name, float time=1f, float weight = -1)
	{
		startFrame = Time.frameCount;
		_go = go;
		_name = name;
		_time = Mathf.Clamp01(time);
		_weight = weight;
	}
	
	[DoNotSerialize]
	public override bool finished
	{
		get
		{
			if (LevelSerializer.IsDeserializing)
			{
				return false;
			}
			if (Time.frameCount <= startFrame + 4)
			{
				return false;
			}
			
			var anim = _go.animation[_name];
		
			bool ret = true;
				
			if (anim.enabled)
			{
				
				if (_weight == -1)
				{
					ret = anim.normalizedTime >= _time;
				
				}
				else
				{
					if (_weight < 0.5)
					{
						ret = anim.weight <= Mathf.Clamp01(_weight + 0.001f);
					}
					ret = anim.weight >= Mathf.Clamp01(_weight - 0.001f);
				}
			
			}
			if(!_go.animation.IsPlaying(_name))
			{
				ret = true;
			}
			if(ret)
			{
				if(anim.weight == 0 || anim.normalizedTime == 1)
				{
					anim.enabled = false;
				}
			}
			return ret;
				
		}
		set
		{
			base.finished = value;
		}
	}
	
}

public static class TaskHelpers
{
	public static WaitForAnimation WaitForAnimation(this GameObject go, string name, float time = 1f)
	{
		return new WaitForAnimation(go, name, time, -1);
	}

	public static WaitForAnimation WaitForAnimationWeight(this GameObject go, string name, float weight=0f)
	{
		return new WaitForAnimation(go, name, 0, weight);
	}
}

public interface IYieldInstruction
{
	YieldInstruction Instruction { get; }
}

public class RadicalWaitForSeconds : IYieldInstruction
{

	private float _time;
	private float _seconds;
	
	public RadicalWaitForSeconds()
	{
	}
	
	public float TimeRemaining
	{
		get
		{
			return Mathf.Clamp((_time + _seconds) - Time.time, 0, 10000000);
		}
		set
		{
			_time = Time.time;
			_seconds = value;
		}
	}
	
	public RadicalWaitForSeconds(float seconds)
	{
		_time = Time.time;
		_seconds = seconds;
	}
	
	#region IYieldInstruction implementation
	public YieldInstruction Instruction
	{
		get
		{
			return new WaitForSeconds(TimeRemaining);
		}
	}
	#endregion
}

public interface INotifyStartStop
{
	void Stop();

	void Start();
}

public class RadicalRoutine : IDeserialized
{
	
	private bool cancel;
	private IEnumerator extended;
	public IEnumerator enumerator;
	public object Notify;
	public string Method;
	public object Target;

	public event Action Cancelled = delegate {};
	public event Action Finished = delegate {};
	
	public void Cancel()
	{
		cancel = true;
		if (extended is INotifyStartStop)
		{
			(extended as INotifyStartStop).Stop();
		}
	}
	
	public static void Run(IEnumerator extendedCoRoutine, string methodName= "", object target = null)
	{
		var rr = new RadicalRoutine();
		rr.Method = methodName;
		rr.Target = target;
		rr.extended = extendedCoRoutine;
		if (rr.extended is INotifyStartStop)
		{
			(rr.extended as INotifyStartStop).Start();
		}
		rr.enumerator = rr.Execute(extendedCoRoutine);
		RadicalRoutineHelper.Current.Run(rr);
		
	}
	
	public static RadicalRoutine Create(IEnumerator extendedCoRoutine)
	{
		var rr = new RadicalRoutine();
		rr.extended = extendedCoRoutine;
		if (rr.extended is INotifyStartStop)
		{
			(rr.extended as INotifyStartStop).Start();
		}
		rr.enumerator = rr.Execute(extendedCoRoutine);
		return rr;
	}
	
	public void Run(string methodName= "", object target = null)
	{
		Method = methodName;
		Target = target;
		RadicalRoutineHelper.Current.Run(this);
	}
	
	private IEnumerator Execute(IEnumerator extendedCoRoutine, Action complete = null)
	{
		
		while (!cancel && extendedCoRoutine != null && (!LevelSerializer.IsDeserializing ? extendedCoRoutine.MoveNext() : true))
		{
			var v = extendedCoRoutine.Current;
			var cr = v as CoroutineReturn;
			if (cr != null)
			{
				if (cr.cancel)
				{
					cancel = true;
					break;
				}
				while (!cr.finished)
				{
					if (cr.cancel)
					{
						cancel = true;
						break;
					}
					yield return null;
				}
				if (cancel)
					break;
			}
			else
			if (v is IYieldInstruction)
			{
				yield return (v as IYieldInstruction).Instruction;
			}
			else
			{
				yield return v;
			}
		}
		
		Cancel();
	
		if (cancel)
		{
			Cancelled();
		}
	
		Finished();
		if (complete != null)
			complete();
		
		
		
	}
	
	
	#region IDeserialized implementation
	public void Deserialized()
	{
		
	}
	#endregion
}
