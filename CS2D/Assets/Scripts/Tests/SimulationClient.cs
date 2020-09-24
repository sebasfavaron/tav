using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class SimulationClient : MonoBehaviour
{
    private float accum2 = 0f;
    private int packetNumber = 0;
    public int pps = 100;
    private float clientTime = 0f;
    public int requiredSnapshots = 3;
    private bool clientPlaying = false;
    private bool connected = false;
    private bool tempDisconnect = false;

    private FakeChannel fakeChannel;

    [SerializeField] private GameObject clientCubePrefab;  // Client
    private List<CubeEntity> cubeEntitiesClient;  // Client

    List<Snapshot> interpolationBuffer = new List<Snapshot>();  // Client
    List<Commands> commandServer = new List<Commands>();  // Client
    private int clientId;  // Client
    
    private int amountOfPlayers = 2;  // Both (but harcoded value to be erased)

    // Start is called before the first frame update
    public void Start()
    {
        fakeChannel = FakeChannel.Instance;
        
        cubeEntitiesClient = new List<CubeEntity>();
        /*for (int i = 0; i < amountOfPlayers; i++)
        {
            var clientCube = Instantiate(clientCubePrefab, new Vector3(i, 0, 0), Quaternion.identity);
            clientCube.transform.parent = null;
            cubeEntitiesClient.Add(new CubeEntity(clientCube, i));
        }*/
        
        /*for (int id = 0; id < 2; id++)
        {
            var clientCube = Instantiate(clientCubePrefab, new Vector3(id, 0, 0), Quaternion.identity);
            clientCube.transform.parent = null;
            cubeEntitiesClient.Add(new CubeEntity(clientCube, id));    
        }*/
        //enabled = false;
        clientId = Random.Range(0, 1000000);
    }

    // Update is called once per frame
    public void Update()
    {
        accum2 += Time.deltaTime;
        if ((int)accum2 % 10 == 0)
        {
            PlayerJoined();

            if (!connected)
            {
                Join();
            }
            
            // Still check this every now and then for new players joining
            PlayerJoined();
        }
        if (!connected || tempDisconnect)
        {
            return;
        }

        //delete from list
        Packet packet3;
        while ( (packet3=fakeChannel.GetPacket(GetPort(clientId), FakeChannel.ChannelType.ACK)) != null)
        {
            var toDel = packet3.buffer.GetInt();
            while (commandServer.Count != 0)
            {
                if (commandServer[0].time <= toDel)
                {
                    commandServer.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }
        }

        while(commandServer.Count != 0)
        {
            if (commandServer[0].timestamp < Time.time)
            {
                commandServer.RemoveAt(0);
            }
            else
            {
                break;
            }
        }

        //send input
        float sendRate = (1f / 100);
        if (accum2 >= sendRate)
        {
            ReadInput();
            var packet2 = Packet.Obtain();
            packet2.buffer.PutInt(commandServer.Count);
            foreach (var currentCommand in commandServer)
            {
                currentCommand.Serialize(packet2.buffer);
            }
            packet2.buffer.Flush();

            fakeChannel.Send(GetPort(clientId), packet2, FakeChannel.ChannelType.INPUT);

            accum2 -= sendRate;
        }

        //receive data
        var packet = fakeChannel.GetPacket(GetPort(clientId), FakeChannel.ChannelType.DATA);
        if (packet != null) {
            var snapshot = new Snapshot(-1, cubeEntitiesClient);
            var buffer = packet.buffer;

            snapshot.Deserialize(buffer);

            int size = interpolationBuffer.Count;
            if(size == 0 || snapshot.packetNumber > interpolationBuffer[size - 1].packetNumber) {
                interpolationBuffer.Add(snapshot);
            }
        }

        if (interpolationBuffer.Count >= requiredSnapshots) {
            clientPlaying = true;
        }
        else if (interpolationBuffer.Count <= 1) {
            clientPlaying = false;
        }
        if (clientPlaying) {
            clientTime += Time.deltaTime;
            Interpolate();
        }
    }
    
    private void ReadInput()
    {
        var timeout = Time.time + 2;
        var command = new Commands(packetNumber, Input.GetKeyDown(KeyCode.W), Input.GetKeyDown(KeyCode.S), 
            Input.GetKeyDown(KeyCode.A), Input.GetKeyDown(KeyCode.D),
            Input.GetKeyDown(KeyCode.Space), timeout);
        commandServer.Add(command);
        if (Input.GetKeyDown(KeyCode.X))
        {
            tempDisconnect = true;  //TODO: esto ya no funciona (pero no hace falta creo)
            //fakeChannel.Disconnect(FakeChannel.ChannelType.INPUT);
            //fakeChannel.Disconnect(FakeChannel.ChannelType.ACK);
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            tempDisconnect = false;
            //fakeChannel.Connect(FakeChannel.ChannelType.INPUT);
            //fakeChannel.Connect(FakeChannel.ChannelType.ACK);
        }
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
        int port = connected ? clientId : 9000; // until connected, the only port you can hear in is 9000, where you wait for your playerJoined confirmation
        while ((playerJoinedPacket = fakeChannel.GetPacket(port, FakeChannel.ChannelType.PLAYER_JOINED)) != null)
        {
            var randomNumber = playerJoinedPacket.buffer.GetInt();
            var receivedId = playerJoinedPacket.buffer.GetInt();

            if (!cubeEntitiesClient.Exists(c => c.id == receivedId))  // Do not add a cube twice, check if it exists already
            {
                var clientCube = Instantiate(clientCubePrefab, new Vector3(), Quaternion.identity);
                cubeEntitiesClient.Add(new CubeEntity(clientCube, receivedId));
            }
            
            if (randomNumber == clientId)
            {
                clientId = receivedId;  // Now clientId is real
                connected = true;
            }
        }
    }
    
    private int GetPort(int cubeID)
    {
        return 9000 + cubeID * 10;
    }
    
    private void SendPacket(Packet packet, Channel sendChannel)
    {
        string serverIP = "127.0.0.1";
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), sendChannel.port);
        sendChannel.Send(packet, remoteEp);
        packet.Free();
    }

    private void Interpolate()
    {
        var previousTime = interpolationBuffer[0].packetNumber * (1f/pps);
        var nextTime =  interpolationBuffer[1].packetNumber * (1f/pps);
        var t =  (clientTime - previousTime) / (nextTime - previousTime);
        var interpolatedSnapshot = Snapshot.CreateInterpolated(interpolationBuffer[0], interpolationBuffer[1], t);
        interpolatedSnapshot.Apply();

        if(clientTime > nextTime) {
            interpolationBuffer.RemoveAt(0);
        }
    }
}
