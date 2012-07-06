using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using Serialization;

//Do not add this script to your own classes! This is created internally
[AddComponentMenu("Storage/Internal/Level Loader (Internal use only, do not add this to your objects!)")]
public class LevelLoader : MonoBehaviour
{
	public LevelSerializer.LevelData Data;
	
	static Texture2D pixel;
	float alpha = 1;
	bool loading = true;
	

	void Awake()
	{
		if(pixel==null)
		{
			pixel = new Texture2D(1,1);
		}
	}
	
	void OnGUI()
	{
		if(!loading && Event.current.type == EventType.repaint)
		{
			alpha = Mathf.Clamp01(alpha - 0.02f);
		}
		else if(alpha == 0)
		{
			GameObject.Destroy(gameObject);
		}
		if(alpha != 0)
		{
			pixel.SetPixel(0,0,new Color(1,1,1,alpha));
			pixel.Apply();
			GUI.DrawTexture(new Rect(0,0,Screen.width, Screen.height), pixel, ScaleMode.StretchToFill);
		}
		
	}
	
	void OnLevelWasLoaded(int level)
	{
		StartCoroutine(Load());
	}
		
	IEnumerator Load ()
	{
		//Need to wait while the base level is prepared, it takes 2 frames
		yield return null;
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();

		//First step is to remove any items that should not exist according to the saved scene
		foreach (var go in UniqueIdentifier.AllIdentifiers.Where(n=>!Data.StoredObjectNames.Any(sn=>sn.Name == n.Id)).ToList())
		{
			Radical.LogNow("Destroying {0}", go.ToString());
			GameObject.Destroy (go.gameObject);
		}

		//Next we need to instantiate any items that are needed by the stored scene
		foreach(var sto in Data.StoredObjectNames.Where(c=>UniqueIdentifier.GetByName(c.Name) == null && !string.IsNullOrEmpty(c.ClassId) ))
		{

			var pf = LevelSerializer.AllPrefabs[sto.ClassId];
			sto.GameObject = Instantiate(pf) as GameObject;
			sto.GameObject.GetComponent<UniqueIdentifier>().Id = sto.Name;
			if(sto.ChildIds.Count > 0)
			{
				var list = sto.GameObject.GetComponentsInChildren<UniqueIdentifier>().ToList();
				for(var i = 0; i < list.Count && i < sto.ChildIds.Count; i++)
				{
					list[i].Id = sto.ChildIds[i];
				}
			}

		}

		foreach(var so in Data.StoredObjectNames)
		{
			var go = UniqueIdentifier.GetByName(so.Name);
			if(go == null)
			{
				Radical.LogNow("Could not find " + so.Name);
			}
			else
				go.SetActiveRecursively(so.Active);
		}



		foreach(var go in Data.StoredObjectNames.Where(c=>!string.IsNullOrEmpty(c.ParentName)))
		{
			var parent = UniqueIdentifier.GetByName(go.ParentName);
			var item = UniqueIdentifier.GetByName(go.Name);
			if(item != null && parent != null)
			{
				item.transform.parent = parent.transform;
			}
		}
		
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();


		//using (new Radical.Logging())
		{


			using (new UnitySerializer.SerializationScope())
			{
				//Now we restore the data for the items
				foreach (var item in Data.StoredItems.GroupBy(i=>i.Name,(name, cps)=>
					new {
					  Name = name,
					  Components = cps.Where(cp=>cp.Name==name).GroupBy(cp=>cp.Type, (type, components)=>new { Type = type, List = components.ToList() } ).ToList()
				}))
				{
					Radical.Log ("\n*****************\n{0}\n********START**********\n", item.Name);
					Radical.IndentLog ();
					var go = UniqueIdentifier.GetByName (item.Name);
					if (go == null)
					{
						Radical.LogWarning (item.Name + " was null");
						continue; 
						
					}
					foreach (var cp in item.Components)
					{
						Type type = Type.GetType (cp.Type);
						if (type == null)
						{
							continue;
						}
						Radical.Log ("<{0}>\n", type.FullName);
						Radical.IndentLog ();

						var list = go.GetComponents (type).Where (c => c.GetType () == type).ToList ();
						//Make sure the lists are the same length
						while (list.Count > cp.List.Count)
						{
							Radical.LogNow("Destroying {0} on {1} wanted {2} found {3}", cp.ToString(), go.ToString(), cp.List.Count, list.Count);
							Component.Destroy (list.Last ());
							list.Remove (list.Last ());
						}
						if(type == typeof(NavMeshAgent)) 
						{
							Action perform = ()=>{
								var comp = cp;
								var tp = type;
								var tname = item.Name;
								UnitySerializer.AddFinalAction(()=>{
									var g = UniqueIdentifier.GetByName (tname);
									var nlist = g.GetComponents (tp).Where (c => c.GetType () == tp).ToList ();
									while (nlist.Count < comp.List.Count)
									{
										try
										{
											nlist.Add (g.AddComponent (tp));
										} catch
										{
										}
									}
									list = list.Where (l => l != null).ToList ();
									//Now deserialize the items back in
									for (var i =0; i < list.Count; i++)
									{
										if (LevelSerializer.CustomSerializers.ContainsKey (tp))
										{
											LevelSerializer.CustomSerializers [tp].Deserialize (comp.List [i].Data, nlist [i]);
										} else
										{
											UnitySerializer.DeserializeInto (comp.List [i].Data, nlist [i]);
										}
									}
								});
							};
							perform();
							
						} else {
							while (list.Count < cp.List.Count)
							{
								try
								{
									list.Add (go.AddComponent (type));
								} catch
								{
								}
							}
							list = list.Where (l => l != null).ToList ();
							//Now deserialize the items back in
							for (var i =0; i < list.Count; i++)
							{
								Radical.Log (string.Format ("Deserializing {0} for {1}", type.Name, go.GetFullName ()));
								if (LevelSerializer.CustomSerializers.ContainsKey (type))
								{
									LevelSerializer.CustomSerializers [type].Deserialize (cp.List [i].Data, list [i]);
								} else
								{
									UnitySerializer.DeserializeInto (cp.List [i].Data, list [i]);
								}
							}
						}
						Radical.OutdentLog ();
						Radical.Log ("</{0}>", type.FullName);
					}
					Radical.OutdentLog ();
					Radical.Log ("\n*****************\n{0}\n********END**********\n\n", item.Name);
					
				}
				
	
				yield return null;
				//Finally we need to fixup any references to other game objects,
				//these have been stored in a list inside the serializer
				//waiting for us to call this.  Vector3s are also deferred until this point
				UnitySerializer.RunDeferredActions ();
			
				yield return null;
				yield return null;
			
				
				UnitySerializer.InformDeserializedObjects ();
			
				//Flag that we aren't deserializing
				LevelSerializer.IsDeserializing = false;
			
				//Tell the world that the level has been loaded
				LevelSerializer.InvokeDeserialized ();
				loading = false;
				//Get rid of the current object that is holding this level loader, it was
				//created solely for the purpose of running this script
				GameObject.Destroy (this.gameObject, 1.1f);
			
			}
		}
	}

}