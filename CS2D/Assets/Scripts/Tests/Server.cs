using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Server : MonoBehaviour
{
    public int pps = 60;

    private Channel channel;
    
    [SerializeField] private GameObject serverCubePrefab;
    [SerializeField] private GameObject playerUICanvas;
    private Dictionary<int, CubeEntity> cubeEntitiesServer;
    private Dictionary<int, CubeEntity> bots;
    private List<int> idsUsed;
    private Dictionary<int, int> packetNumbers; // packetNumbers[cubeId] = cubePacketNumber
    private Dictionary<int, int> maxInputs; // maxInputs[cubeId] = cubeMaxInput

    // Start is called before the first frame update
    public void Start()
    {
        channel = new Channel(Utils.serverPort);

        cubeEntitiesServer = new Dictionary<int, CubeEntity>();
        bots = new Dictionary<int, CubeEntity>();
        idsUsed = new List<int>();
        packetNumbers = new Dictionary<int, int>();
        maxInputs = new Dictionary<int, int>();
        InvokeRepeating(nameof(BotRandomMove), 1f, 0.01f);
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
        if (!cubeEntitiesServer.ContainsKey(id)) return;
        
        var cube = cubeEntitiesServer[id];
        
        int prevMax = maxInputs[id], amountOfCommandsToProcess = packet.buffer.GetInt();
        for (int i = 0; i < amountOfCommandsToProcess; i++)
        {
            var command = new Commands();
            command.Deserialize(packet.buffer);

            if (command.inputNumber > maxInputs[id])
            {
                var cubeGO = cube.GO;
                var _transform = cubeGO.transform;
                Vector3 move = _transform.forward * command.forwards + Vector3.down * Utils.gravity;
                cubeGO.GetComponent<CharacterController>().Move(move * (Utils.speed * Time.deltaTime));
                _transform.Rotate(new Vector3(0f, command.rotate * (Utils.rotateSpeed * Time.deltaTime), 0f));
                
                if (!command.hitPackage.hitName.Equals(""))
                {
                    print($"{cube.id} hit {command.hitPackage.hitName} and did {command.hitPackage.damage} damage");
                    TakeDamage(command.hitPackage.hitName, command.hitPackage.damage, cube.id);
                }

                maxInputs[id] = command.inputNumber;
            }
        }

        // send ack
        if (!cube.isBot && maxInputs[id] > prevMax) SendAck(cube);
    }

    private void SendAck(CubeEntity cube)
    {
        var packet3 = Packet.Obtain();
        packet3.buffer.PutInt((int) Utils.Ports.ACK);
        packet3.buffer.PutInt(maxInputs[cube.id]);
        packet3.buffer.Flush();
        Utils.Send(packet3, channel, cube.port);
    }

    private void ReceiveJoins(Packet packet, bool isBot = false)
    {
        int id = packet.buffer.GetInt();
        
        var existingPlayer = idsUsed.Contains(id);
        if (!existingPlayer)
        {
            PlayerJoined(id, isBot);
            idsUsed.Add(id);
        }
    }
    
    private void SendData()
    {
        if (cubeEntitiesServer.Count == 0) return;
        
            
        foreach (var kv in cubeEntitiesServer)
        {
            var cube = kv.Value;
            if(cube.isBot) continue;

            packetNumbers[cube.id] = packetNumbers[cube.id] + 1;
            
            // serialize
            var packet = Packet.Obtain();
            packet.buffer.PutInt((int) Utils.Ports.DATA);
            var snapshot = new Snapshot(cubeEntitiesServer, packetNumbers[cube.id]);
            snapshot.Serialize(packet.buffer);
            packet.buffer.Flush();
            Utils.Send(packet, channel, cube.port);
        }
    }
    
    private void PlayerJoined(int id, bool isBot = false)  // Server
    {
        // init new player
        var cubeGO = Instantiate(serverCubePrefab, Utils.RandomStartPos(), Quaternion.identity);
        
        // Attach label on top of his head
        var canvas = Instantiate(playerUICanvas, new Vector3(), Quaternion.identity);
        canvas.transform.SetParent(cubeGO.transform);
        canvas.transform.localPosition = new Vector3(0f, 2f, 0f);
        
        cubeGO.transform.SetParent(GameObject.Find("Players(Server)").transform);
        cubeGO.name = $"player-{id}";
        var newCube = new CubeEntity(cubeGO, id, isBot);
        cubeEntitiesServer[newCube.id] = newCube;

        newCube.SetUIManager(canvas.GetComponent<UIManager>(), $"{id}", 1f);
        if(isBot) {
            bots[newCube.id] = newCube; // store bots in another list to test applying movement to them
        }
        else
        {
            packetNumbers[newCube.id] = 0;
            maxInputs[newCube.id] = 0;
        }
        print($"{cubeGO.name} joined");
        
        foreach (var cube in cubeEntitiesServer.Values)
        {
            if(cube.isBot) continue;
            
            // send player joined packet to everyone (including to the new player as a confirmation)
            Packet playerJoinedPacket = Packet.Obtain();
            var buffer = playerJoinedPacket.buffer;
            buffer.PutInt((int) Utils.Ports.PLAYER_JOINED);
            buffer.PutInt(id);
            buffer.PutInt(cubeEntitiesServer.Count-1);
            foreach (var sendCube in cubeEntitiesServer.Values)
            {
                if(sendCube.id == cube.id) continue;  // dont send a player to himself
                
                buffer.PutInt(sendCube.id);
                sendCube.Serialize(buffer);
            }
            buffer.Flush();
            Utils.Send(playerJoinedPacket, channel, cube.port);
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
            var command = new Commands(Time.frameCount, 1, 0.2f, null, false);
        
            var packet = Packet.Obtain();
            packet.buffer.PutInt((int) Utils.Ports.INPUT);
            packet.buffer.PutInt(kv.Key);
            packet.buffer.PutInt(1);
            for (int i = 0; i < 1; i++)
            {
                command.Serialize(packet.buffer);
            }
            packet.buffer.Flush();

            ReceiveInputs(packet);
        }
    }
    
    public void TakeDamage(string cubeName, float damage, int shooterId)
    {
        if (!cubeEntitiesServer.ContainsKey(shooterId)) return;

        var matchingCube = cubeEntitiesServer.Values.FirstOrDefault(c => c.GO.transform.name.Equals(cubeName));
        if (matchingCube != null)
        {
            float health = matchingCube.TakeDamage(damage);
            if (health <= 0f)
            {
                foreach (var kv in cubeEntitiesServer)
                {
                    var packet = Packet.Obtain();
                    packet.buffer.PutInt((int) Utils.Ports.PLAYER_DIED);
                    packet.buffer.PutInt(matchingCube.id);
                    packet.buffer.Flush();
                    Utils.Send(packet, channel, kv.Value.port);
                }
            
                // Teleport and respawn
                cubeEntitiesServer[shooterId].points += 1; // award shooter a point
                matchingCube.health = 100f; // restore health
                matchingCube.GO.transform.position = Utils.waitRoomPos; // tp to wait room
                print($"{matchingCube.id} was killed by {shooterId}. Respawining in 10 seconds");
                StartCoroutine(RespawnPlayer(matchingCube));
            }
        }
    }

    private IEnumerator RespawnPlayer(CubeEntity cube)
    {
        yield return new WaitForSeconds(10);
        print("Respawning player..");
        cube.GO.transform.position = Utils.RandomStartPos();
    }

    private void OnDestroy() {
        channel.Disconnect();
    }
}
