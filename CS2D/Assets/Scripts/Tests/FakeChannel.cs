using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FakeChannel : Singleton<FakeChannel>
{
    private Dictionary<int, Dictionary<ChannelType, List<Packet>>> channels; // <port, <channelType, packets>>

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

    public void Send(int port, Packet packet, ChannelType channelType)
    {
        List<Packet> channel = channels[port][channelType];
        
        channel.Add(packet);
    }

    public Packet GetPacket(int port, ChannelType channelType)
    {
        List<Packet> channel;
        try
        {
            channel = channels[port][channelType];
        }
        catch (KeyNotFoundException e)
        {
            return null;
        }
        if (channel == null || channel.Count == 0)
        {
            return null;
        }

        var first = channel[0];
        channel.RemoveAt(0);
        return first;
    }

    public void InitDictionary(int port)
    {
        if (channels == null)
        {
            channels = new Dictionary<int, Dictionary<ChannelType, List<Packet>>>();
        }
        channels[port] = new Dictionary<ChannelType, List<Packet>>();
        foreach (ChannelType i in Enum.GetValues(typeof(ChannelType)))
        {
            channels[port][i] = new List<Packet>();
        }
    }
}
