using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class SimulationTest : MonoBehaviour
{
    
    /*
     el prox paso es:

    - un jugador es representado por un cubo.
    - cada vez que se une un jugador al servidor se crea una entidad cubo que lo representa
    - el servidor envia un "PlayerJoined" (evento) a todos los demas clientes para que puedan crear la entidad Cubo que representa a ese nuevo jugador
    - los clientes envian Input para mover sus cubos (sin timeout)
        - el servidor toma los inputs y simula los movimientos
    - el servidor envia el snapshot del mundo a los clientes
    */
    
    /*
     TP
    1. Un jugador se representa con un Cubo. Un jugador es representado por un ID. Pueden usar como ID un Random ahora (las chances de colisión son bajas).
    2. Cuando un jugador (cliente) se conecta al servidor este envía un evento al servidor de "Join". El servidor luego crea una entidad Cubo para ese jugador y envía a todos los jugadores conectados un "PlayerJoined" para que estos puedan crear las entidades Cubo en sus simulaciones
    3. Cuando el cliente que ha hecho el "Join" recibe el "PlayerJoined" entonces empieza a enviar los inputs al servidor.
    SEMI-DONE (SOLO PARA UN JUGADOR) 4. El servidor periodicamente serializa y envia el snapshot a todos los jugadores conectados.
    DONE 5. Todos los jugadores reciben los snapshots y lo utilizan para interpolar.

    Caso de uso de nuevo jugador:
    JUGADOR ----(Join)----> Server
    JUGADORES <----(PlayerJoined)---- Server

    Caso de uso de simulación de movimiento de los jugadores:
    JUGADORES ----(Input)----> Server
    JUGADORES <----(Snapshot)---- Server
    */
    
    /*
     * SimulationTest
     * Snapshot
     * Commands
     * CubeEntity
     */

    private Channel channel;  // Send data (position of all players) to client
    private Channel channel2;  // Receive input from client
    private Channel channel3;  // Send number of inputs processed (ack)
    private Channel playerJoinedChannel;  // Server (where it sends new playerJoined events)
    private Channel joinChannel;  // Server (where it listens for new join events)

    private float accum = 0f;
    private float accum2 = 0f;

    private float clientTime = 0f;
    public int pps = 100;
    public int requiredSnapshots = 3;
    private int packetNumber = 0;
    private bool clientPlaying = false;
    private bool connected = true;
    private int countSpace = 0;
    [SerializeField] private GameObject clientCubePrefab;  // Client
    [SerializeField] private GameObject serverCubePrefab;  // Server
    private List<CubeEntity> cubeEntitiesClient;  // Client
    private List<CubeEntity> cubeEntitiesServer;  // Server

    List<Snapshot> interpolationBuffer = new List<Snapshot>();  // Client
    List<Commands> commandServer = new List<Commands>();  // Client
    private int clientId;  // Client
    
    private int amountOfPlayers = 2;  // Both (but harcoded value to be erased)

    // Start is called before the first frame update
    void Start() {
        StartServer();
        StartClient();
    }

    private void StartClient()  // Client
    {
        this.clientId = -1;
        cubeEntitiesClient = new List<CubeEntity>();
        /*for (int i = 0; i < amountOfPlayers; i++)
        {
            var clientCube = Instantiate(clientCubePrefab, new Vector3(i, 0, 0), Quaternion.identity);
            clientCube.transform.parent = null;
            cubeEntitiesClient.Add(new CubeEntity(clientCube, i));
        }*/

        StartCoroutine(Join());
    }

    private void StartServer()  // Server
    {
        cubeEntitiesServer = new List<CubeEntity>();
        /*for (int i = 0; i < amountOfPlayers; i++)
        {
            var serverCube = Instantiate(serverCubePrefab, new Vector3(i, 0, 0), Quaternion.identity);
            serverCube.transform.parent = null;
            cubeEntitiesServer.Add(new CubeEntity(serverCube, i));
        }*/
        channel = new Channel(9000);
        channel2 = new Channel(9001);
        channel3 = new Channel(9002);
        playerJoinedChannel = new Channel(9003);
        joinChannel = new Channel(9004);
    }

    private void OnPlayerJoin()  // Server
    {
        
    }

    private void OnDestroy()  // Server
    {
        channel.Disconnect();
        channel2.Disconnect();
        channel3.Disconnect();
    }

    // Update is called once per frame
    void Update() {
        accum += Time.deltaTime;
        accum2 += Time.deltaTime;

        if (connected)
        {
            UpdateServer();
        }
        UpdateClient();

    }

    private void UpdateServer()  // Server
    {
        //TODO: talk to more than one client

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
            SendPacket(packet, channel);
            
            // Restart accum
            accum -= sendRate;
        }
        
        
        //receive input
        Packet packet2;
        while ( (packet2 = channel2.GetPacket()) != null)
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
                var myCube = cubeEntitiesClient.Find(c => c.id == 123);  // Dont do this every time, store a reference
                myCube.cubeGameObject.GetComponent<Rigidbody>().AddForceAtPosition(force, Vector3.zero, ForceMode.Impulse);

                max = commands.time;
            }

            //send ack
            var packet3 = Packet.Obtain();
            packet3.buffer.PutInt(max);
            packet3.buffer.Flush();
            SendPacket(packet3, channel3);
        }
        
        //receive joins
        Packet joinPacket;
        while ((joinPacket = joinChannel.GetPacket()) != null)
        {
            print("server: got join packet");
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
    
    private void PlayerJoined(int id)  // Server
    {
        var serverCube = Instantiate(serverCubePrefab, new Vector3(id, 0, 0), Quaternion.identity);
        serverCube.transform.parent = null;
        cubeEntitiesServer.Add(new CubeEntity(serverCube, id));

        Packet playerJoinedPacket = Packet.Obtain();
        playerJoinedPacket.buffer.PutInt(id);
        playerJoinedPacket.buffer.Flush();
        SendPacket(playerJoinedPacket, playerJoinedChannel);
    }

    private void UpdateClient()  // Client
    {
        if(clientId < 0) return;  // skip until you have a valid id (maybe not necessary)
        
        //delete from list
        Packet packet3; 
        while ( (packet3=channel3.GetPacket()) != null)
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

            SendPacket(packet2, channel2);

            accum2 -= sendRate;
        }

        //receive data
        var packet = channel.GetPacket();
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

    private void SendPacket(Packet packet, Channel sendChannel)
    {
        string serverIP = "127.0.0.1";
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), sendChannel.port);
        sendChannel.Send(packet, remoteEp);
        packet.Free();
    }

    private void ReadInput()  // Client
    {
        var timeout = Time.time + 2;
        var command = new Commands(packetNumber, Input.GetKeyDown(KeyCode.W), Input.GetKeyDown(KeyCode.S), 
            Input.GetKeyDown(KeyCode.A), Input.GetKeyDown(KeyCode.D),
            Input.GetKeyDown(KeyCode.Space), timeout);
        commandServer.Add(command);
        if (Input.GetKeyDown(KeyCode.X))
        {
            connected = false;
            channel2.Disconnect();
            channel3.Disconnect();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            connected = true;
            StartCoroutine(Join());
            channel2 = new Channel(9001);
            channel3 = new Channel(9002);
        }
    }

    IEnumerator Join()
    {
        var joinPacket = Packet.Obtain();
        
        // send id
        var id = 123; //Random.Range(0, 999999);  // Warning: on disconnect/connect the player gets a new id (potentially dangerous, maybe send old id)
        joinPacket.buffer.PutInt(id);
        joinPacket.buffer.Flush();
        SendPacket(joinPacket, joinChannel);
        joinChannel.Disconnect();
        
        // wait for confirmation
        var ownIdReceived = false;
        while (!ownIdReceived)
        {
            Packet playerJoinedPacket = playerJoinedChannel.GetPacket();
            if (playerJoinedPacket == null)
            {
                yield return new WaitForSeconds(1);
                print("nope");
                continue;
            }
            print("got one");

            var receivedId = playerJoinedPacket.buffer.GetInt();

            var clientCube = Instantiate(clientCubePrefab, new Vector3(Random.Range(1,5), Random.Range(1,5), Random.Range(1,5)), Quaternion.identity);
            clientCube.transform.parent = null;
            cubeEntitiesClient.Add(new CubeEntity(clientCube, receivedId));
            if (receivedId == id)
            {
                print("got mine!");
                ownIdReceived = true;
            }
        }

        this.clientId = id;
    }

    private void Interpolate()  // Client
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