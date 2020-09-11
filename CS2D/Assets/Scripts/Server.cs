using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;

public class Server : MonoBehaviour {

	[Header("Connection")]
	public int serverPort;
	public int clientPort;
	public float fakeDelay;
	public float fakePacketLoss;
	public float snapshotSendRate;

	private Channel channel;
	private float lastSnapshotSentTime;

	[Header("Game")]
	public Object playerPrefab;
	List<Player> players = new List<Player>();

	private static Server instance = null;
	public static Server Instance {
		get {
			return instance;
		}	
	}

	void Awake() {
		instance = this;
	}

	void Start() {
		channel = new Channel(null, serverPort, clientPort);
		lastSnapshotSentTime = Time.realtimeSinceStartup;
	}

	void OnDestroy() {
		channel.Disconnect();
		instance = null;
	}

	void Update() {
		Packet inPacket = channel.GetPacket ();
		if (inPacket != null) {
			//read it!
			BitBuffer bitBuffer = inPacket.buffer;
			int messageCount = bitBuffer.GetInt ();
			for (int i = 0; i < messageCount; i++) {
				//parse message
				Message clientMessage = ReadClientMessage(bitBuffer, inPacket.fromEndPoint);
				if (clientMessage != null) {
					if (clientMessage.Type == MessageType.CONNECT_PLAYER) {
						ProcessConnectPlayer (clientMessage as ConnectPlayerMessage);
					} else if (clientMessage.Type == MessageType.DISCONNECT_PLAYER) {
						ProcessDisconnectPlayer (clientMessage as DisconnectPlayerMessage);
					} else {
						Player player = GetPlayerWithEndPoint(inPacket.fromEndPoint);
						if (player != null) {
							player.CommunicationManager.ReceiveMessage (clientMessage);
						} 
					}
				}
			}
		}

		float currentTime = Time.realtimeSinceStartup;
		float timeToSendSnapshot = 1.0f / snapshotSendRate;
		if (currentTime - lastSnapshotSentTime >= timeToSendSnapshot) {
			SnapshotMessage snapshotMessage = new SnapshotMessage (-1, BuildGameData ());
			snapshotMessage.TimeToSend = fakeDelay;
			for (int i = 0; i < players.Count; i++) {
				players[i].CommunicationManager.SendMessage(snapshotMessage);
			}
			lastSnapshotSentTime = currentTime;
		}

		for (int playerIdx = 0; playerIdx < players.Count; playerIdx++) {
			Player player = players[playerIdx];

			Packet p = player.CommunicationManager.BuildPacket ();

			if (p != null) {
				bool shouldDropPacket = Random.Range (0.0001f, 100.0f) < fakePacketLoss;
				if (!shouldDropPacket) {
					channel.Send (p, player.endPoint);
				}
			}
		}
	}

	Message ReadClientMessage(BitBuffer bitBuffer, IPEndPoint clientEndPoint) {
		MessageType messageType = bitBuffer.GetEnum<MessageType> ((int)MessageType.TOTAL);
		Message clientMessage = null;
		switch (messageType) {
		case MessageType.CONNECT_PLAYER:
			clientMessage = ConnectPlayerMessage.CreateConnectPlayerMessageToReceive (clientEndPoint);
			break;
		case MessageType.DISCONNECT_PLAYER:							
			clientMessage = new DisconnectPlayerMessage ();
			break;
		case MessageType.PLAYER_INPUT:
			clientMessage = new PlayerInputMessage ();
			break;
		case MessageType.ACK_RELIABLE_MAX_WAIT_TIME:
			clientMessage = AckReliableMessage.CreateAckReliableMessageMessageToReceive ();			
			break;
		case MessageType.ACK_RELIABLE_SEND_EVERY_PACKET:
			clientMessage = AckReliableSendEveryFrameMessage.CreateAckReliableSendEveryFrameMessageMessageToReceive ();
			break;
		default:
			Debug.LogError("Got a client message that cannot be understood");
			return null;
		}
		clientMessage.From = clientEndPoint;
		clientMessage.Load(bitBuffer);

		return clientMessage;
	}

	public void ProcessConnectPlayer(ConnectPlayerMessage connectPlayerMessage) {
		int playerId = connectPlayerMessage.PlayerId;
		Player player = GetPlayerWithId (playerId);
		if (player != null) {
			DisconnectPlayer (player);
		}

		GameObject playerGO = Instantiate (playerPrefab) as GameObject;
		playerGO.name = "Player " + playerId; 
		player = playerGO.GetComponent<Player> ();
		player.endPoint = connectPlayerMessage.EndPoint;
		player.Id = playerId;
		players.Add(player);

		//send all players that a new player has connected
		for (int i = 0; i < players.Count; i++) {
			Player playerToSendTo = players [i];
			PlayerConnectedMessage playerConnectedMessage = PlayerConnectedMessage.CreatePlayerConnectedMessageToSend (playerToSendTo, playerId);
			playerToSendTo.CommunicationManager.SendMessage (playerConnectedMessage);
		}

		//send connecting player a message that the other players have connected so it can create the entity
		for (int i = 0; i < players.Count; i++) {
			Player playerAlreadyConnected = players [i];
			if (playerAlreadyConnected != player) {
				PlayerConnectedMessage playerConnectedMessage = PlayerConnectedMessage.CreatePlayerConnectedMessageToSend (playerAlreadyConnected, playerAlreadyConnected.Id);
				player.CommunicationManager.SendMessage (playerConnectedMessage);
			}
		}
	}	

	void ProcessDisconnectPlayer(DisconnectPlayerMessage disconnectPlayerMessage) {
		Player player = GetPlayerWithEndPoint(disconnectPlayerMessage.From);
		if (player != null) {
			DisconnectPlayer (player);
			for (int i = 0; i < players.Count; i++) {
				Player playerToSendTo = players [i];
				PlayerDisconnectedMessage playerDisconnectedMessage = PlayerDisconnectedMessage.CreatePlayerDisconnectedMessageToSend (playerToSendTo, player.Id);
				playerToSendTo.CommunicationManager.SendMessage(playerDisconnectedMessage);
			}
		}					
	}

	void DisconnectPlayer(Player player) {
		Destroy(player.gameObject);
		players.Remove(player);
	}

	Player GetPlayerWithId(int playerId) {
		for (int i = 0; i < players.Count; i++) {
			if (players[i].Id == playerId) {
				return players[i];
			}
		}
		return null;
	}

	public Player GetPlayerWithEndPoint(IPEndPoint endPoint) {
		for (int i = 0; i < players.Count; i++) {
			if (players[i].endPoint.Equals(endPoint)) {
				return players[i];
			}
		}
		return null;
	}

	private GameData BuildGameData() {
		GameData gameData = new GameData ();
		gameData.Time = Time.realtimeSinceStartup;
		for (int i = 0; i < players.Count; i++) {
			gameData.Players.Add (players [i].BuildPlayerData ());
		}
		return gameData;
	}
}
