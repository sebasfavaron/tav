using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

	PlayerInput playerInput = new PlayerInput();

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		playerInput.up = UnityEngine.Input.GetKey (KeyCode.W);
		playerInput.down = UnityEngine.Input.GetKey (KeyCode.S);
		playerInput.left = UnityEngine.Input.GetKey (KeyCode.A);
		playerInput.right = UnityEngine.Input.GetKey (KeyCode.D);
		playerInput.shoot = UnityEngine.Input.GetKey (KeyCode.Space);
	}

	public PlayerInput Input {
		get {
			return playerInput;
		}
	}
}
