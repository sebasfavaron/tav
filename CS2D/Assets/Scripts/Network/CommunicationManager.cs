using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommunicationManager {

	List<Message> outMessages = new List<Message> ();
	int messagesReadyToSend = 0;
	List<Message> inMessages = new List<Message> ();

	int lastReceivedReliableMessageId = -1;
	int lastReceivedSendInEveryFramePacketMessageId = -1;
	int reliableSendInEveryPacketMessageId = 0;
	int reliableMessageId = 0;

	public int GetNewReliableSendInEveryPacketMessageId() {
		return ++reliableSendInEveryPacketMessageId;
	}

	public int GetNewReliableMessageId() {
		return ++reliableMessageId;
	}

	public int ReliableSendInEveryPacketMessageId {
		get {
			return reliableSendInEveryPacketMessageId;
		}
	}

	public int ReliableMessageId {
		get {
			return reliableMessageId;
		}
	}

	public int LastReceivedReliableMessageId {
		get {
			return lastReceivedReliableMessageId;
		}
		set {
			lastReceivedReliableMessageId = value;
		}
	}

	public int LastReliableSendInEveryPacketMessageIdReceived {
		get {
			return lastReceivedSendInEveryFramePacketMessageId;
		}
		set {
			lastReceivedSendInEveryFramePacketMessageId = value;
		}
	}

	public void SendMessage(Message message) {
		outMessages.Add (message);
	}

	public void ReceiveMessage(Message message) {
		if (message.Reliability == ReliabilityType.RELIABLE_MAX_WAIT_TIME) {
			SendMessage (AckReliableMessage.CreateAckReliableMessageMessageToSend (message.ReliabilityId));
			if (message.ReliabilityId == (LastReceivedReliableMessageId + 1)) {
				//accept it... valid message since its +1 since the last received
				//send to the sender that its reliable message has been received
				LastReceivedReliableMessageId = message.ReliabilityId;
			} else {
				//we need to discard it... either its been already processed or out of order
				return;
			}
			//send to the sender that its reliable message has been received
			SendMessage (AckReliableMessage.CreateAckReliableMessageMessageToSend (message.ReliabilityId));
		} else if (message.Reliability == ReliabilityType.RELIABLE_SEND_EVERY_PACKET) {
			//TODO: optimize this. if outMessages already contains an ack with id less than this one then it should be removed
			SendMessage (AckReliableSendEveryFrameMessage.CreateAckReliableSendEveryFrameMessageMessageToSend (message.ReliabilityId));
			if (message.ReliabilityId > LastReliableSendInEveryPacketMessageIdReceived) {	
				//accept it... valid message since its +1 since the last received
				//send to the sender that its reliable message has been received
				LastReliableSendInEveryPacketMessageIdReceived = message.ReliabilityId;
			} else {
				//must discard.. got out of order message
				return;
			}
		}

		switch (message.Type) {
		case MessageType.ACK_RELIABLE_MAX_WAIT_TIME:			
			ProcessAckReliable (message as AckReliableMessage);
			break;
		case MessageType.ACK_RELIABLE_SEND_EVERY_PACKET:
			ProcessAckReliableSendEveryFrame (message as AckReliableSendEveryFrameMessage);
			break;
		default:
			inMessages.Add (message);
			break;	
		}
	}

	void ProcessAckReliable(AckReliableMessage ackReliableMessage) {		
		for (int i = 0; i < outMessages.Count; i++) {
			Message message = outMessages [i];
			if (message.ReliabilityId == ackReliableMessage.MessageIdToAck) {
				outMessages.RemoveAt (i);
				break;
			}
		}
	}

	void ProcessAckReliableSendEveryFrame(AckReliableSendEveryFrameMessage ackReliableSendEveryFrameMessage) {
		for (int i = 0; i < outMessages.Count; i++) {
			Message message = outMessages [i];
			if (message.Reliability == ReliabilityType.RELIABLE_SEND_EVERY_PACKET &&
				message.ReliabilityId <= ackReliableSendEveryFrameMessage.MessageIdToAck) {
				outMessages.RemoveAt (i);
				i--;
			}
		}
	}

	private void Update() {
		messagesReadyToSend = 0;
		for (int i = 0; i < outMessages.Count; i++) {			
			Message message = outMessages [i];
			message.Update (Time.deltaTime);
			if (message.NeedsToBeSent) {
				messagesReadyToSend++;
			}
		}
	}


	public Packet BuildPacket() {
		Update ();

		if (messagesReadyToSend > 0) {
			Packet outPacket = new Packet ();
			outPacket.buffer.PutInt (messagesReadyToSend);
			for (int i = 0; i < outMessages.Count; i++) {
				Message serverMessage = outMessages [i];
				if (serverMessage.NeedsToBeSent) {
					serverMessage.Save (outPacket.buffer);
					if (serverMessage.Reliability == ReliabilityType.UNRELIABLE) {
						outMessages.RemoveAt (i);
						i--;
					} else if (serverMessage.Reliability == ReliabilityType.RELIABLE_SEND_EVERY_PACKET) {
						serverMessage.TimeToSend = 0;
					} else {
						serverMessage.TimeToSend = serverMessage.ReliableMaxTime;
					}
				}
			}

			outPacket.buffer.Flush ();
			return outPacket;
		} else {
			return null;
		}
	}

	public Message GetMessage() {
		if (inMessages.Count > 0) {
			Message message = inMessages [0];
			inMessages.RemoveAt (0);
			return message;
		} else {
			return null;
		}
	}

	public bool HasMessage() {
		return inMessages.Count > 0;
	}
}
