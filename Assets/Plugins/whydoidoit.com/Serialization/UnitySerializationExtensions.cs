using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Serialization;
using System.Linq;

[Serializer(typeof(Vector3))]
public class SerializeVector3 : SerializerExtensionBase<Vector3>
{
	public override IEnumerable<object> Save (Vector3 target)
	{
		return new object[] { target.x, target.y, target.z};
	}
	
	public override object Load (object[] data, object instance)
	{
		Radical.Log ("Vector3: {0},{1},{2}", data [0], data [1], data [2]);
		return new UnitySerializer.DeferredSetter (d => {
			return new Vector3 ((float)data [0], (float)data [1], (float)data [2]);
		}
		);
	}
}

[Serializer(typeof(AnimationState))]
public class SerializeAnimationState : SerializerExtensionBase<AnimationState>
{
	public override IEnumerable<object> Save (AnimationState target)
	{
		return new object[] { target.name};
	}
	
	public override object Load (object[] data, object instance)
	{
		var uo = UnitySerializer.DeserializingObject;
		return new UnitySerializer.DeferredSetter (d => {
			var p = uo.GetType ().GetProperty ("animation").GetGetMethod ();
			if (p != null) {
				var animation = (Animation)p.Invoke (uo, null);
				if (animation != null) {
					return animation [(string)data [0]];
					
				}
			}
			return null;
		});
	}
}

[Serializer(typeof(Quaternion))]
public class SerializeQuaternion : SerializerExtensionBase<Quaternion>
{
	public override IEnumerable<object> Save (Quaternion target)
	{
		
		return new object[] { target.x, target.y, target.z, target.w};
	}
	
	public override object Load (object[] data, object instance)
	{
		return new UnitySerializer.DeferredSetter (d => new Quaternion ((float)data [0], (float)data [1], (float)data [2], (float)data [3]));
	}
}

[Serializer(typeof(Bounds))]
public class SerializeBounds : SerializerExtensionBase<Bounds>
{
	public override IEnumerable<object> Save (Bounds target)
	{
		return new object[] { target.center.x, target.center.y, target.center.z, target.size.x, target.size.y, target.size.z };
	}

	public override object Load (object[] data, object instance)
	{
		return new Bounds (
				new Vector3 ((float)data [0], (float)data [1], (float)data [2]),
			    new Vector3 ((float)data [3], (float)data [4], (float)data [5]));
	}
}

public class SerializerExtensionBase<T> : ISerializeObjectEx
{
	#region ISerializeObject implementation
	public object[] Serialize (object target)
	{
		return Save ((T)target).ToArray ();
	}

	public object Deserialize (object[] data, object instance)
	{
		return Load (data, instance);
	}
	#endregion
	
	public virtual IEnumerable<object> Save (T target)
	{
		return new object[0];
	}
	
	public virtual object Load (object[] data, object instance)
	{
		return null;
	}
	
	#region ISerializeObjectEx implementation
	public bool CanSerialize (Type targetType, object instance)
	{
		return CanBeSerialized (targetType, instance);
	}
	#endregion
	
	public virtual bool CanBeSerialized (Type targetType, object instance)
	{
		return true;
	}


}

[Serializer(typeof(GUITexture))]
[Serializer(typeof(RenderTexture))]
[Serializer(typeof(MovieTexture))]
[Serializer(typeof(WebCamTexture))]
[Serializer(typeof(Material))]
[Serializer(typeof(AudioClip))]
[Serializer(typeof(Texture2D))]
[Serializer(typeof(Mesh))]
[Serializer(typeof(AnimationClip))]
public class SerializeTextureReference : SerializerExtensionBase<object>
{
	public override IEnumerable<object> Save (object target)
	{
		return new object [] { SaveGameManager.GetAssetId (target) };
	}
	
	public override bool CanBeSerialized (Type targetType, object instance)
	{
		return SaveGameManager.GetAssetId (instance).index != -1;
	}
	
