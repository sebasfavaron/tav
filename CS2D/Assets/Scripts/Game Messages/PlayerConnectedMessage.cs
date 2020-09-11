using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerConnectedMessage : Message {
	int playerId;

	public static PlayerConnectedMessage CreatePlayerConnectedMessageToSend(Player receiver, int playerConnectedId) {
		int messageId = receiver.CommunicationManager.GetNewReliableSendInEveryPacketMessageId ();
		return new PlayerConnectedMessage (messageId, playerConnectedId);
	}

	public static PlayerConnectedMessage CreatePlayerConnectedMessageToReceive() {
		return new PlayerConnectedMessage (-1, -1);
	}
		
	private PlayerConnectedMessage(int id, int playerId) : base(id, MessageType.PLAYER_CONNECTED, ReliabilityType.RELIABLE_SEND_EVERY_PACKET) {
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
}
