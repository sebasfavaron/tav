using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameData {

	float time;
	List<PlayerData> playersData = new List<PlayerData>();

	public void Save(BitBuffer bitBuffer) {
		bitBuffer.PutFloat (time);
		bitBuffer.PutInt (playersData.Count);
		for (int i = 0; i < playersData.Count; i++) {
			playersData [i].Save (bitBuffer); 
		}
	}

	public void Load(BitBuffer bitBuffer) {
		time = bitBuffer.GetFloat ();
		int playerCount = bitBuffer.GetInt ();
		for (int i = 0; i < playerCount; i++) {
			PlayerData playerData = new PlayerData ();
			playerData.Load (bitBuffer);
			playersData.Add (playerData);
		}
	}

	public List<PlayerData> Players {
		get {
			return playersData;
		}
	}

	public float Time {
		get {
			return time;
		}
		set {
			time = value;
		}
	}
}
