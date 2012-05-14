using UnityEngine;
using UnityEditor;
using System.Collections;

[ExecuteInEditMode]
public class Fairy : MonoBehaviour 
{
	public ParticleSystem ourParticleSystem = null;
	public Mesh ourMesh = null;
	// Use this for initialization
	public void Start () 
	{
		//If we weren't assigned an emitter, create one.
		if(ourParticleSystem == null)
		{
			ourParticleSystem = (ParticleSystem)gameObject.AddComponent("ParticleSystem");
			ParticleEmitter.DestroyImmediate(ourParticleSystem.particleEmitter);
			ourParticleSystem.gameObject.AddComponent("EllipsoidParticleEmitter");
			
			ourParticleSystem.loop = true;
			ourParticleSystem.startSize = 0.1f;
			ourParticleSystem.emissionRate = 100.0f;
			ourParticleSystem.startColor = new Color(130.0f/255.0f, 219.0f/255.0f, 253.0f/255.0f, 150.0f/255.0f);
			ourParticleSystem.startSpeed = 0.1f;
			ourParticleSystem.startLifetime = 0.25f;
			//ourParticleSystem.gameObject.AddComponent("EllipsoidParticleEmitter");
			ourParticleSystem.transform.position = transform.position;
		}
		ourParticleSystem.name = "Fairy Particle System";
		ourParticleSystem.transform.position = this.gameObject.transform.position + new Vector3(-1.0f, 0.5f, -0.5f);
		ourParticleSystem.transform.parent = transform;
		ourParticleSystem.Play();
	}
	
	// Update is called once per frame
	void Update () 
	{
	}
}
