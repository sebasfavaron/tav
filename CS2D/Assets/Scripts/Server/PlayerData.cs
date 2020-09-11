using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData {

	int playerId;
	Vector2 position = new Vector2();

	public int PlayerId {
		get {
			return playerId;
		}
		set {
			playerId = value;
		}
	}

	public Vector2 Position {
		get {
			return position;
		}
		set {
			position = value;
		}
	}

	public void Save(BitBuffer bitBuffer) {
		bitBuffer.PutInt (playerId);
		bitBuffer.PutFloat (position.x);
		bitBuffer.PutFloat (position.y);
	}

	public void Load(BitBuffer bitBuffer) {
		playerId = bitBuffer.GetInt ();
		float x = bitBuffer.GetFloat ();
		float y = bitBuffer.GetFloat ();
		position.Set (x, y);
	}
}
