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
    public float rps = 60; // reconciliates per second
    private Cooldown packetsCooldown;
    private Cooldown reconciliateCooldown;
    private int inputNumber = 0;
    private float clientTime = 0f;
    public int requiredSnapshots = 3;
    private bool connected = false;
    private bool tempDisconnect = false;

    private Channel channel;

    [SerializeField] private GameObject clientCubePrefab;
    [SerializeField] private GameObject clientReconciliateCubePrefab;
    [SerializeField] private GameObject playerUICanvas;
    [SerializeField] private GameObject laserPrefab;
    private GeneralUIManager UICanvas;
    private Dictionary<int, CubeEntity> cubeEntitiesClient;

    List<Snapshot> interpolationBuffer = new List<Snapshot>();
    List<Commands> commands = new List<Commands>();
    private int clientId;
    private CubeEntity clientCube = null;
    private CharacterController clientCharacterController = null;
    private CubeEntity reconciliateClientCube;
    private CharacterController reconciliateCharacterController;
    private int msFakeLag;

    // Start is called before the first frame update
    public void Start()
    {
        cubeEntitiesClient = new Dictionary<int, CubeEntity>();
        clientId = Random.Range(0, 1000000);
        channel = new Channel(Utils.GetPortFromId(clientId));
        GameObject.Find("Players(Client)").transform.position = Vector3.zero;
        print($"IPAddress: {GameManager.IPAddress}");
        UICanvas = GameObject.Find("UICanvas").GetComponent<GeneralUIManager>();
        packetsCooldown = new Cooldown(1f/pps);
        reconciliateCooldown = new Cooldown(1f/rps);
        GameManager.laserPrefab = laserPrefab;
    }

    private void FixedUpdate()
    {
        packetsCooldown.UpdateCooldown();
        reconciliateCooldown.UpdateCooldown();

        foreach (var cube in cubeEntitiesClient.Values)
        {
            cube.UpdateShootingCooldown();
        }
        
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
        
        if(clientCube != null) UICanvas.SetUI(clientCube.points);
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

        var snapshot = new Snapshot(cubeEntitiesClient, -1, new List<Bullet>());
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

        if (reconciliateCooldown.IsOver())
        {
            Reconciliate(); // for client
            reconciliateCooldown.RestartCooldown();
        }
        
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
        interpolationBuffer[0].bullets.ForEach(bullet =>
        {
            bullet.Cast();
        });
        
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

        // 3. Apply reconciliate position and rotation to client
        clientCube.GO.transform.position = reconciliateClientCube.GO.transform.position;
        if (Vector3.Distance(reconciliateClientCube.GO.transform.position, clientCube.GO.transform.position) > 0.0001f)
        {
        }

        clientCube.GO.transform.rotation = reconciliateClientCube.GO.transform.rotation;
        if (Quaternion.Angle(reconciliateClientCube.GO.transform.rotation, clientCube.GO.transform.rotation) < 0.0001f)
        {
        }
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
        var shoot = Input.GetKey(KeyCode.Space);
        var hitPackages = shoot ? clientCube.Shoot() : null;
        var command = new Commands(inputNumber, forwards, rotate, hitPackages, hitPackages != null, timeout);
        
        commands.Add(command);
        inputNumber++;

        // Local commands (we dont send these to the server)
        if(Input.GetKey(KeyCode.Keypad0))
        {
            msFakeLag = 0;
        } else if(Input.GetKey(KeyCode.Keypad1))
        {
            msFakeLag = 100;
        } else if(Input.GetKey(KeyCode.Keypad2))
        {
            msFakeLag = 200;
        } else if(Input.GetKey(KeyCode.Keypad3))
        {
            msFakeLag = 300;
        } else if(Input.GetKey(KeyCode.Keypad4))
        {
            msFakeLag = 500;
        } else if(Input.GetKey(KeyCode.Keypad5))
        {
            msFakeLag = 1000;
        } else if(Input.GetKey(KeyCode.Keypad6))
        {
            msFakeLag = 1500;
        }
        
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
        if (commands.Count != 0 && packetsCooldown.IsOver())
        {
            packetsCooldown.RestartCooldown();
            
            var packet = Packet.Obtain();
            packet.buffer.PutInt((int) Utils.Ports.INPUT);
            packet.buffer.PutInt(clientId);
            packet.buffer.PutInt(commands.Count);
            foreach (var command in commands)
            {
                command.Serialize(packet.buffer);
            }
            packet.buffer.Flush();

            if (connected && clientCube.id >= 0)
            {
                if (msFakeLag > 0)
                {
                    StartCoroutine(DelayedInputSend(packet));
                }
                else
                {
                    Utils.Send(packet, channel, Utils.serverPort);
                }
            }
        }
    }

    private IEnumerator DelayedInputSend(Packet packet)
    {
        yield return new WaitForSecondsRealtime(msFakeLag/1000f);
        Utils.Send(packet, channel, Utils.serverPort);
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
        if (cubeEntitiesClient.ContainsKey(receivedId)) return; // Skip anyone you already added
        
        var cubeGO = Instantiate(clientCubePrefab, Utils.startPos, Quaternion.identity);
        cubeGO.transform.SetParent(GameObject.Find("Players(Client)").transform);
        
        var cube = new CubeEntity(cubeGO, receivedId);
        cubeEntitiesClient[cube.id] = cube;

        // Attach label on top of his head
        AddUIToCube(cube, receivedId);

        // Check if player joined is this client (server confirmation that I'm joined)
        if (receivedId == clientId)
        {
            // Add myself
            cubeGO.name = $"client-{clientId}";
            print($"I joined, I'm {cubeGO.name}");
            clientCharacterController = cubeGO.GetComponent<CharacterController>();
            clientCube = cube;
            GameManager.clientId = clientId;

            // Add my reconciliation clone
            var reconciliateGO = Instantiate(clientReconciliateCubePrefab, Utils.startPos, Quaternion.identity);
            reconciliateGO.name = $"reconciliate-{clientId}";
            reconciliateClientCube = new CubeEntity(reconciliateGO, clientId);
            reconciliateCharacterController = reconciliateGO.GetComponent<CharacterController>();
            
            // Now add all players already in-game
            int previousCubesAmount = packet.buffer.GetInt();
            print($"client-{clientId} receiving join with {previousCubesAmount} players");
            for (int i = 0; i < previousCubesAmount; i++)
            {
                int newCubeId = packet.buffer.GetInt();
                if (!cubeEntitiesClient.ContainsKey(newCubeId))
                {
                    var newCubeGO = Instantiate(clientCubePrefab, Utils.startPos, Quaternion.identity);
                    newCubeGO.name = $"player-{newCubeId}";
                    print($"adding {newCubeGO.name}");
                    newCubeGO.transform.SetParent(GameObject.Find("Players(Client)").transform);

                    var newCube = new CubeEntity(newCubeGO, newCubeId);
                    newCube.Deserialize(packet.buffer);
                    cubeEntitiesClient[newCubeId] = newCube;

                    AddUIToCube(newCube, newCubeId);
                }
            }

            connected = true;
        }
        else
        {
            // If new player is not the client
            cubeGO.name = $"player-{receivedId}";
            print($"{cubeGO.name} joined");
        }
    }

    private void AddUIToCube(CubeEntity cube, int cubeId)
    {
        var canvas = Instantiate(playerUICanvas, new Vector3(), Quaternion.identity);
        canvas.transform.SetParent(cube.GO.transform);
        canvas.transform.localPosition = new Vector3(0f, 2f, 0f);
        cube.SetUIManager(canvas.GetComponent<UIManager>(), $"{cubeId}", 1f);
        // var healthBar = canvas.GetComponent<HealthBar>();
        // if (healthBar != null)
        // {
        //     var healthBarComp = healthBar.GetComponent<HealthBar>();
        //     if(healthBarComp != null) cube.SetHealthBar(healthBarComp);
        //     else
        //     {
        //         print("mini not");
        //         cube.SetHealthBar(healthBar);
        //     }
        // }
        // else
        // {
        //     print("not");
        // }
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