	public override object Load (object[] data, object instance)
	{
		return SaveGameManager.GetAsset ((SaveGameManager.AssetReference)data [0]);
	}
}




/// <summary>
/// Store a reference to a game object, first checking whether it is really another game
/// object and not a prefab
/// </summary>
[Serializer(typeof(GameObject))]
public class SerializeGameObjectReference : SerializerExtensionBase<GameObject>
{
	static SerializeGameObjectReference ()
	{
		UnitySerializer.CanSerialize += (tp) => {
			return !(
				typeof(Bounds).IsAssignableFrom (tp) ||
				typeof(MeshFilter).IsAssignableFrom (tp) 
				
				);
		};
	}
	
	public override IEnumerable<object> Save (GameObject target)
	{
		return new object[] { target.GetId (), UniqueIdentifier.GetByName (target.gameObject.GetId ()) != null /* Identify a prefab */ };
	}
	
	public override object Load (object[] data, object instance)
	{
		if (instance != null) {
			return instance;
		}
		
		if (!((bool)data [1])) {
			Radical.Log ("[[Disabled, will not be set]]");
		}
		Radical.Log ("GameObject: {0}", data [0]);
		
		return instance ?? new UnitySerializer.DeferredSetter ((d) => {
			return UniqueIdentifier.GetByName ((string)data [0]) ;
		}) { enabled = (bool)data [1]};
	}
	
}

[ComponentSerializerFor(typeof(NavMeshAgent))]
public class SerializeNavMeshAgent : IComponentSerializer
{
	public class StoredInfo
	{
		public bool hasPath, offMesh;
		public float x, y, z, speed, angularSpeed, height, offset, acceleration;
	}
	
	#region IComponentSerializer implementation
	public byte[] Serialize (Component component)
	{
		var agent = (NavMeshAgent)component;
		return UnitySerializer.Serialize (new StoredInfo {
			x=agent.destination.x, 
			y = agent.destination.y, 
			z = agent.destination.z, 
		    speed = agent.speed,
			acceleration = agent.acceleration,
			angularSpeed = agent.angularSpeed,
			height = agent.height,
			offset = agent.baseOffset,
			hasPath = agent.hasPath,
			offMesh = agent.isOnOffMeshLink
		
		});
	}

	public void Deserialize (byte[] data, Component instance)
	{
		Loom.QueueOnMainThread (() => {
			var agent = (NavMeshAgent)instance;
			var si = UnitySerializer.Deserialize<StoredInfo> (data);
			var path = new NavMeshPath();
			agent.speed = si.speed;
			agent.angularSpeed = si.angularSpeed;
			agent.height = si.height;
			agent.baseOffset = si.offset;
			if(si.hasPath && !agent.isOnOffMeshLink)
			{
				agent.CalculatePath(new Vector3 (si.x, si.y, si.z), path);
				agent.SetPath(path);
			}
		}, 0.1f);
	
		
	}
	#endregion
	
}

[ComponentSerializerFor(typeof(Renderer))]
[ComponentSerializerFor(typeof(ClothRenderer))]
[ComponentSerializerFor(typeof(LineRenderer))]
[ComponentSerializerFor(typeof(TrailRenderer))]
[ComponentSerializerFor(typeof(ParticleRenderer))]
[ComponentSerializerFor(typeof(SkinnedMeshRenderer))]
[ComponentSerializerFor(typeof(MeshRenderer))]
public class SerializeRenderer : IComponentSerializer
{
	
	public class StoredInformation
	{
		public bool Enabled;
		public List<Color> materials = new List<Color> ();
	}
	
	#region IComponentSerializer implementation
	public byte[] Serialize (Component component)
	{
		using (new UnitySerializer.SerializationSplitScope()) {
			var renderer = (Renderer)component;
			var si = new StoredInformation ();
			si.Enabled = renderer.enabled;
			/*	foreach (var m in renderer.materials) {
				try {
					si.materials.Add (m.color);
				} catch {
					si.materials.Add (Color.black);
				}

			} */
			return UnitySerializer.Serialize (si);
			
		}
	}

