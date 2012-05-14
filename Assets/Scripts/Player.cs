using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
	//public Camera tpCamera = null;
	//TODO: This is stupid.  Rewrite ThirdPersonController so it inherits from CharacterController.
	//Then get rid of this stupid member.
	//EDIT: Oh, hey.  They made CharacterController a sealed class.  GG, ass-clowns.
	//public ThirdPersonController thirdPersonController = null;
	public Fairy fairy = null;
	public CharacterController characterController = null;
	private GameObject cameraObject = null;
	private GameObject fairyObject = null;
	// Use this for initialization
	void Start () 
	{	
		//Make a character controller if one wasn't assigned.
		/*if(characterController == null)
			characterController = (CharacterController)this.gameObject.AddComponent("CharacterController");*/
		
		/*if(tpCamera == null)
		{
			//This is so we can instantiate a camera in the worldspace if they haven't assigned one.
			cameraObject = new GameObject("Camera Object");
			cameraObject.transform.parent = transform;
			cameraObject.transform.position = this.transform.position;
			cameraObject.transform.Translate(new Vector3(0.0f, 1.3f, -3.0f));
			cameraObject.transform.LookAt(this.transform.position);
			tpCamera = (Camera)cameraObject.AddComponent("Camera");
			tpCamera.name = "Player Camera";
		}
		if(thirdPersonController == null)
			thirdPersonController = (ThirdPersonController)gameObject.AddComponent("ThirdPersonController");*/
		
		/*thirdPersonController.SetCamera(tpCamera);
		//TODO: Inheritance is your friend
		thirdPersonController.SetController(characterController);
		thirdPersonController.Awake();*/
		
		if(fairy == null)
		{
			fairyObject = new GameObject("Fairy Object");
			fairyObject.transform.parent = transform;
			fairyObject.transform.position = transform.position;
			fairyObject.transform.localRotation = transform.localRotation;
			fairyObject.transform.Translate(new Vector3(-1.0f, 0.5f, -0.5f));
			fairy = (Fairy)fairyObject.AddComponent("Fairy");
			fairy.Start();
		}
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}
}
