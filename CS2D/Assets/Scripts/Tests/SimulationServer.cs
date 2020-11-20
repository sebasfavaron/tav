﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class SimulationServer : MonoBehaviour
{
    public int pps = 60;

    private Channel channel;
    
    [SerializeField] private GameObject serverCubePrefab;
    [SerializeField] private GameObject playerUICanvas;
    private Dictionary<int, CubeEntity> cubeEntitiesServer;
    private Dictionary<int, CubeEntity> bots;
    private List<int> portsUsed;
    private Dictionary<int, int> packetNumbers; // packetNumbers[cubePort] = cubePacketNumber
    private Dictionary<int, int> maxInputs; // maxInputs[cubePort] = cubeMaxInput

    // Start is called before the first frame update
    public void Start()
    {
        channel = new Channel(Utils.serverPort);

        cubeEntitiesServer = new Dictionary<int, CubeEntity>();
        bots = new Dictionary<int, CubeEntity>();
        portsUsed = new List<int>();
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
        
        var cube = cubeEntitiesServer[port];
        
        int prevMax = maxInputs[port], amountOfCommandsToProcess = packet.buffer.GetInt();
        for (int i = 0; i < amountOfCommandsToProcess; i++)
        {
            var command = new Commands();
            command.Deserialize(packet.buffer);

            if (command.inputNumber > maxInputs[port])
            {
                var cubeGO = cube.cubeGameObject;
                var _transform = cubeGO.transform;
                Vector3 move = _transform.forward * command.forwards + Vector3.down * Utils.gravity;
                cubeGO.GetComponent<CharacterController>().Move(move * (Utils.speed * Time.deltaTime));
                _transform.Rotate(new Vector3(0f, command.rotate * (Utils.rotateSpeed * Time.deltaTime), 0f));
                
                if (command.shoot)
                {
                    Gun gun = cube.cubeGameObject.GetComponent<Gun>();
                    if(gun != null) gun.Shoot(cube.cubeGameObject.transform);
                }

                maxInputs[port] = command.inputNumber;
            }
        }

        // send ack
        if (!cube.isBot && maxInputs[port] > prevMax) SendAck(cube.port);
    }

    private void SendAck(int port)
    {
        var packet3 = Packet.Obtain();
        packet3.buffer.PutInt((int) Utils.Ports.ACK);
        packet3.buffer.PutInt(maxInputs[port]);
        packet3.buffer.Flush();
        Utils.Send(packet3, channel, port);
    }

    private void ReceiveJoins(Packet packet, bool isBot = false)
    {
        int id = packet.buffer.GetInt();
        int port = Utils.GetPortFromId(id);
        
        
        var existingPlayer = portsUsed.Contains(port);
        if (!existingPlayer)
        {
            // if(keysOfCubesToDebug.Contains(port)) print($"client-{id} sent join");
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
        var cubeGO = Instantiate(serverCubePrefab, Utils.startPos, Quaternion.identity);
        var canvas = Instantiate(playerUICanvas, new Vector3(), Quaternion.identity);
        canvas.transform.SetParent(cubeGO.transform);
        canvas.transform.localPosition = new Vector3(0f, 2f, 0f);
        var text = canvas.GetComponentInChildren<Text>();
        if(text != null) text.text = $"{id}";
        var newCube = new CubeEntity(cubeGO, id, isBot);
        cubeGO.name = $"server-{id}";
        cubeGO.transform.SetParent(GameObject.Find("Players(Server)").transform);
        cubeEntitiesServer[newCube.port] = newCube;
        if(isBot) {
            cubeGO.name = $"server-bot-{id}";
            bots[newCube.port] = newCube; // store bots in another list to test applying movement to them
        }
        else
        {
            packetNumbers[newCube.port] = 0;
            maxInputs[newCube.port] = 0;
        }
        
        foreach (var cube in cubeEntitiesServer.Values)
        {
            // send player joined packet to everyone (including to the new player as a confirmation)
            Packet playerJoinedPacket = Packet.Obtain();
            var buffer = playerJoinedPacket.buffer;
            buffer.PutInt((int) Utils.Ports.PLAYER_JOINED);
            buffer.PutInt(id);
            buffer.PutInt(cubeEntitiesServer.Count-1);
            print($"send to client-{cube.id} {cubeEntitiesServer.Count-1} friends");
            foreach (var sendCube in cubeEntitiesServer.Values)
            {
                if(sendCube.port == cube.port) continue;  // dont send the player to himself
                
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
            var command = new Commands(0, 1, 0.2f, false);
        
            var packet = Packet.Obtain();
            packet.buffer.PutInt((int) Utils.Ports.INPUT);
            packet.buffer.PutInt(kv.Key);
            packet.buffer.PutInt(1);
            command.Serialize(packet.buffer);
            packet.buffer.Flush();

            ReceiveInputs(packet);
        }
    }
    
    public void TakeDamage(string cubeName, float damage)
    {
        var matchingCube = cubeEntitiesServer.Values.FirstOrDefault(c => c.cubeGameObject.transform.name.Equals(cubeName));

        if (matchingCube != null)
        {
            var target = matchingCube.cubeGameObject.GetComponent<Target>();
            if (target != null)
            {
                if (target.TakeDamage(damage) <= 0f)
                {
                    foreach (var kv in cubeEntitiesServer)
                    {
                        var packet = Packet.Obtain();
                        packet.buffer.PutInt((int) Utils.Ports.PLAYER_DIED);
                        print($"sending dead port {matchingCube.port} to {kv.Value.port}"); // 
                        packet.buffer.PutInt(matchingCube.port);
                        packet.buffer.Flush();
                        Utils.Send(packet, channel, kv.Value.port);
                    }
                
                    // Destroy and remove from Dictionary
                    Destroy(matchingCube.cubeGameObject);
                    cubeEntitiesServer.Remove(matchingCube.port);
                    print($"killed {cubeName}");
                }
            }
        }
    }
    
    private void OnDestroy() {
        channel.Disconnect();
    }
}
