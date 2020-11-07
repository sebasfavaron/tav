using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using Debug = System.Diagnostics.Debug;
using Random = UnityEngine.Random;

public class SimulationClient : MonoBehaviour
{
    private float accum = 0f;
    public int pps = 60;
    private float accumReconciliate = 0;
    private float joinCooldown = 0;
    public float rps = 1; // reconciliates per second
    private int packetNumber = 0;
    private int inputNumber = 0;
    private float clientTime = 0f;
    public int requiredSnapshots = 3;
    private bool clientPlaying = false;
    private bool connected = false;
    private bool tempDisconnect = false;
    private readonly int serverPort = Utils.serverPort;

    private Channel channel;

    [SerializeField] private GameObject clientCubePrefab;
    [SerializeField] private GameObject clientReconciliateCubePrefab;
    private Dictionary<int, CubeEntity> cubeEntitiesClient;

    List<Snapshot> interpolationBuffer = new List<Snapshot>();
    List<Commands> commands = new List<Commands>();
    List<Commands> reconciliateCommands = new List<Commands>();
    private int clientId;
    private CubeEntity clientCube = null;
    private CharacterController clientCharacterController = null;
    private CubeEntity reconciliateClientCube;
    private CharacterController reconciliateCharacterController;

    // Start is called before the first frame update
    public void Start()
    {
        cubeEntitiesClient = new Dictionary<int, CubeEntity>();
        clientId = Random.Range(0, 1000000);
        channel = new Channel(Utils.GetPortFromId(clientId));
    }

    private void FixedUpdate()
    {
        ReadStoreApplySendInput(); // TODO: not using sendrate/pps?
        // float sendRate = (1f / pps);
        // accum += Time.deltaTime;
        // if (accum >= sendRate)
        // {
        //     StoreAndApplyInput();
        //     var packet = Packet.Obtain();
        //     packet.buffer.PutInt(commands.Count);
        //     foreach (var command in commands)
        //     {
        //         // print("----------------");
        //         // print(command.inputNumber);
        //         command.Serialize(packet.buffer);
        //     }
        //     packet.buffer.Flush();
        //
        //     fakeChannel.Send(GetPort(clientId), packet, FakeChannel.ChannelType.INPUT);
        //
        //     accum -= sendRate;
        // }
    }

    // Update is called once per frame
    public void Update()
    {
        clientTime += Time.deltaTime;
        
        while(commands.Count > 0 && commands[0].timestamp < Time.time)
        {
            commands.RemoveAt(0);
        }
        
        // if (!connected || tempDisconnect) //TODO: code connect/reconnect as an input, blocking messages from/to the server
        // {
        //     if (Input.GetKeyDown(KeyCode.C))
        //     {
        //         tempDisconnect = false;
        //         //fakeChannel.Connect(FakeChannel.ChannelType.INPUT);
        //         //fakeChannel.Connect(FakeChannel.ChannelType.ACK);
        //     }
        // }

        if (!connected)
        {
            Join(); // TODO: spam? maybe bring cooldown back for this only
        }
        
        Packet packet;
        while ((packet = channel.GetPacket()) != null)
        {
            int type = packet.buffer.GetInt();
            switch (type)
            {
                case (int) Utils.Ports.ACK:
                    AckInputs(packet);
                    break;
                case (int) Utils.Ports.DATA:
                    ReceiveData(packet);
                    break;
                case (int) Utils.Ports.PLAYER_JOINED:
                    PlayerJoined(packet);
                    break;
            }
        }

        InterpolateAndReconciliate();
    }

    private void JoinActions(Packet packet)
    {
        PlayerJoined(packet);
        if (!connected)
        {
            Join();
        }
    }

    private void AckInputs(Packet packet)
    {
        if (!connected) return;
        
        var toDel = packet.buffer.GetInt();
        // clientInServer = new CubeEntity(Vector3.zero, Quaternion.identity, clientCubePrefab);
        // clientInServer.Deserialize(packet3.buffer);
        while (commands.Count > 0 && commands[0].inputNumber <= toDel)
        {
            commands.RemoveAt(0);
        }
    }
    
