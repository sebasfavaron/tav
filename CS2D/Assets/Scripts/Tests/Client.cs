using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;
using Debug = System.Diagnostics.Debug;
using Random = UnityEngine.Random;

public class Client : MonoBehaviour
{
    public int pps = 60;
    public float rps = 1; // reconciliates per second
    private int inputNumber = 0;
    private float clientTime = 0f;
    public int requiredSnapshots = 3;
    private bool connected = false;
    private bool tempDisconnect = false;

    private Channel channel;

    [SerializeField] private GameObject clientCubePrefab;
    [SerializeField] private GameObject clientReconciliateCubePrefab;
    [SerializeField] private GameObject playerUICanvas;
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
        GameObject.Find("Players(Client)").transform.position = Vector3.zero;
        print($"IPAddress: {GameManager.IPAddress}");
    }

    private void FixedUpdate()
    {
        ReadStoreApplySendInput();
        InterpolateAndReconciliate();
    }

    // Update is called once per frame
    public void Update()
    {
        clientTime += Time.deltaTime;
        
        while(commands.Count > 0 && commands[0].timestamp < Time.time)
        {
            commands.RemoveAt(0);
        }

        if (!connected)
        {
            Join();
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
                case (int) Utils.Ports.PLAYER_DIED:
                    PlayerDied(packet);
                    break;
            }
        }
    }
    
    private void AckInputs(Packet packet)
    {
        if (!connected) return;
        
        var toDel = packet.buffer.GetInt();
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
        if(cond)
        {
            interpolationBuffer.Add(snapshot);
        }
    }

    private void InterpolateAndReconciliate()
    {
        if (interpolationBuffer.Count == 0) return;
        
        Reconciliate(); // for client
        while (interpolationBuffer.Count >= requiredSnapshots && !tempDisconnect)
        {
            Interpolate(); // for other players
        }
    }
    
    private void Interpolate()
    {
        var previousTime = interpolationBuffer[0].packetNumber * (1f/pps);
        var nextTime =  interpolationBuffer[1].packetNumber * (1f/pps);
        var t =  (clientTime - previousTime) / (nextTime - previousTime);
        var interpolatedSnapshot = Snapshot.CreateInterpolated(interpolationBuffer[0], interpolationBuffer[1], t, clientId);
        interpolatedSnapshot.Apply(clientId);
        
        if(clientTime > nextTime) {
            interpolationBuffer.RemoveAt(0);
        }
    }

    private void Reconciliate()
    {
        var clientInServer = interpolationBuffer[interpolationBuffer.Count - 1].cubeEntities[clientCube.id];

        // 1. Tp client to client's position in server
        reconciliateClientCube.GO.transform.position = clientInServer.position;
        reconciliateClientCube.GO.transform.rotation = clientInServer.rotation;

        // 2. Simulate movements from 'commands' list (all commands not confirmed by server)
        foreach (var command in commands)
        {
            Apply(command, reconciliateClientCube, reconciliateCharacterController);
        }

        // 3. Apply reconciliate position and rotation to client (todo: maybe only do it if difference between clientCube and reconClientCube is greater than THRESHOLD)
        // var step = Vector3.Distance(clientCube.cubeGameObject.transform.position,
        //     reconciliateClientCube.cubeGameObject.transform.position);
        // if (step > 0f) print(step);
        // if (step > 0.5f) print("something wrong");
        
        clientCube.GO.transform.position = reconciliateClientCube.GO.transform.position;
        clientCube.GO.transform.rotation = reconciliateClientCube.GO.transform.rotation;
    }

    private void ReadStoreApplySendInput()
    {
        if (clientCube?.id < 0)
        {
            return;
        }
        
        var command = ReadStoreInput();

        // Read local input (input you dont store nor send to the server)
        if (Input.GetKeyDown(KeyCode.X))
        {
            tempDisconnect = true;
        }
        
        Apply(command, clientCube, clientCharacterController);
        SendInputs();
    }

    private Commands ReadStoreInput()
    {
        var timeout = Time.time + 2f;
        var rotate = Input.GetAxis("Horizontal");
        var forwards = Input.GetAxis("Vertical");
        var hitPackage = Input.GetKey(KeyCode.Space) ? clientCube.Shoot() : null;
        var command = new Commands(inputNumber, forwards, rotate, hitPackage, timeout);
        
        commands.Add(command);
        inputNumber++;

        return command;
    }

    private void Apply(Commands command, CubeEntity cube, CharacterController characterController){
        if(cube == null || characterController == null)
        {
            return;
        }

        var _transform = cube.GO.transform;
        Vector3 move = _transform.forward * command.forwards + Vector3.down * Utils.gravity;
        characterController.Move(move * (Utils.speed * Time.deltaTime));
        _transform.Rotate(new Vector3(0f, command.rotate * (Utils.rotateSpeed * Time.deltaTime), 0f));
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
                command.Serialize(packet.buffer);
            }
            packet.buffer.Flush();

            if (connected && clientCube.id >= 0) Utils.Send(packet, channel, Utils.serverPort);
        }
    }

    private void Join()
    {
        var joinPacket = Packet.Obtain();

        // Send id
        joinPacket.buffer.PutInt((int) Utils.Ports.JOIN);
        joinPacket.buffer.PutInt(clientId);
        joinPacket.buffer.Flush();
        Utils.Send(joinPacket, channel, Utils.serverPort);
    }

    private void PlayerJoined(Packet packet)
    {
        var receivedId = packet.buffer.GetInt();
        
        // Check if player joined is this client (server confirmation that I'm joined)
        if (!connected && (receivedId == clientId))
        {
            // Add myself
            var cubeGO = Instantiate(clientCubePrefab, Utils.startPos, Quaternion.identity);
            var canvas = Instantiate(playerUICanvas, new Vector3(), Quaternion.identity);
            canvas.transform.SetParent(cubeGO.transform);
            canvas.transform.localPosition = new Vector3(0f, 2f, 0f);
            var text = canvas.GetComponentInChildren<Text>();
            if(text != null) text.text = $"{receivedId}";
            
            cubeGO.name = $"client-{clientId}";
            print($"I joined, I'm {cubeGO.name}");
            clientCube = new CubeEntity(cubeGO, receivedId);
            clientCharacterController = cubeGO.GetComponent<CharacterController>();
            cubeEntitiesClient[clientCube.id] = clientCube;
            GameManager.clientId = clientId;

            var reconciliateGO = Instantiate(clientReconciliateCubePrefab, Utils.startPos, Quaternion.identity);
            reconciliateGO.name = $"reconciliate-{clientId}";
            reconciliateClientCube = new CubeEntity(reconciliateGO, clientId);
            reconciliateCharacterController = reconciliateGO.GetComponent<CharacterController>();
            // reconciliateCharacterController.transform.GetChild(1).gameObject.active = false;
            // reconciliateCharacterController.transform.GetChild(0).gameObject.active = false;
            
            
            // Now add all previously connected players
            int previousCubesAmount = packet.buffer.GetInt();
            print($"client-{clientId} receiving join with {previousCubesAmount} players");
            for (int i = 0; i < previousCubesAmount; i++)
            {
                int newCubeId = packet.buffer.GetInt();
                if (!cubeEntitiesClient.ContainsKey(newCubeId))
                {
                    var newCubeGO = Instantiate(clientCubePrefab, Utils.startPos, Quaternion.identity);
                    canvas = Instantiate(playerUICanvas, new Vector3(), Quaternion.identity);
                    canvas.transform.SetParent(newCubeGO.transform);
                    canvas.transform.localPosition = new Vector3(0f, 2f, 0f);
                    text = canvas.GetComponentInChildren<Text>();
                    if(text != null) text.text = $"{newCubeId}";
                    
                    newCubeGO.name = $"player-{newCubeId}";
                    print($"adding {newCubeGO.name}");
                    newCubeGO.transform.SetParent(GameObject.Find("Players(Client)").transform);

                    var newCube = new CubeEntity(newCubeGO, newCubeId);
                    newCube.Deserialize(packet.buffer);
                    cubeEntitiesClient[newCubeId] = newCube;
                }
            }

            connected = true;
        }
        else
        {
            // If new player is not the client
            var cubeGO = Instantiate(clientCubePrefab, Utils.startPos, Quaternion.identity);
            var canvas = Instantiate(playerUICanvas, new Vector3(), Quaternion.identity);
            canvas.transform.SetParent(cubeGO.transform);
            canvas.transform.localPosition = new Vector3(0f, 2f, 0f);
            var text = canvas.GetComponentInChildren<Text>();
            if(text != null) text.text = $"{receivedId}";
            
            cubeGO.name = $"player-{receivedId}";
            print($"{cubeGO.name} joined");
            cubeGO.transform.SetParent(GameObject.Find("Players(Client)").transform);
            var cube = new CubeEntity(cubeGO, receivedId);
            cubeEntitiesClient[cube.id] = cube;
        }
    }
    
    private void PlayerDied(Packet packet)
    {
        var receivedId = packet.buffer.GetInt();
        
        if (cubeEntitiesClient.ContainsKey(receivedId))
        {
            if (receivedId == clientCube.id)
            {
                print("You died! Respawning.."); // Respawn will be done in server
                // TODO: show text in screen saying you died
            }
            else
            {
                print($"player {cubeEntitiesClient[receivedId].GO.name} died");
            }
        }
    }

    private void OnDestroy() {
        channel.Disconnect();
    }
}
