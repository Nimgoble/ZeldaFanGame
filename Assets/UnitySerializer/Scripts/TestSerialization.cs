using UnityEngine;
using System.Collections;
using Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;


public class TestSerialization : MonoBehaviour
{

	
	void OnGUI()
	{
		
		
		if(GUILayout.Button("Save"))
		{
			//Save the game with a prefix of Game
			LevelSerializer.SaveGame("Game");
			Radical.CommitLog();
		}
		
		//Check to see if there is resume info
		if(LevelSerializer.CanResume)
		{
			if(GUILayout.Button("Resume"))
			{
				LevelSerializer.Resume();
			}
		}
		
		if(LevelSerializer.SavedGames.Count > 0)
		{
			GUILayout.Label("Available saved games");
			//Look for saved games under the given player name
			foreach(var g in LevelSerializer.SavedGames[LevelSerializer.PlayerName])
			{
				if(GUILayout.Button(g.Caption))
				{
					g.Load();
				}
					
			}
		}
	}
	
	// Update is called once per frame
	void Update()
	{

	}
}


