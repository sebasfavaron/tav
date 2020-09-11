using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;

public enum MessageType {	
	ACK_RELIABLE_SEND_EVERY_PACKET,
	ACK_RELIABLE_MAX_WAIT_TIME,
	//server to client message type
	PLAYER_CONNECTED,
	PLAYER_DISCONNECTED,
	SNAPSHOT,
	//client to server message type
	CONNECT_PLAYER,
	DISCONNECT_PLAYER,
	PLAYER_INPUT,
	TOTAL
}

public enum ReliabilityType {
	UNRELIABLE,
	RELIABLE_SEND_EVERY_PACKET,
	RELIABLE_MAX_WAIT_TIME
}

public class Message {
	MessageType messageType;
	ReliabilityType reliabilityType;
	int id;
	float timeToSend;
	float reliableMaxTime;
	IPEndPoint fromEndPoint;

	public Message(int id, MessageType messageType, ReliabilityType reliabilityType) {
		this.messageType = messageType;
		this.id = id;
		this.reliabilityType = reliabilityType;
	}

	public void Update(float dt) {
		timeToSend -= dt;
	}	

	public virtual void Load(BitBuffer bitBuffer) {
		id = bitBuffer.GetInt ();
	}

	public virtual void Save(BitBuffer bitBuffer) {
		bitBuffer.PutEnum(messageType, (int)MessageType.TOTAL);
		bitBuffer.PutInt (id);
	}		
		
	public MessageType Type {
		get {
			return messageType;
		}
	}

	public ReliabilityType Reliability {
		get {
			return reliabilityType;
		}
	}

	public bool NeedsToBeSent {
		get {
			return timeToSend <= 0;
		}
	}

	public float TimeToSend {
		set {
			timeToSend = value;
		}
	}

	public float ReliableMaxTime {
		get {
			return reliableMaxTime;
		}
		set {
			reliableMaxTime = value;
		}
	}	

	public int ReliabilityId {
		get {
			return id;
		}
	}

	public IPEndPoint From {
		get {
			return fromEndPoint;
		}
		set {
			fromEndPoint = value;
		}
	}
}
	
//from client to server
	
public class DisconnectPlayerMessage : Message {

	//used by server
	public DisconnectPlayerMessage() : base(-1, MessageType.DISCONNECT_PLAYER, ReliabilityType.RELIABLE_MAX_WAIT_TIME) {		
	}

	//used by client
	public DisconnectPlayerMessage(int id, int playerId) : base(id, MessageType.DISCONNECT_PLAYER, ReliabilityType.RELIABLE_MAX_WAIT_TIME) {
	}		
}

public class PlayerInputMessage : Message {
	PlayerInput playerInput;

	//used by server
	public PlayerInputMessage() : base(-1, MessageType.PLAYER_INPUT, ReliabilityType.UNRELIABLE) {
		this.playerInput = new PlayerInput ();
	}

	//used by client
	public PlayerInputMessage(int id, PlayerInput playerInput) : base(id, MessageType.PLAYER_INPUT, ReliabilityType.UNRELIABLE) {
		this.playerInput = playerInput;
	}

	public override void Load (BitBuffer bitBuffer) {
		base.Load (bitBuffer);
		playerInput.Load (bitBuffer);
	}

	public override void Save(BitBuffer bitBuffer) {
		base.Save(bitBuffer);
		playerInput.Save (bitBuffer);
	}

	public PlayerInput Input {
		get {
			return playerInput;
		}
	}
}

public class SnapshotMessage : Message {
	GameData gameData;

	//used by client
	public SnapshotMessage() : base(-1, MessageType.SNAPSHOT, ReliabilityType.UNRELIABLE) {
		gameData = new GameData ();
	}

	//used by server
	public SnapshotMessage(int id, GameData gameData) : base(id, MessageType.SNAPSHOT, ReliabilityType.UNRELIABLE) {
		this.gameData = gameData;
	}

	public override void Load (BitBuffer bitBuffer) {
		base.Load (bitBuffer);
		gameData.Load (bitBuffer);
	}

	public override void Save(BitBuffer bitBuffer) {
		base.Save(bitBuffer);
		gameData.Save (bitBuffer);
	}

	public GameData GameSnapshot {
		get {
			return gameData;
		}
	}
}
