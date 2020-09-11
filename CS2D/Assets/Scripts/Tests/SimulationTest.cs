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

    private Channel channel;
    private Channel channel2;
    private Channel channel3;

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
    
    private int amountOfPlayers = 2;  // Both

    // Start is called before the first frame update
    void Start() {
        StartClient();
        StartServer();
    }

    private void StartClient()  // Client
    {
        cubeEntitiesClient = new List<CubeEntity>();
        for (int i = 0; i < amountOfPlayers; i++)
        {
            var clientCube = Instantiate(clientCubePrefab, new Vector3(i, 0, 0), Quaternion.identity);
            clientCube.transform.parent = null;
            cubeEntitiesClient.Add(new CubeEntity(clientCube, i));
        }
    }

    private void StartServer()  // Server
    {
        cubeEntitiesServer = new List<CubeEntity>();
        for (int i = 0; i < amountOfPlayers; i++)
        {
            var serverCube = Instantiate(serverCubePrefab, new Vector3(i, 0, 0), Quaternion.identity);
            serverCube.transform.parent = null;
            cubeEntitiesServer.Add(new CubeEntity(serverCube, i));
        }
        channel = new Channel(9000);  // Send data (position from all players) to client
        channel2 = new Channel(9001);  // Receive input from client
        channel3 = new Channel(9002);  // Send number of inputs processed (ack)
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
            string serverIP = "127.0.0.1";
            int port = 9000;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel.Send(packet, remoteEp);
            packet.Free();
            
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
                cubeEntitiesServer[0].cubeGameObject.GetComponent<Rigidbody>().AddForceAtPosition(force, Vector3.zero, ForceMode.Impulse);

                max = commands.time;
            }

            //send ack
            var packet3 = Packet.Obtain();
            packet3.buffer.PutInt(max);
            packet3.buffer.Flush();
            string serverIP = "127.0.0.1";
            int port = 9002;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel.Send(packet3, remoteEp);
            packet3.Free();
        }
    }

    private void UpdateClient()  // Client
    {
        
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

            string serverIP = "127.0.0.1";
            int port = 9001;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel.Send(packet2, remoteEp);
            packet2.Free();
            
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
            channel2 = new Channel(9001);
            channel3 = new Channel(9002);
        }
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