using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;

public class ConnectPlayerMessage : Message {
	int playerId;
	IPEndPoint endPoint;

	public static ConnectPlayerMessage CreateConnectPlayerMessageToSend(int playerId) {
		return new ConnectPlayerMessage (playerId);
	}

	public static ConnectPlayerMessage CreateConnectPlayerMessageToReceive(IPEndPoint endPoint) {
		return new ConnectPlayerMessage (endPoint);
	}

	private ConnectPlayerMessage(IPEndPoint endPoint) : base(-1, MessageType.CONNECT_PLAYER, ReliabilityType.UNRELIABLE) {
		this.endPoint = endPoint;
	}
		
	private ConnectPlayerMessage(int playerId) : base(-1, MessageType.CONNECT_PLAYER, ReliabilityType.UNRELIABLE) {		
		this.playerId = playerId;
	}

	public override void Load (BitBuffer bitBuffer) {
		base.Load (bitBuffer);
		playerId = bitBuffer.GetInt ();
	}

	public override void Save(BitBuffer bitBuffer) {
		base.Save(bitBuffer);
		bitBuffer.PutInt(playerId);
	}

	public int PlayerId {
		get {
			return playerId;
		}
	}

	public IPEndPoint EndPoint {
		get {
			return endPoint;
		}
	}
}