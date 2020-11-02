using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FakeChannel : Singleton<FakeChannel>
{
    private Dictionary<int, List<Packet>> ports; // <port, packets>. One port per client

    public enum ChannelType
    {
        JOIN,
        PLAYER_JOINED,
        DATA,
        INPUT,
        ACK
    }

    private void Start()
    {
    }

    public void Send(int port, Packet packet)
    {
        List<Packet> packets = ports[port];
        packets.Add(packet);
    }

    public Packet GetPacket(int port)
    {
        List<Packet> packets;
        try
        {
            packets = ports[port];
        }
        catch (KeyNotFoundException e)
        {
            print("Key not found");
            return null;
        }
        if (packets == null || packets.Count == 0)
        {
            return null;
        }

        var first = packets[0];
        packets.RemoveAt(0);
        return first;
    }

    public void InitPorts(int port)
    {
        if (ports == null)
        {
            ports = new Dictionary<int, List<Packet>>();
        }
        ports[port] = new List<Packet>();
    }

    public int GetPort(int cubeID)
    {
        return 9000 + cubeID;
    }
}
