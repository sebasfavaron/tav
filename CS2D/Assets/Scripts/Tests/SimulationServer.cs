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
        // receive input
        Packet packet;
        foreach (var cube in cubeEntitiesServer)
        {
            int port = GetPort(cube.id);
            while ( (packet = fakeChannel.GetPacket(port, FakeChannel.ChannelType.INPUT)) != null)
            {
                int max = 0, amountOfCommandsToProcess = packet.buffer.GetInt();
                for (int i = 0; i < amountOfCommandsToProcess; i++)
                {
                    var command = new Commands();
                    command.Deserialize(packet.buffer);

                    if (command.inputNumber <= maxInput)
                    {
                        // print(command.inputNumber);
                        // TODO: fix double sending of command in ack
                        return;
                    }
                 
                    // Vector3 force = Vector3.zero;
                    // force += command.space ? Vector3.up * (500 * Time.fixedDeltaTime) : Vector3.zero;
                    // force += command.up ? Vector3.forward * (200 * Time.fixedDeltaTime) : Vector3.zero;
                    // force += command.down ? Vector3.back * (200 * Time.fixedDeltaTime) : Vector3.zero;
                    // force += command.left ? Vector3.left * (200 * Time.fixedDeltaTime) : Vector3.zero;
                    // force += command.right ? Vector3.right * (200 * Time.fixedDeltaTime) : Vector3.zero;
                    //
                    // cube.cubeGameObject.GetComponent<Rigidbody>().AddForceAtPosition(force, Vector3.zero, ForceMode.Impulse);
                    
                    cube.cubeGameObject.GetComponent<CharacterController>().Move(command.moveVector * (10 * Time.fixedDeltaTime));

                    max = Mathf.Max(command.inputNumber, max);
                }

                // send ack
                var packet3 = Packet.Obtain();
                packet3.buffer.PutInt(max);
                // var sendCube = new CubeEntity(Vector3.zero, Quaternion.identity, serverCubePrefab);
                // sendCube.Serialize(packet3.buffer);
                packet3.buffer.Flush();
                fakeChannel.Send(port, packet3, FakeChannel.ChannelType.ACK);
                maxInput = Mathf.Max(max, maxInput);
            }
        }
    }

    // Update is called once per frame
    public void Update()
    {
        accum += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.B))
        {
            for (int i = 0; i < 10; i++)
            {
                BotJoin();
            }
        }
        
        // receive joins from new players (from port 9000 because they dont have a unique port yet)
        Packet joinPacket;
        while ((joinPacket = fakeChannel.GetPacket(9000, FakeChannel.ChannelType.JOIN)) != null)
        {
            var rndNumber = joinPacket.buffer.GetInt();
            var existingPlayer = randomNumbersReceived.Contains(rndNumber);
            if (!existingPlayer)
            {
                PlayerJoined(rndNumber);
                randomNumbersReceived.Add(rndNumber);    
            }
        }

        // send data
        float sendRate = (1f / pps);
        if (accum >= sendRate)
        {
            packetNumber += 1;
            
            // serialize
            var snapshot = new Snapshot(packetNumber, maxInput, cubeEntitiesServer);
            var packet = Packet.Obtain();
            snapshot.Serialize(packet.buffer);
            packet.buffer.Flush();
            foreach (var cube in cubeEntitiesServer)
            {
                fakeChannel.Send(GetPort(cube.id), packet, FakeChannel.ChannelType.DATA);
            }
            
            // Restart accum
            accum -= sendRate;
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
        playerJoinedPacket.buffer.PutInt(rndNumber);
        playerJoinedPacket.buffer.PutInt(id);
        playerJoinedPacket.buffer.Flush();
        fakeChannel.Send(9000, playerJoinedPacket, FakeChannel.ChannelType.PLAYER_JOINED); // send it to new player
        foreach (var cube in cubeEntitiesServer) // send it to the rest
        {
            fakeChannel.Send(GetPort(cube.id), playerJoinedPacket, FakeChannel.ChannelType.PLAYER_JOINED);
        }
        
        // init new player
        int playerPort = GetPort(id);
        var serverCubeGO = Instantiate(serverCubePrefab, new Vector3(), Quaternion.identity);
        var serverCube = new CubeEntity(serverCubeGO, id);
        cubeEntitiesServer.Add(serverCube);
        fakeChannel.InitPorts(playerPort); // init ports for new player
    }
    
    private int GetPort(int cubeID)
    {
        return fakeChannel.GetPort(cubeID);
    }

    private void BotJoin() // Same as client's Join()
    {
        var joinPacket = Packet.Obtain();

        // send id
        joinPacket.buffer.PutInt(Random.Range(1,1000000));
        joinPacket.buffer.Flush();
        fakeChannel.Send(9000, joinPacket, FakeChannel.ChannelType.JOIN);
    }
}
