using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimulationClient : MonoBehaviour
{
    private float accum = 0f;
    public int pps = 60;
    private float accumReconciliate = 0;    private float joinCooldown = 0;
    public float rps = 1; // reconciliates per second
    private int packetNumber = 0;
    private int inputNumber = 0;
    private float clientTime = 0f;
    public int requiredSnapshots = 3;
    private bool clientPlaying = false;
    private bool connected = false;
    private bool tempDisconnect = false;

    private FakeChannel fakeChannel;

    [SerializeField] private GameObject clientCubePrefab;
    private CharacterController characterController;
    private List<CubeEntity> cubeEntitiesClient;

    List<Snapshot> interpolationBuffer = new List<Snapshot>();
    List<Commands> commands = new List<Commands>();
    List<Commands> reconciliateCommands = new List<Commands>();
    private int clientId;
    private CubeEntity clientCube = null;
    private CharacterController clientCubeCharController = null;

    private CubeEntity clientInServer;

    // Start is called before the first frame update
    public void Start()
    {
        fakeChannel = FakeChannel.Instance;
        cubeEntitiesClient = new List<CubeEntity>();
        clientId = Random.Range(0, 1000000);
        characterController = GetComponent<CharacterController>();
    }

    private void FixedUpdate()
    {
        if (!connected) return;

        // Send input
        float sendRate = (1f / pps);
        accum += Time.deltaTime;
        if (accum >= sendRate)
        {
            StoreAndApplyInput();
            var packet = Packet.Obtain();
            packet.buffer.PutInt(commands.Count);
            foreach (var command in commands)
            {
                // print("----------------");
                // print(command.inputNumber);
                command.Serialize(packet.buffer);
            }
            packet.buffer.Flush();

            fakeChannel.Send(GetPort(clientId), packet, FakeChannel.ChannelType.INPUT);

            accum -= sendRate;
        }
        
        // reconciliate (client)
        float reconciliateRate = (1f / rps);
        accumReconciliate += Time.deltaTime;
        if (accumReconciliate >= reconciliateRate)
        {
            Reconciliate();
            accumReconciliate -= reconciliateRate;
        }

        // interpolate (others)
        clientPlaying = (interpolationBuffer.Count >= requiredSnapshots && interpolationBuffer.Count > 1);
        while (clientPlaying) {
            if(!Interpolate()) break;
            clientPlaying = (interpolationBuffer.Count >= requiredSnapshots && interpolationBuffer.Count > 1);
        }
    }

    // Update is called once per frame
    public void Update()
    {
        clientTime += Time.deltaTime;
        if (joinCooldown <= 0)
        {
            // Check this every now and then for new players joining
            PlayerJoined();
            if (!connected)
            {
                Join();
            }

            joinCooldown = 2f;
        }
        else
        {
            joinCooldown -= Time.deltaTime;
        }
        if (!connected || tempDisconnect) //TODO: code connect/reconnect as an input, blocking messages from/to the server
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                tempDisconnect = false;
                //fakeChannel.Connect(FakeChannel.ChannelType.INPUT);
                //fakeChannel.Connect(FakeChannel.ChannelType.ACK);
            }
            else
            {
                // Exit early if i'm disconnected
                return;
            }
        }

        // Delete acknowledged commands from list
        Packet packet3;
        while ( (packet3=fakeChannel.GetPacket(GetPort(clientId), FakeChannel.ChannelType.ACK)) != null)
        {
            var toDel = packet3.buffer.GetInt();
            // clientInServer = new CubeEntity(Vector3.zero, Quaternion.identity, clientCubePrefab);
            // clientInServer.Deserialize(packet3.buffer);
            while (commands.Count > 0 && commands[0].inputNumber <= toDel)
            {
                commands.RemoveAt(0);
            }
        }

        /*while(commands.Count > 0 && commands[0].timestamp < Time.time)
        {
            commands.RemoveAt(0);
        }*/

        // Receive data
        var packet = fakeChannel.GetPacket(GetPort(clientId), FakeChannel.ChannelType.DATA);
        if (packet != null)
        {
            var snapshot = new Snapshot(-1, -1, cubeEntitiesClient);
            var buffer = packet.buffer;

            snapshot.Deserialize(buffer);

            int size = interpolationBuffer.Count;
            if(size == 0 || snapshot.packetNumber > interpolationBuffer[size - 1].packetNumber) {
                interpolationBuffer.Add(snapshot);
            }
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
        var lastServerSnapshot = interpolationBuffer[interpolationBuffer.Count - 1];
        clientInServer = lastServerSnapshot.cubeEntities.Find(c => c.id == clientId);
        
        // 1. Deactivate real client
        // 2. Instantiate fake client
        // 3. Simulate movements from 'commands' list (all commands not confirmed by server)
        // 4. Check if the resulting position is the same as the real client's position (with a delta for error). If false, tp real client to fake client's position

        // 1. Save client's position
        // var clientCopy = new CubeEntity(clientCube);
        
        // 2. Tp client to client's position in server
        // clientCubeRigidBody.position = clientInServer.position; // WARN: capaz tengo que hacer deep copy
        // clientCubeRigidBody.rotation = clientInServer.rotation; // WARN: capaz tengo que hacer deep copy
        clientCube.cubeGameObject.transform.position = clientInServer.position;
        clientCube.cubeGameObject.transform.rotation = clientInServer.rotation;

        // 3. Simulate movements from 'commands' list (all commands not confirmed by server)
        var toRemove = new List<Commands>();
        print("=======================================");
        reconciliateCommands.ForEach(command =>
        {
            print($"c: {command.inputNumber}, s: {lastServerSnapshot.inputNumber}, apply: {command.inputNumber >= lastServerSnapshot.inputNumber}");
            if (command.inputNumber >= lastServerSnapshot.inputNumber)
            {
                // print("Applying");
                // TODO: investigar desfazaje entre inputNumber del snapshot y reconciliateCommands (podria ser la razon por la que salta tanto para atras el cliente y no vuelve al "presente")
                Apply(command);
                toRemove.Add(command);
            }
        });
        toRemove.ForEach(c => reconciliateCommands.Remove(c));
        
        // 4. Check if the resulting position is the same as the saved client's position (with a delta for error). If true, tp client to saved client's position (or simply don't check)
        // not checking for now..
    }

    private void StoreAndApplyInput()
    {
        var timeout = Time.time + 1;
        inputNumber += 1;
        var moveVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        var command = new Commands(inputNumber, moveVector, timeout);
        // var command = new Commands(inputNumber, Input.GetKeyDown(KeyCode.W), Input.GetKeyDown(KeyCode.S),
        //     Input.GetKeyDown(KeyCode.A), Input.GetKeyDown(KeyCode.D),
        //     Input.GetKeyDown(KeyCode.Space), timeout);
        commands.Add(command);
        reconciliateCommands.Add(command);

        if (Input.GetKeyDown(KeyCode.X))
        {
            tempDisconnect = true;
            //fakeChannel.Disconnect(FakeChannel.ChannelType.INPUT);
            //fakeChannel.Disconnect(FakeChannel.ChannelType.ACK);
        }
        
        Apply(command);
    }
    
    private void Apply(Commands command){
        if(clientCubeCharController == null)
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

        clientCubeCharController.Move(command.moveVector * (10 * Time.fixedDeltaTime));
    }

    private void Join()
    {
        var joinPacket = Packet.Obtain();

        // send random number to validate future PlayerJoined packet
        joinPacket.buffer.PutInt(clientId);  // Not the actual clientId, just a way to know if the future PlayerJoined packet is yours
        joinPacket.buffer.Flush();
        fakeChannel.Send(9000, joinPacket, FakeChannel.ChannelType.JOIN);
    }

    private void PlayerJoined()
    {
        // wait for confirmation
        Packet playerJoinedPacket;
        int port = connected ? GetPort(clientId) : 9000; // until connected, the only port you can hear in is 9000, where you wait for your playerJoined confirmation
        while ((playerJoinedPacket = fakeChannel.GetPacket(port, FakeChannel.ChannelType.PLAYER_JOINED)) != null)
        {
            var randomNumber = playerJoinedPacket.buffer.GetInt();
            var receivedId = playerJoinedPacket.buffer.GetInt();

            var cubeGO = Instantiate(clientCubePrefab, new Vector3(), Quaternion.identity);
            var cube = new CubeEntity(cubeGO, receivedId);
            cubeEntitiesClient.Add(cube);

            if (randomNumber == clientId)
            {
                clientId = receivedId;  // Now clientId is real
                clientCube = cube;
                clientCubeCharController = clientCube.cubeGameObject.GetComponent<CharacterController>();
                connected = true;
            }
        }
    }
    
    private int GetPort(int cubeID)
    {
        return fakeChannel.GetPort(cubeID);
    }
    
    private void SendPacket(Packet packet, Channel sendChannel)
    {
        string serverIP = "127.0.0.1";
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), sendChannel.port);
        sendChannel.Send(packet, remoteEp);
        packet.Free();
    }

    private bool Interpolate()
    {
        var previousTime = interpolationBuffer[0].packetNumber * (1f/pps);
        var nextTime =  interpolationBuffer[1].packetNumber * (1f/pps);
        var t =  (clientTime - previousTime) / (nextTime - previousTime);
        var interpolatedSnapshot = Snapshot.CreateInterpolated(interpolationBuffer[0], interpolationBuffer[1], t);
        interpolatedSnapshot.Apply(clientId);
        
        if(clientTime > nextTime) {
            interpolationBuffer.RemoveAt(0);
            return true;
        }

        return false;
    }
}
