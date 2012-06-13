using UnityEngine;
using UnityEditor;
using System.Collections;

public class PlayerMenuItem
{
	[MenuItem("ZeldaFanGame/Create Player")]
    static void Create()
    {
		GameObject player = GameObject.Instantiate(Resources.Load("Prefabs/Player")) as GameObject;
		player.transform.position.Set(0,0,0);
		player.transform.eulerAngles.Set(0,0,0);
    }
}
