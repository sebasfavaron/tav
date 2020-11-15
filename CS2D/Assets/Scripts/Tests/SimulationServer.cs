using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimulationServer : MonoBehaviour
{
    private float accum = 0f;
    private int packetNumber = 0;
    public int pps = 100;
    private int maxInput = 0;

    private Channel channel;
    
    [SerializeField] private GameObject serverCubePrefab;
    private Dictionary<int, CubeEntity> cubeEntitiesServer;
    private Dictionary<int, CubeEntity> bots;
    private List<int> portsUsed;
    private CharacterController characterController;
    private Dictionary<int, int> sendPorts;


    // Start is called before the first frame update
    public void Start()
    {
        channel = new Channel(Utils.serverPort);

        cubeEntitiesServer = new Dictionary<int, CubeEntity>();
        bots = new Dictionary<int, CubeEntity>();
        portsUsed = new List<int>();
        characterController = GetComponent<CharacterController>();
        InvokeRepeating(nameof(BotRandomMove), 1f, 0.2f);
    }

    private void FixedUpdate()
    {
        SendData();
    }

    // Update is called once per frame
    public void Update()
    {

        if (Input.GetKeyDown(KeyCode.B))
        {
            for (int i = 0; i < 1; i++)
            {
                BotJoin();
            }
        }

        Packet packet;
        while ((packet = channel.GetPacket()) != null)
        {
            int type = packet.buffer.GetInt();
            switch (type)
            {
                case (int) Utils.Ports.INPUT:
                    ReceiveInputs(packet);
                    break;
                case (int) Utils.Ports.JOIN:
                    ReceiveJoins(packet);
                    break;
            }
        }
    }

    private void ReceiveInputs(Packet packet)
    {
        int id = packet.buffer.GetInt();
        if (!cubeEntitiesServer.ContainsKey(Utils.GetPortFromId(id))) return;
        
        var cube = cubeEntitiesServer[Utils.GetPortFromId(id)];
        
        int max = 0, amountOfCommandsToProcess = packet.buffer.GetInt();
        for (int i = 0; i < amountOfCommandsToProcess; i++)
        {
            var command = new Commands();
            command.Deserialize(packet.buffer);

            if (command.inputNumber > maxInput)
            {
                Vector3 move = cube.cubeGameObject.transform.forward * command.moveVector.z + 
                               cube.cubeGameObject.transform.right * command.moveVector.x + command.moveVector + Vector3.down * Utils.gravity;
                cube.cubeGameObject.GetComponent<CharacterController>().Move(move * (Utils.speed * Time.fixedDeltaTime));
                
                max = Mathf.Max(command.inputNumber, max);
            }
        }
        // cube.packetNumber = max;

        // send ack
        if (!cube.isBot) SendAck(max, cube.port);
    }

    private void SendAck(int max, int port)
    {
        var packet3 = Packet.Obtain();
        packet3.buffer.PutInt((int) Utils.Ports.ACK);
        packet3.buffer.PutInt(max);
        // var sendCube = new CubeEntity(Vector3.zero, Quaternion.identity, serverCubePrefab);
        // sendCube.Serialize(packet3.buffer);
        packet3.buffer.Flush();
        Utils.Send(packet3, channel, port);
        maxInput = Mathf.Max(max, maxInput);
    }

    private void ReceiveJoins(Packet packet, bool isBot = false)
    {
        int id = packet.buffer.GetInt();
        int port = Utils.GetPortFromId(id);
        var existingPlayer = portsUsed.Contains(port);
        if (!existingPlayer)
        {
            PlayerJoined(id, isBot);
            portsUsed.Add(port);
        }
        else
        {
            print("Existing id (or diff id that would receive the same port) trying to join. Ignoring");
        }
    }
    
    private void SendData()
    {
        if (cubeEntitiesServer.Count == 0) return;
        
        // accum += Time.deltaTime;
        // float sendRate = (1f / pps);
        // if (accum >= sendRate)
        // {
        //     // Restart accum
        //     accum -= sendRate;
        // }
        packetNumber++; // TODO: no sirve de nada, el packetNumber es uno por cliente. Por ahora anda porque estoy probando con un solo cliente
            
        foreach (var kv in cubeEntitiesServer)
        {
            var cube = kv.Value;
            if(cube.isBot) continue;
            
            // serialize
            var packet = Packet.Obtain();
            packet.buffer.PutInt((int) Utils.Ports.DATA);
            var snapshot = new Snapshot(cubeEntitiesServer, packetNumber); // TODO: hacer esto custom por cada cliente (maxinput, packetnumber)
            snapshot.Serialize(packet.buffer);
            packet.buffer.Flush();
            Utils.Send(packet, channel, cube.port);
        }
    }
    
    private void PlayerJoined(int id, bool isBot = false)  // Server
    {
        // send player joined packet to everyone (including to the new player as a confirmation)
        Packet playerJoinedPacket = Packet.Obtain();
        playerJoinedPacket.buffer.PutInt((int) Utils.Ports.PLAYER_JOINED);
        playerJoinedPacket.buffer.PutInt(id);
        playerJoinedPacket.buffer.Flush();
        
        // init new player
        var serverCubeGO = Instantiate(serverCubePrefab, new Vector3(0f, 3f, 0f), Quaternion.identity);
        var serverCube = new CubeEntity(serverCubeGO, id);
        serverCubeGO.name = $"server-{id}";
        cubeEntitiesServer[serverCube.port] = serverCube;
        if(isBot) {
            serverCubeGO.name = $"server-bot-{id}";
            bots[serverCube.port] = serverCube; // store bots in another list to test applying movement to them
            print($"adding bot {serverCube.id}-{serverCube.port}");
        }
        
        foreach (var cube in cubeEntitiesServer.Values) // send it to the new player and the rest
        {
            if(!cube.isBot) Utils.Send(playerJoinedPacket, channel, cube.port);
        }
    }

    private void BotJoin() // Same as client's Join()
    {
        var joinPacket = Packet.Obtain();
        var id = Random.Range(0, 1000000);
        print(id);
        
        // send id
        joinPacket.buffer.PutInt(id);
        joinPacket.buffer.Flush();
        ReceiveJoins(joinPacket, true);
        joinPacket.Free();
    }

    private void BotRandomMove()
    {
        foreach (var kv in bots)
        {
            var moveVector = new Vector3(100, 10, 10);
            var command = new Commands(0, moveVector, 0);
        
            var packet = Packet.Obtain();
            packet.buffer.PutInt((int) Utils.Ports.INPUT);
            packet.buffer.PutInt(kv.Key);
            packet.buffer.PutInt(1);
            command.Serialize(packet.buffer);
            packet.buffer.Flush();

            ReceiveInputs(packet);
        }
    }
    
    private void OnDestroy() {
        channel.Disconnect();
    }
}
