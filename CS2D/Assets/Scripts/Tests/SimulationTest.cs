using System;
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
    
    /* Solucion al tema de los channels: usar una clase FakeChannel con un mapa de
        (puerto, cola) donde puedo encolar y desencolar mensajes a un puerto, 
        simulando asi una comunicacion UDP.
        Nota: no simula por default la perdida ni mezcla de paquetes
     */
    private void Start()
    {
    }
    
    private void Update()
    {
    }
    
    public void SendPacket(Packet packet, Channel sendChannel)
    {
        string serverIP = "127.0.0.1";
        IPEndPoint remoteEp = null;
        IPAddress ip = IPAddress.Parse(serverIP);
        try
        {
            remoteEp = new IPEndPoint(ip, sendChannel.port);
        }
        catch (Exception e)
        {
            print(remoteEp);
        }

        if (remoteEp != null)
        {
            sendChannel.Send(packet, remoteEp);
            packet.Free();   
        }
    }
}