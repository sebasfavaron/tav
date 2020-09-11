using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Client : MonoBehaviour {

	[Header("Connection")]
	public string serverIp = "127.0.0.1";
	public int serverPort;
	public int clientPort;
	Channel channel;

	int reliableSendInEveryPacketMessageId = 0;
	int reliableMessageId = 0;
	int lastReceivedReliableMessageId = -1;
	int lastReceivedSendInEveryFramePacketMessageId = -1;
	List<Message> outMessages = new List<Message>();
	public int playerId;
	private PlayerController localPlayerController;

	[Header("Game")]
	public Object playerPrefab;
	private List<PlayerNetworkView> players = new List<PlayerNetworkView>();

	public int LastReceivedSendInEveryFramePacketMessageId {
		get {
			return lastReceivedSendInEveryFramePacketMessageId;
		}
		set {
			lastReceivedSendInEveryFramePacketMessageId = value;
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

	void Start() {
		channel = new Channel(serverIp, clientPort, serverPort);
	}

	void OnDestroy() {
		channel.Disconnect();
	}

	public int GetNewReliableSendInEveryPacketMessageId() {
		reliableSendInEveryPacketMessageId++;
		return reliableSendInEveryPacketMessageId - 1;
	}

	public int GetNewReliableMessageId() {
		reliableMessageId++;
		return reliableMessageId - 1;
	}

	void Update() {
		if (Input.GetKeyDown(KeyCode.C)) {
			//send player connect message
			outMessages.Add(ConnectPlayerMessage.CreateConnectPlayerMessageToSend(playerId));
		}

		if (Input.GetKeyDown (KeyCode.V)) {
			//send player disconnect message
			Packet p = new Packet();
			DisconnectPlayerMessage disconnectPlayerMessage = new DisconnectPlayerMessage(GetNewReliableMessageId(), playerId);
			p.buffer.PutInt(1);
			disconnectPlayerMessage.Save(p.buffer);
			p.buffer.Flush ();
			channel.Send(p);
		}

		Packet inPacket = channel.GetPacket ();
		if (inPacket != null) {			
			//read it!
			BitBuffer bitBuffer = inPacket.buffer;
			int messageCount = bitBuffer.GetInt ();
			List<Message> serverMessages = new List<Message> ();
			for (int i = 0; i < messageCount; i++) {
				//parse message
				Message serverMessage = ReadServerMessage(bitBuffer);
				if (serverMessage != null) {
					serverMessages.Add (serverMessage);
				}
			}
			for (int i = 0; i < serverMessages.Count; i++) {
				ProcessServerMessage(serverMessages[i]);
			}
		}

		if (localPlayerController != null) {
			PlayerInputMessage playerInputMessage = new PlayerInputMessage (-1, localPlayerController.Input);
			outMessages.Add (playerInputMessage);
			outMessages.Add(AckReliableSendEveryFrameMessage.CreateAckReliableSendEveryFrameMessageMessageToSend(lastReceivedSendInEveryFramePacketMessageId));
		}			

		int messagesReadyToSend = 0;
		for (int i = 0; i < outMessages.Count; i++) {			
			Message clientMessage = outMessages [i];
			clientMessage.Update (Time.deltaTime);
			if (clientMessage.NeedsToBeSent) {
				messagesReadyToSend++;
			}
		}

		if (messagesReadyToSend > 0) {
			Packet outPacket = new Packet ();
			outPacket.buffer.PutInt (outMessages.Count);
			for (int i = 0; i < outMessages.Count; i++) {
				Message clientMessage = outMessages [i];
				clientMessage.Save (outPacket.buffer);
				if (clientMessage.Reliability == ReliabilityType.UNRELIABLE) {
					outMessages.RemoveAt (i);
					i--;
				} else if (clientMessage.Reliability == ReliabilityType.RELIABLE_SEND_EVERY_PACKET) {
					clientMessage.TimeToSend = 0;
				} else {
					clientMessage.TimeToSend = clientMessage.ReliableMaxTime;
				}
			}
			
			outPacket.buffer.Flush ();
			channel.Send (outPacket);
		}
	}

	Message ReadServerMessage(BitBuffer bitBuffer) {
		MessageType messageType = bitBuffer.GetEnum<MessageType> ((int)MessageType.TOTAL);
		Message serverMessage = null;
		switch (messageType) {
		case MessageType.PLAYER_CONNECTED:
			serverMessage = PlayerConnectedMessage.CreatePlayerConnectedMessageToReceive ();
			break;
		case MessageType.PLAYER_DISCONNECTED:
			serverMessage = PlayerDisconnectedMessage.CreatePlayerDisconnectedMessageToReceive();
			break;
		case MessageType.SNAPSHOT:
			serverMessage = new SnapshotMessage ();
			break;
		case MessageType.ACK_RELIABLE_MAX_WAIT_TIME:
			Debug.Log ("Client::got ack reliable wait time ");
			serverMessage = AckReliableMessage.CreateAckReliableMessageMessageToReceive ();
			break;
		case MessageType.ACK_RELIABLE_SEND_EVERY_PACKET:
			serverMessage = AckReliableSendEveryFrameMessage.CreateAckReliableSendEveryFrameMessageMessageToReceive ();
			break;
		default:
			Debug.LogError("Got a server message that cannot be understood");
			return null;
		}
		serverMessage.Load(bitBuffer);

		if (serverMessage.Reliability == ReliabilityType.RELIABLE_MAX_WAIT_TIME) {
			if (serverMessage.ReliabilityId == (LastReceivedReliableMessageId + 1)) {
				//accept it... valid message since its +1 since the last received
				//send to the sender that its reliable message has been received
				outMessages.Add (AckReliableMessage.CreateAckReliableMessageMessageToSend (serverMessage.ReliabilityId));
				LastReceivedReliableMessageId = serverMessage.ReliabilityId;
			} else {
				//we need to discard it... either its been already processed or out of order
				return null;
			}
		} else if (serverMessage.Reliability == ReliabilityType.RELIABLE_SEND_EVERY_PACKET) {						
			if (serverMessage.ReliabilityId > LastReceivedSendInEveryFramePacketMessageId) {
				//set the last "reliable send in every packet" message id.. it will get sent to the server on the next packet
				LastReceivedSendInEveryFramePacketMessageId = serverMessage.ReliabilityId;
				Debug.Log ("Client::lastReceivedSendInEveryFramePacketMessageId = " + lastReceivedSendInEveryFramePacketMessageId);
			} else {
				//discard
				return null;
			}
		}

		return serverMessage;
	}

	void ProcessServerMessage(Message serverMessage) {
		Debug.Log ("Client::ProcessServerMessage " + serverMessage.Type);
		switch (serverMessage.Type) {
		case MessageType.PLAYER_CONNECTED:
			ProcessPlayerConnected(serverMessage as PlayerConnectedMessage);
			break;
		case MessageType.PLAYER_DISCONNECTED:
			ProcessPlayerDisconnected(serverMessage as PlayerDisconnectedMessage);
			break;
		case MessageType.SNAPSHOT:
			ProcessSnapshot(serverMessage as SnapshotMessage);
			break;
		case MessageType.ACK_RELIABLE_MAX_WAIT_TIME:			
			ProcessAckReliable (serverMessage as AckReliableMessage);
			break;
		case MessageType.ACK_RELIABLE_SEND_EVERY_PACKET:
			ProcessAckReliableSendEveryFrame (serverMessage as AckReliableSendEveryFrameMessage); 
			break;
		}			
	}

	public void ProcessPlayerConnected(PlayerConnectedMessage playerConnectedMessage) {
		int playerId = playerConnectedMessage.PlayerId;
		GameObject playerGO = Instantiate (playerPrefab) as GameObject;	
		playerGO.name = "Player Network View " + playerId;
		PlayerNetworkView player = playerGO.GetComponent<PlayerNetworkView> ();
		player.Id = playerId;
		if (playerId == this.playerId) {
			localPlayerController = playerGO.AddComponent<PlayerController> ();
		}
		players.Add (player);
	}

	public void ProcessPlayerDisconnected(PlayerDisconnectedMessage playerDisconnectedMessage) {
		int playerId = playerDisconnectedMessage.PlayerId;
		PlayerNetworkView player = GetPlayerWithId (playerId);
		if (player != null) {
			players.Remove (player);
			Destroy (player);
		}
	}

	public void ProcessSnapshot(SnapshotMessage snapshotMessage) {
		List<PlayerData> playersData = snapshotMessage.GameSnapshot.Players;
		for (int i = 0; i < playersData.Count; i++) {
			PlayerData playerData = playersData[i];
			PlayerNetworkView playerNetworkView = GetPlayerWithId (playerData.PlayerId);
			if (playerNetworkView != null) {
				playerNetworkView.Load (playerData);
			}
		}
	}

	public void ProcessAckReliable(AckReliableMessage ackReliableMessage) {
		Debug.Log ("Client::ProcessAckReliable " + ackReliableMessage.MessageIdToAck);
		for (int i = 0; i < outMessages.Count; i++) {
			Message message = outMessages [i];
			if (message.ReliabilityId == ackReliableMessage.MessageIdToAck) {
				outMessages.RemoveAt (i);
			}
		}
	}

	public void ProcessAckReliableSendEveryFrame(AckReliableSendEveryFrameMessage ackReliableSendEveryFrameMessage) {
		for (int i = 0; i < outMessages.Count; i++) {
			Message message = outMessages [i];
			if (message.Reliability == ReliabilityType.RELIABLE_SEND_EVERY_PACKET &&
				message.ReliabilityId <= ackReliableSendEveryFrameMessage.MessageIdToAck) {
				outMessages.RemoveAt (i);
				i--;
			}
		}
	}

	PlayerNetworkView GetPlayerWithId(int playerId) {
		for (int i = 0; i < players.Count; i++) {
			if (players[i].Id == playerId) {
				return players[i];
			}
		}
		return null;
	}
}