    private void ReceiveData(Packet packet)
    {
        if(!connected) return;

        var snapshot = new Snapshot(cubeEntitiesClient, -1);
        snapshot.Deserialize(packet.buffer);

        int bufferSize = interpolationBuffer.Count;
        var lastBufferPacketNumber = bufferSize != 0 ? interpolationBuffer[bufferSize - 1].packetNumber : -1;

        int newPacketNumber = snapshot.packetNumber;

        // I check with client's packetNumber but they are all the same
        bool cond = (bufferSize == 0 || newPacketNumber > lastBufferPacketNumber) && bufferSize < requiredSnapshots + 1;
        if (bufferSize != 0)
        {
            print($"{newPacketNumber - lastBufferPacketNumber}, {interpolationBuffer.Count}, {lastBufferPacketNumber}");
        }
        if(cond) {
            interpolationBuffer.Add(snapshot);
        }
    }

    private void InterpolateAndReconciliate()
    {
        print($"{interpolationBuffer.Count} >= {requiredSnapshots}");
        while (interpolationBuffer.Count >= requiredSnapshots && !tempDisconnect)
        {
            Interpolate();
            Reconciliate();    
        }
    }
    
    private void Interpolate()
    {
        var previousTime = interpolationBuffer[0].packetNumber * (1f/pps);
        var nextTime =  interpolationBuffer[1].packetNumber * (1f/pps);
        var t =  (clientTime - previousTime) / (nextTime - previousTime);
        var interpolatedSnapshot = Snapshot.CreateInterpolated(interpolationBuffer[0], interpolationBuffer[1], t);
        interpolatedSnapshot.Apply(clientId);
        
        if(clientTime > nextTime) {
            interpolationBuffer.RemoveAt(0);
        }
    }

    private void Reconciliate()
    {
        var max = 0;
        // Snapshot lastServerSnapshot = null;
        // interpolationBuffer.ForEach(snapshot =>
        // {
        //     if (snapshot.inputNumber > max)
        //     {
        //         lastServerSnapshot = snapshot;
        //     }
        // });
        var clientInServer = interpolationBuffer[interpolationBuffer.Count - 1].cubeEntities[clientCube.port];
        
        // 1. Deactivate real client
        // 2. Instantiate fake client
        // 3. Simulate movements from 'commands' list (all commands not confirmed by server)
        // 4. Check if the resulting position is the same as the real client's position (with a delta for error). If false, tp real client to fake client's position

        // 1. Save client's position
        // var clientCopy = new CubeEntity(clientCube);
        
        // 2. Tp client to client's position in server
        // clientCubeRigidBody.position = clientInServer.position; // WARN: capaz tengo que hacer deep copy
        // clientCubeRigidBody.rotation = clientInServer.rotation; // WARN: capaz tengo que hacer deep copy
        reconciliateClientCube.cubeGameObject.transform.position = clientInServer.position;
        reconciliateClientCube.cubeGameObject.transform.rotation = clientInServer.rotation;

        // 3. Simulate movements from 'commands' list (all commands not confirmed by server)
        // var toRemove = new List<Commands>();
        // reconciliateCommands.ForEach(command =>
        // {
        //     print($"c: {command.inputNumber}, s: {lastServerSnapshot.inputNumber}, apply: {command.inputNumber >= lastServerSnapshot.inputNumber}");
        //     if (command.inputNumber >= lastServerSnapshot.inputNumber)
        //     {
        //         // print("Applying");
        //         // TODO: investigar desfazaje entre inputNumber del snapshot y reconciliateCommands (podria ser la razon por la que salta tanto para atras el cliente y no vuelve al "presente")
        //         Apply(command);
        //         toRemove.Add(command);
        //     }
        // });
        // toRemove.ForEach(c => reconciliateCommands.Remove(c));
        foreach (var command in commands)
        {
            Apply(command, reconciliateClientCube, reconciliateCharacterController);
        }

        // 4. Apply reconciliate position and rotation to client
        clientCube.cubeGameObject.transform.position = reconciliateClientCube.cubeGameObject.transform.position;
        clientCube.cubeGameObject.transform.rotation = reconciliateClientCube.cubeGameObject.transform.rotation;
    }

