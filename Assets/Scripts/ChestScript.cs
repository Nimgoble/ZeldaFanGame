using UnityEngine;
using System.Collections;

public class ChestScript : MonoBehaviour 
{
	public bool locked = false;
	public bool open = false;
	// Use this for initialization
	void Start () 
	{
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}
	
	public bool IsLocked() {return locked;}
	
	
	public void Open()
	{
		if(open)
			return;
		
		open = this.gameObject.animation.Play("Take 001");
	}
}