	public void Deserialize (byte[] data, Component instance)
	{
		 
		using (new UnitySerializer.SerializationSplitScope()) {
			var renderer = (Renderer)instance;
			var si = UnitySerializer.Deserialize<StoredInformation> (data);
			if (si == null) {
				return;
			}
			renderer.enabled = si.Enabled;
			var i = 0;
			foreach (var m in renderer.materials) {
				try {
					m.color = si.materials [i++];
				} catch {
				
				}
			}
		}
	}
	#endregion
	
}

[SubTypeSerializer(typeof(Component))]
public class SerializeComponentReference: SerializerExtensionBase<Component>
{
	

	public override IEnumerable<object> Save (Component target)
	{
		return new object[] { target.gameObject.GetId (), UniqueIdentifier.GetByName (target.gameObject.GetId ()) != null, target.GetType ().AssemblyQualifiedName,"" /* Identify a prefab */ };
	}
	
	public override object Load (object[] data, object instance)
	{
		if (!((bool)data [1])) {
			Radical.Log ("[[Disabled, will not be set]]");
		}
		Radical.Log ("Component: {0}.{1}", data [0], data [2]);
		return new UnitySerializer.DeferredSetter ((d) => {
			var item = UniqueIdentifier.GetByName ((string)data [0]);
			return item != null ? item.GetComponent (Type.GetType ((string)data [2])) : null;
		}) { enabled = (bool)data [1]};
	}
}

public class ProvideAttributes : IProvideAttributeList
{
	private string[] _attributes;
	protected bool AllSimple = true;

	public ProvideAttributes (string[] attributes, bool allSimple = true)
	{
		_attributes = attributes;
		AllSimple = allSimple;
	}

	#region IProvideAttributeList implementation
	public IEnumerable<string> GetAttributeList (Type tp)
	{
		return _attributes;
	}
	#endregion

	#region IProvideAttributeList implementation
	public virtual bool AllowAllSimple (Type tp)
	{
		return AllSimple;
	}
	#endregion 
}

[AttributeListProvider(typeof(Camera))]
public class ProvideCameraAttributes : ProvideAttributes
{
	public ProvideCameraAttributes () : base(new string[0])
	{
	}
}

[AttributeListProvider(typeof(Transform))]
public class ProviderTransformAttributes : ProvideAttributes
{
	public ProviderTransformAttributes () : base(new string[] {
		"localPosition",
		"localRotation",
		"localScale"
	}, false)
	{
	}
}

[AttributeListProvider(typeof(Renderer))]
[AttributeListProvider(typeof(Collider))]
[AttributeListProvider(typeof(AudioListener))]
[AttributeListProvider(typeof(Joint))]
[AttributeListProvider(typeof(ParticleEmitter))]
[AttributeListProvider(typeof(Cloth))]
[AttributeListProvider(typeof(Light))]
[AttributeListProvider(typeof(MeshFilter))]
[AttributeListProvider(typeof(TextMesh))]
public class ProviderRendererAttributes : ProvideAttributes
{
	public ProviderRendererAttributes () : base(new string[] {
		"active",
		"text",
		"anchor",
		"alignment",
		"lineSpacing",
		"offsetZ",
		"playAutomatically",
		"animatePhysics",
		"tabSize",
		"enabled",
		"isTrigger",
		"emit",
		"minSize",
		"maxSize",
		"minEnergy",
		"maxEnergy",
		"minEmission",
		"maxEmission",
		"rndRotation",
		"rndVelocity",
		"rndAngularVelocity",
		"angularVelocity",
		"emitterVelocityScale",
		"localVelocity",
		"worldVelocity",
		"useWorldVelocity"
			
		
	}, false)
	{
		
	}
}

