using UnityEngine;
using System.Collections;

public class MainMenu : MonoBehaviour 
{

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}
	
	void OnGUI()
	{
		if(GUI.Button(new Rect(10, 10, 70, 20), "Button 1"))
		{
		}
		if(GUI.Button(new Rect(10, 30, 70, 20), "Button 2"))
		{
		}
		if(GUI.Button(new Rect(10, 50, 70, 20), "Button 3"))
		{
		}
	}
}
