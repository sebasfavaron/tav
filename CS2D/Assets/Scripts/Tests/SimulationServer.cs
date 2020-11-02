using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimulationServer : MonoBehaviour
{
    private float accum = 0f;
    private int packetNumber = 0;
    public int pps = 100;
    private int maxInput = 0;

    private FakeChannel fakeChannel;
    
    [SerializeField] private GameObject serverCubePrefab;
    private List<CubeEntity> cubeEntitiesServer;
    private List<int> randomNumbersReceived;
    private CharacterController characterController;


    // Start is called before the first frame update
    public void Start()
    {
        fakeChannel = FakeChannel.Instance;
        fakeChannel.InitPorts(9000);  // reserved port for initial joins and playerJoins

        cubeEntitiesServer = new List<CubeEntity>();
        randomNumbersReceived = new List<int>();
        characterController = GetComponent<CharacterController>();
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
            for (int i = 0; i < 10; i++)
            {
                BotJoin();
            }
        }

        Packet packet;
        foreach (var cubeEntity in cubeEntitiesServer)
        {
            while ((packet = fakeChannel.GetPacket((int) cubeEntity.port)) != null)
            {
                int type = packet.buffer.GetInt();
                switch (type)
                {
                    case (int) Utils.Ports.INPUT:
                        ReceiveInputs(packet, cubeEntity);
                        break;
                    default:
                        print($"BAD message type on port {cubeEntity.port}");
                        break;
                }
                packet.Free();
            }
        }
        
        if ((packet = fakeChannel.GetPacket(9000)) != null)
        {
            int type = packet.buffer.GetInt();
            int other = packet.buffer.GetInt();
            int other2 = packet.buffer.GetInt();
            print($"receiving on port 9000 type {type} and other {other} {other2} (should be {(int) Utils.Ports.JOIN})");
            if (type == (int) Utils.Ports.JOIN)
            {
                // receive joins from new players (from port 9000 because they dont have a unique port yet)
                ReceiveJoins(packet);
            }
            else
            {
                print("BAD message type on port 9000");
            }
            packet.Free();
        }
    }

    private void ReceiveInputs(Packet packet, CubeEntity cube)
    {
        int max = 0, amountOfCommandsToProcess = packet.buffer.GetInt();
        for (int i = 0; i < amountOfCommandsToProcess; i++)
        {
            var command = new Commands();
            command.Deserialize(packet.buffer);

            if (command.inputNumber <= maxInput)
            {
                // print(command.inputNumber);
                // TODO: fix double sending of command in ack instead of this ugly "fix"
                return;
            }
            cube.cubeGameObject.GetComponent<CharacterController>().Move(command.moveVector * (10 * Time.fixedDeltaTime));

            max = Mathf.Max(command.inputNumber, max);
        }

        // send ack
        var packet3 = Packet.Obtain();
        packet3.buffer.PutInt((int) Utils.Ports.ACK);
        packet3.buffer.PutInt(max);
        // var sendCube = new CubeEntity(Vector3.zero, Quaternion.identity, serverCubePrefab);
        // sendCube.Serialize(packet3.buffer);
        packet3.buffer.Flush();
        fakeChannel.Send((int) cube.port, packet3);
        maxInput = Mathf.Max(max, maxInput);
    }
    
    private void ReceiveJoins(Packet packet)
    {
        var rndNumber = packet.buffer.GetInt();
        var existingPlayer = randomNumbersReceived.Contains(rndNumber);
        if (!existingPlayer)
        {
            PlayerJoined(rndNumber);
            randomNumbersReceived.Add(rndNumber);    
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
        packetNumber++; // TODO: no sirve de nada, el packetNumber es uno por cliente
            
        
        foreach (var cube in cubeEntitiesServer)
        {
            // serialize
            var snapshot = new Snapshot(packetNumber, maxInput, cubeEntitiesServer); // TODO: hacer esto custom por cada cliente (maxinput, packetnumber)
            var packet = Packet.Obtain();
            packet.buffer.PutInt((int) Utils.Ports.DATA);
            snapshot.Serialize(packet.buffer);
            packet.buffer.Flush();
            fakeChannel.Send((int) cube.port, packet);
            packet.Free();
        }
    }
    
    private void PlayerJoined(int rndNumber)  // Server
    {
        if (cubeEntitiesServer.Count == 1000) // Not an actual limitation for FakeChannel
        {
            print($"Reached server limit (for port limitations). Not adding player with random number {rndNumber}");
            return;
        }
        
        var id = cubeEntitiesServer.Count + 1;

        // send player joined packet to everyone (including to the new player as a confirmation)
        Packet playerJoinedPacket = Packet.Obtain();
        playerJoinedPacket.buffer.PutInt((int) Utils.Ports.PLAYER_JOINED);
        playerJoinedPacket.buffer.PutInt(rndNumber);
        playerJoinedPacket.buffer.PutInt(id);
        playerJoinedPacket.buffer.Flush();
        print($"server sending {(int) Utils.Ports.PLAYER_JOINED}, {rndNumber}, {id}");
        fakeChannel.Send(9000, playerJoinedPacket); // send it to new player
        foreach (var cube in cubeEntitiesServer) // send it to the rest
        {
            fakeChannel.Send((int) cube.port, playerJoinedPacket);
        }
        
        // init new player
        var serverCubeGO = Instantiate(serverCubePrefab, new Vector3(), Quaternion.identity);
        var serverCube = new CubeEntity(serverCubeGO, id);
        serverCube.port = 9000 + id; // TODO: could missmatch with client port. Maybe send it
        cubeEntitiesServer.Add(serverCube);
        fakeChannel.InitPorts((int) serverCube.port); // init ports for new player
    }
    
    private int GetPort(int cubeID)
    {
        return fakeChannel.GetPort(cubeID);
    }

    private void BotJoin() // Same as client's Join()
    {
        var joinPacket = Packet.Obtain();

        // send id
        joinPacket.buffer.PutInt((int) Utils.Ports.JOIN);
        joinPacket.buffer.PutInt(Random.Range(1,1000000));
        joinPacket.buffer.Flush();
        fakeChannel.Send(9000, joinPacket);
    }
}
