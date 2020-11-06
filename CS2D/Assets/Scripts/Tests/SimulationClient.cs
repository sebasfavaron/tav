using System;
using System.Collections;
using System.Collections.Generic;
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
        // if (!connected) return; // TODO: creo que tengo que sacar este check, por ahi me genera el desfazaje

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
        //     else
        //     {
        //         // Exit early if i'm disconnected
        //         return; // TODO: check if this is necessary or just generating el desfazaje
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
        
        var snapshot = new Snapshot(cubeEntitiesClient);
        snapshot.Deserialize(packet.buffer);

        int size = interpolationBuffer.Count;
        var newClientCube = snapshot.cubeEntities[(int) clientCube.port];
        bool cond = (size == 0 || newClientCube.packetNumber >
                        interpolationBuffer[size - 1].cubeEntities[(int) clientCube.port].packetNumber) &&
                    size < requiredSnapshots + 1;
        if(size != 0) print($"{newClientCube.packetNumber} > {interpolationBuffer[size - 1].cubeEntities[(int) clientCube.port].packetNumber}");
        if(cond) {
            interpolationBuffer.Add(snapshot);
        }
    }

    private void InterpolateAndReconciliate()
    {
        print($"{interpolationBuffer.Count}, {requiredSnapshots}");
        while (interpolationBuffer.Count >= requiredSnapshots)
        {
            Interpolate();
            Reconciliate();    
        }
    }
    
    private void Interpolate()
    {
        var previousTime = interpolationBuffer[0].cubeEntities[(int) clientCube.port].packetNumber * (1f/pps);
        var nextTime =  interpolationBuffer[1].cubeEntities[(int) clientCube.port].packetNumber * (1f/pps);
        var t =  (clientTime - previousTime) / (nextTime - previousTime);
        var interpolatedSnapshot = Snapshot.CreateInterpolated(interpolationBuffer[0], interpolationBuffer[1], t);
        interpolatedSnapshot.Apply(clientId);
        
        if(clientTime > nextTime) {
            interpolationBuffer.RemoveAt(0);
        }
    }

    private void Reconciliate()
    {
        print("commands.Count");
        var max = 0;
        // Snapshot lastServerSnapshot = null;
        // interpolationBuffer.ForEach(snapshot =>
        // {
        //     if (snapshot.inputNumber > max)
        //     {
        //         lastServerSnapshot = snapshot;
        //     }
        // });
        var clientInServer = interpolationBuffer[interpolationBuffer.Count - 1].cubeEntities[(int) clientCube.port];
        
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
        if (clientCube?.port == null)
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
        // var command = new Commands(inputNumber, Input.GetKeyDown(KeyCode.W), Input.GetKeyDown(KeyCode.S),
        //     Input.GetKeyDown(KeyCode.A), Input.GetKeyDown(KeyCode.D),
        //     Input.GetKeyDown(KeyCode.Space), timeout);
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
        
        // Vector3 force = Vector3.zero;
        // force += command.space ? Vector3.up * (500 * Time.fixedDeltaTime) : Vector3.zero;
        // force += command.up ? Vector3.forward * (200 * Time.fixedDeltaTime) : Vector3.zero;
        // force += command.down ? Vector3.back * (200 * Time.fixedDeltaTime) : Vector3.zero;
        // force += command.left ? Vector3.left * (200 * Time.fixedDeltaTime) : Vector3.zero;
        // force += command.right ? Vector3.right * (200 * Time.fixedDeltaTime) : Vector3.zero;
        //
        // clientCubeRigidBody.AddForceAtPosition(force, Vector3.zero, ForceMode.Impulse);
        float speed = 10;
        float gravity = 9.800f;
        Vector3 move = client.cubeGameObject.transform.forward * command.moveVector.y + 
                       client.cubeGameObject.transform.right * command.moveVector.x; //command.moveVector + Vector3.down * gravity;
        characterController.Move(command.moveVector * (speed * Time.fixedDeltaTime));
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

            if (clientCube.port != null) Utils.Send(packet, channel, serverPort);
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
        cubeEntitiesClient[(int) cube.port] = cube;

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
