using UnityEngine;
using System.Collections;

public class GameManager : MonoSingleton<GameManager> 
{
	private string currentLevel;
	// Use this for initialization
	void Start () 
	{
	
	}
	
	public string GetCurrentLevel()
	{
		return instance.currentLevel;
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}
}
