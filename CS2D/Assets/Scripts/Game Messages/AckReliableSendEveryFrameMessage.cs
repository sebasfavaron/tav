using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AckReliableSendEveryFrameMessage : Message {

	int messageIdToAck;

	public static AckReliableSendEveryFrameMessage CreateAckReliableSendEveryFrameMessageMessageToSend(int messageIdToAck) {
		return new AckReliableSendEveryFrameMessage (-1, messageIdToAck);
	}

	public static AckReliableSendEveryFrameMessage CreateAckReliableSendEveryFrameMessageMessageToReceive() {
		return new AckReliableSendEveryFrameMessage (-1, -1);
	}

	private AckReliableSendEveryFrameMessage(int id, int messageIdToAck) : base(id, MessageType.ACK_RELIABLE_SEND_EVERY_PACKET, ReliabilityType.UNRELIABLE) {
		this.messageIdToAck = messageIdToAck;
	}

	public override void Load (BitBuffer bitBuffer) {
		base.Load (bitBuffer);
		messageIdToAck = bitBuffer.GetInt ();
	}

	public override void Save(BitBuffer bitBuffer) {
		base.Save(bitBuffer);
		bitBuffer.PutInt(messageIdToAck);
	}

	public int MessageIdToAck {
		get {
			return messageIdToAck;
		}
	}

}
