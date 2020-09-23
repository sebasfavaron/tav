using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class SimulationServer : MonoBehaviour
{
    private float accum = 0f;
    private int packetNumber = 0;
    public int pps = 100;

    private FakeChannel fakeChannel;
    
    [SerializeField] private GameObject serverCubePrefab;  // Server
    private List<CubeEntity> cubeEntitiesServer;  // Server

    private int amountOfPlayers = 2;  // Both (but harcoded value to be erased)
    
    
    // Start is called before the first frame update
    public void Start()
    {
        fakeChannel = FakeChannel.Instance;
        
        cubeEntitiesServer = new List<CubeEntity>();
        /*for (int i = 0; i < amountOfPlayers; i++)
        {
            var serverCube = Instantiate(serverCubePrefab, new Vector3(i, 0, 0), Quaternion.identity);
            serverCube.transform.parent = null;
            cubeEntitiesServer.Add(new CubeEntity(serverCube, i));
        }*/

        //connections = GetComponentInParent<SimulationTest>();
        /*for (int id = 0; id < 2; id++)
        {
            var serverCube = Instantiate(serverCubePrefab, new Vector3(id, 0, 0), Quaternion.identity);
            serverCube.transform.parent = null;
            cubeEntitiesServer.Add(new CubeEntity(serverCube, id));    
        }*/

        //enabled = false;
    }

    // Update is called once per frame
    public void Update()
    {
        accum += Time.deltaTime;

        //send data
        float sendRate = (1f / pps);
        if (accum >= sendRate)
        {
            packetNumber += 1;
            
            //serialize
            var packet = Packet.Obtain();
            var snapshot = new Snapshot(packetNumber, cubeEntitiesServer);
            snapshot.Serialize(packet.buffer);
            packet.buffer.Flush();
            fakeChannel.Send(packet, FakeChannel.ChannelType.DATA);
            
            // Restart accum
            accum -= sendRate;
        }
        
        
        //receive input
        Packet packet2;
        while ( (packet2 = fakeChannel.GetPacket(FakeChannel.ChannelType.INPUT)) != null)
        {
            int max = 0, amountOfCommandsToProcess = packet2.buffer.GetInt();
            for (int i = 0; i < amountOfCommandsToProcess; i++){
                var commands = new Commands();
                commands.Deserialize(packet2.buffer);
                Vector3 force = Vector3.zero;
                force += commands.space ? Vector3.up * 5 : Vector3.zero;
                force += commands.up ? Vector3.forward * 2 : Vector3.zero;
                force += commands.down ? Vector3.back * 2 : Vector3.zero;
                force += commands.left ? Vector3.left * 2 : Vector3.zero;
                force += commands.right ? Vector3.right * 2 : Vector3.zero;
                if (cubeEntitiesServer.Count > 0)
                {
                    var myCube = cubeEntitiesServer.Find(c => c.id == 123);  // Should be dependent on the channel and the cube asociated with it
                    myCube.cubeGameObject.GetComponent<Rigidbody>().AddForceAtPosition(force, Vector3.zero, ForceMode.Impulse);
                }
                
                max = commands.time;
            }

            //send ack
            var packet3 = Packet.Obtain();
            packet3.buffer.PutInt(max);
            packet3.buffer.Flush();
            fakeChannel.Send(packet3, FakeChannel.ChannelType.ACK);
        }
        
        //receive joins
        Packet joinPacket;
        while ((joinPacket = fakeChannel.GetPacket(FakeChannel.ChannelType.JOIN)) != null)
        {
            var id = joinPacket.buffer.GetInt();
            //  Make sure id is unique to avoid collisions (cant change id yet because the client is listening for his own id, needs different approach)
            /*var idAlreadyUsed = true;
            while (idAlreadyUsed)
            {
                idAlreadyUsed = false;
                foreach (var cube in cubeEntitiesServer)
                {
                    if (cube.id == id)
                    {
                        idAlreadyUsed = true;
                        id = Random.Range(0, 999999);                        
                        break;
                    }   
                }
            }
            */
            PlayerJoined(id);
        }
    }
    
    private void OnPlayerJoin()  // Server
    {
        
    }
    
    private void PlayerJoined(int id)  // Server
    {
        //serverCube.transform.parent = null;
        if (!cubeEntitiesServer.Exists(c => c.id == id))
        {
            var serverCube = Instantiate(serverCubePrefab, new Vector3(Random.Range(-4, 4), 1, Random.Range(-4,4)), Quaternion.identity);
            cubeEntitiesServer.Add(new CubeEntity(serverCube, id));
        }
        else
        {
            print($"Error adding played with id {id}. Id already exists");
        }

        Packet playerJoinedPacket = Packet.Obtain();
        playerJoinedPacket.buffer.PutInt(id);
        playerJoinedPacket.buffer.Flush();
        fakeChannel.Send(playerJoinedPacket, FakeChannel.ChannelType.PLAYER_JOINED);
    }

    private void BotJoin(int id)
    {
        var joinPacket = Packet.Obtain();

        // send id
        //clientId = Random.Range(0, 999999);  // Warning: on disconnect/connect the player gets a new id (potentially dangerous, maybe send old id)
        joinPacket.buffer.PutInt(id);
        joinPacket.buffer.Flush();
        fakeChannel.Send(joinPacket, FakeChannel.ChannelType.JOIN);
    }
}
