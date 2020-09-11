using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNetworkView : MonoBehaviour {

	int id;

	public int Id {
		get {
			return id;
		}
		set {
			id = value;
		}
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void Load(PlayerData playerData) {		
		Vector2 position = playerData.Position;
		transform.position = new Vector3(position.x, position.y, 0);
	}
}
