﻿using System.Collections;
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
    public static float gravity = 1f;
    public static float speed = 8f;
    public static Vector3 startPos = new Vector3(0f, 6f, 0f);
    public static float rotateSpeed = 400f;


    // Start is called before the first frame update
    void Start()
    {
        
    }
    
    public static void Send(Packet packet, Channel channel, int remotePort)
    {
        string serverIP = GameManager.IPAddress;
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), remotePort);
        channel.Send(packet, remoteEp);
        packet.Free();
    }

    public static int GetPortFromId(int id)
    {
        return 9000 + id % 1000;
    }
}