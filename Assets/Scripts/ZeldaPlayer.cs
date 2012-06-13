using UnityEngine;
using UnityEditor;
using System.Collections;

public class ZeldaPlayer : MonoBehaviour
{
	//Not actually sure what I'm going to do with this, anymore.
	//It seems that prefabs have taken the place of actual classes.
	//I don't like it, but there we have it.
	
	private GameObject fairy = null;
	private Vector3 fairyOffset = new Vector3(-1.0f, 1.5f, -1.0f);
	public GameObject ourCamera = null;
	private Texture2D [] heartTextures;
	private uint health = 12;
	private uint maxHealth = 12;
	private uint keys = 0;
	// Use this for initialization
	public void Start () 
	{
		if(fairy == null)
		{
			fairy = Instantiate(Resources.Load("Prefabs/Fairy")) as GameObject;
			fairy.transform.localPosition = this.transform.localPosition + fairyOffset;
			fairy.transform.parent = this.transform;
		}
		if(ourCamera == null)
		{
			ourCamera = new GameObject();
			ourCamera.transform.position = this.transform.position;
			ourCamera.AddComponent(typeof(Camera));
			ourCamera.name = "Camera Object";
			ourCamera.transform.LookAt(this.transform);
		}
		//Setup our camera
		ThirdPersonCamera tpCam = this.gameObject.GetComponent("ThirdPersonCamera") as ThirdPersonCamera;
		tpCam.SetCamera(ourCamera.transform);
		
		//Initialize hearts
		heartTextures = new Texture2D[4]
		{ 
			(Texture2D)Resources.Load("Heart1", typeof(Texture2D)),
			(Texture2D)Resources.Load("Heart2", typeof(Texture2D)),
			(Texture2D)Resources.Load("Heart3", typeof(Texture2D)),
			(Texture2D)Resources.Load("Heart4", typeof(Texture2D))
		};
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(Input.GetKeyDown(KeyCode.E))
		{
			RaycastHit hit = new RaycastHit();
			Debug.DrawRay(this.collider.bounds.center, this.transform.forward, Color.green, 100.0f);
			if(Physics.Raycast(this.collider.bounds.center, this.transform.forward, out hit, 100.0f))
			{
				GameObject obj = hit.collider.gameObject;
				//Get absolute parent
				while(obj.transform.parent != null)
					obj = obj.transform.parent.gameObject;
				if(obj.tag == "Chest")
				{
					ChestScript chest = (ChestScript)obj.GetComponent(typeof(ChestScript));
					//If locked and we have a key
					if(chest.IsLocked() && keys > 0)
					{
						keys--;
						chest.Open();
					}
					else//Unlocked
						chest.Open();
				}
			}
		}
	}
	
	void OnGUI()
	{
		DrawHealth ();
		//DrawMagic
		//DrawEtc
	}
	
	private void DrawHealth()
	{
		uint wholeHearts = health / 4;
		uint partialHeart = health % 4;
		Rect place = new Rect(15, 15, 32, 32);
		for(int i = 0; i < wholeHearts; i++)
		{
			place.x += 33;
			GUI.DrawTexture(place, heartTextures[3]);
		}
		if( partialHeart > 0 )
		{
			place.x += 33;
			GUI.DrawTexture(place, heartTextures[partialHeart - 1]);
		}
	}
}
