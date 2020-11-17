﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimulationServer : MonoBehaviour
{
    private float accum = 0f;
    public int pps = 100;
    private bool oneClientConnected = false;

    private Channel channel;
    
    [SerializeField] private GameObject serverCubePrefab;
    private Dictionary<int, CubeEntity> cubeEntitiesServer;
    private Dictionary<int, CubeEntity> bots;
    private List<int> portsUsed;
    private CharacterController characterController;
    private Dictionary<int, int> packetNumbers; // packetNumbers[cubePort] = cubePacketNumber
    private Dictionary<int, int> maxInputs; // maxInputs[cubePort] = cubeMaxInput
    private List<int> keysOfCubesToDebug;

    // Start is called before the first frame update
    public void Start()
    {
        channel = new Channel(Utils.serverPort);

        cubeEntitiesServer = new Dictionary<int, CubeEntity>();
        bots = new Dictionary<int, CubeEntity>();
        portsUsed = new List<int>();
        keysOfCubesToDebug = new List<int>();
        characterController = GetComponent<CharacterController>();
        packetNumbers = new Dictionary<int, int>();
        maxInputs = new Dictionary<int, int>();
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
        int port = Utils.GetPortFromId(id);
        if (!cubeEntitiesServer.ContainsKey(port)) return;
        
        if(keysOfCubesToDebug.Contains(port)) print($"client-{id} sent inputs");
        var cube = cubeEntitiesServer[port];
        
        int max = 0, amountOfCommandsToProcess = packet.buffer.GetInt();
        for (int i = 0; i < amountOfCommandsToProcess; i++)
        {
            var command = new Commands();
            command.Deserialize(packet.buffer);

            if (command.inputNumber > maxInputs[port])
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
        maxInputs[port] = Mathf.Max(max, maxInputs[port]);
    }

    private void ReceiveJoins(Packet packet, bool isBot = false)
    {
        int id = packet.buffer.GetInt();
        int port = Utils.GetPortFromId(id);
        
        
        var existingPlayer = portsUsed.Contains(port);
        if (!existingPlayer)
        {
            if(keysOfCubesToDebug.Contains(port)) print($"client-{id} sent join");
            PlayerJoined(id, isBot);
            portsUsed.Add(port);
        }
        else
        {
            // print($"Existing id (or diff id that would receive the same port) trying to join. Ignoring id {id}, port {port}");
        }
    }
    
    private void SendData()
    {
        if (cubeEntitiesServer.Count == 0) return;
        
            
        foreach (var kv in cubeEntitiesServer)
        {
            var cube = kv.Value;
            if(cube.isBot) continue;

            packetNumbers[cube.port] = packetNumbers[cube.port] + 1;
            
            // serialize
            var packet = Packet.Obtain();
            packet.buffer.PutInt((int) Utils.Ports.DATA);
            var snapshot = new Snapshot(cubeEntitiesServer, packetNumbers[cube.port]);
            snapshot.Serialize(packet.buffer);
            packet.buffer.Flush();
            Utils.Send(packet, channel, cube.port);
        }
    }
    
    private void PlayerJoined(int id, bool isBot = false)  // Server
    {
        // init new player
        var serverCubeGO = Instantiate(serverCubePrefab, Utils.startPos, Quaternion.identity);
        var serverCube = new CubeEntity(serverCubeGO, id, isBot);
        serverCubeGO.name = $"server-{id}";
        serverCubeGO.transform.SetParent(GameObject.Find("Players(Server)").transform);
        cubeEntitiesServer[serverCube.port] = serverCube;
        if(isBot) {
            serverCubeGO.name = $"server-bot-{id}";
            bots[serverCube.port] = serverCube; // store bots in another list to test applying movement to them
        }
        else
        {
            packetNumbers[serverCube.port] = 0;
            maxInputs[serverCube.port] = 0;
        }

        if (oneClientConnected)
        {
            keysOfCubesToDebug.Add(serverCube.port);
        }
        oneClientConnected = true;
        
        foreach (var cube in cubeEntitiesServer.Values)
        {
            // send player joined packet to everyone (including to the new player as a confirmation)
            Packet playerJoinedPacket = Packet.Obtain();
            var buffer = playerJoinedPacket.buffer;
            // buffer.PutInt((int) Utils.Ports.PLAYER_JOINED);
            // buffer.PutInt(id);
            // buffer.PutInt(cubeEntitiesServer.Count);
            // foreach (var sendCube in cubeEntitiesServer.Values)
            // {
            //     buffer.PutInt(sendCube.id);
            //     sendCube.Serialize(buffer);
            // }
            // buffer.Flush();
            //
            // var receivedId = playerJoinedPacket.buffer.GetInt();
            // int previousCubesAmount = packet.buffer.GetInt();
            // for (int i = 0; i < previousCubesAmount; i++)
            // {
            //     print($"cube {i} id: {packet.buffer.GetInt()}");
            //     cubeEntitiesClient[newCube.port] = newCube;
            // }
            //
            //
            // playerJoinedPacket = Packet.Obtain();
            // buffer = playerJoinedPacket.buffer;
            buffer.PutInt((int) Utils.Ports.PLAYER_JOINED);
            buffer.PutInt(id);
            buffer.PutInt(cubeEntitiesServer.Count);
            print($"send to client-{cube.id} {cubeEntitiesServer.Count} friends");
            foreach (var sendCube in cubeEntitiesServer.Values)
            {
                print($"friend with id {sendCube.id}");
                buffer.PutInt(sendCube.id);
                sendCube.Serialize(buffer);
            }
            buffer.Flush();
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