    private void ReadStoreApplySendInput()
    {
        if (clientCube?.port < 0)
        {
            return;
        }
        
        var command = ReadStoreInput();

        // Read local input (input you dont store nor send to the server)
        if (Input.GetKeyDown(KeyCode.X))
        {
            tempDisconnect = true;
            //fakeChannel.Disconnect(FakeChannel.ChannelType.INPUT);
            //fakeChannel.Disconnect(FakeChannel.ChannelType.ACK);
        }
        
        Apply(command, clientCube, clientCharacterController);
        SendInputs();
    }

    private Commands ReadStoreInput()
    {
        var timeout = Time.time + 2;
        var moveVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        var command = new Commands(inputNumber, moveVector, timeout);
        
        commands.Add(command);
        reconciliateCommands.Add(command);
        packetNumber++;
        inputNumber++;

        return command;
    }

    private void Apply(Commands command, CubeEntity client, CharacterController characterController){
        if(client == null || characterController == null)
        {
            return;
        }
        
        float gravity = Utils.gravity;
        float speed = Utils.speed;
        Vector3 move = client.cubeGameObject.transform.forward * command.moveVector.z + 
                       client.cubeGameObject.transform.right * command.moveVector.x + command.moveVector + Vector3.down * gravity;
        characterController.Move(move * (speed * Time.fixedDeltaTime));
    }
    
    private void SendInputs()
    {
        if (commands.Count != 0)
        {
            var packet = Packet.Obtain();
            packet.buffer.PutInt((int) Utils.Ports.INPUT);
            packet.buffer.PutInt(clientId);
            packet.buffer.PutInt(commands.Count);
            foreach (var command in commands)
            {
                // print("----------------");
                // print(command.inputNumber);
                command.Serialize(packet.buffer);
            }
            packet.buffer.Flush();

            if (connected && clientCube.port >= 0) Utils.Send(packet, channel, serverPort);
        }
    }

    private void Join()
    {
        var joinPacket = Packet.Obtain();

        // send random number to validate future PlayerJoined packet
        joinPacket.buffer.PutInt((int) Utils.Ports.JOIN);
        joinPacket.buffer.PutInt(clientId);
        joinPacket.buffer.Flush();
        Utils.Send(joinPacket, channel, serverPort);
        // joinPacket.Free();
    }

    private void PlayerJoined(Packet packet)
    {
        // wait for confirmation
        var receivedId = packet.buffer.GetInt();

        var cubeGO = Instantiate(clientCubePrefab, new Vector3(), Quaternion.identity);
        var cube = new CubeEntity(cubeGO, receivedId);
        cubeEntitiesClient[cube.port] = cube;

        // if player joined is this client
        if (!connected && receivedId == clientId)
        {
            clientCube = cube;
            clientCharacterController = clientCube.cubeGameObject.GetComponent<CharacterController>();
            
            var conciliateGO = Instantiate(clientReconciliateCubePrefab, new Vector3(), Quaternion.identity);
            reconciliateClientCube = new CubeEntity(conciliateGO, clientId);
            reconciliateCharacterController = reconciliateClientCube.cubeGameObject.GetComponent<CharacterController>();
            // conciliateCharacterController.transform.GetChild(1).gameObject.active = false;
            // conciliateCharacterController.transform.GetChild(0).gameObject.active = false;
            
            connected = true;
        }
    }
    
    private void OnDestroy() {
        channel.Disconnect();
    }
}
