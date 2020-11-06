using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class Utils
{

    public enum Ports
    {
        JOIN,
        PLAYER_JOINED,
        DATA,
        INPUT,
        ACK
    };

    public static int serverPort = 8999;
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }
    
    public static void Send(Packet packet, Channel channel, int remotePort)
    {
        string serverIP = "127.0.0.1";
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), remotePort);
        channel.Send(packet, remoteEp);
        packet.Free();
    }

    public static int GetPortFromId(int id)
    {
        return 9000 + id % 1000;
    }
}
