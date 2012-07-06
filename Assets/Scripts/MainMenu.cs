using UnityEngine;
using System.Collections;

public class MainMenu : MonoBehaviour 
{
	private const float buttonWidth = 200.0f;
	private const float buttonHeight = 50.0f;
	private string [] buttonTexts = {"New Game", "Load Game", "Options"};
	
	// Use this for initialization
	public void Start () 
	{
	}
	
	public void Stop()
	{
	}
	
	// Update is called once per frame
	void Update () 
	{
	}
	
	void OnGUI()
	{
		float top = Screen.height / 2;
		float left = Screen.width / 2;
		left -= buttonWidth / 2;
		for(int i = 0; i < buttonTexts.Length; i++)
		{
			//TODO: Put in "Continue" button if there are saved games.
			if(GUI.Button(new Rect(left, top + ((i * buttonHeight) + 5), buttonWidth, buttonHeight), buttonTexts[i]))
			{
			}
		}
	}
}
