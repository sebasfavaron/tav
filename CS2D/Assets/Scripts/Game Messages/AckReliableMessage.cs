using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AckReliableMessage : Message {

	int messageIdToAck;

	public static AckReliableMessage CreateAckReliableMessageMessageToSend(int messageIdToAck) {
		return new AckReliableMessage (-1, messageIdToAck);
	}

	public static AckReliableMessage CreateAckReliableMessageMessageToReceive() {
		return new AckReliableMessage (-1, -1);
	}
		
	private AckReliableMessage(int id, int messageIdToAck) : base(id, MessageType.ACK_RELIABLE_MAX_WAIT_TIME, ReliabilityType.UNRELIABLE) {		
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
