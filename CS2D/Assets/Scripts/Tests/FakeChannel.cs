using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FakeChannel : Singleton<FakeChannel>
{
    private List<Packet> channel;  // Send data (position of all players) to client
    private List<Packet> channel2;  // Receive input from client
    private List<Packet> channel3;  // Send number of inputs processed (ack)
    private List<Packet> playerJoinedChannel;  // Server (where it sends new playerJoined events)
    private List<Packet> joinChannel;  // Server (where it listens for new join events)
    private Dictionary<ChannelType, List<Packet>> channels;

    public enum ChannelType
    {
        DATA,
        INPUT,
        ACK,
        PLAYER_JOINED,
        JOIN
    }

    private void Start()
    {
        print("fakechannel start");
        InitDictionary();
    }

    public void Send(Packet packet, ChannelType channelType)
    {
        List<Packet> channel = channels[channelType];
        
        channel.Add(packet);
    }

    public Packet GetPacket(ChannelType channelType)
    {
        if (channels == null)
        {
            InitDictionary();
            return null;
        }
        
        List<Packet> channel = channels[channelType];
        if (channel.Count == 0)
        {
            return null;
        }

        var first = channel[0];
        channel.RemoveAt(0);
        return first;
    }

    private void InitDictionary()
    {
        channels = new Dictionary<ChannelType, List<Packet>>();
        foreach (ChannelType i in Enum.GetValues(typeof(ChannelType)))
        {
            channels[i] = new List<Packet>();
        }
    }

    public string PrintChannels()
    {
        string ret = "";
        foreach (ChannelType i in Enum.GetValues(typeof(ChannelType)))
        {
            ret += channels[i].Count + ", ";
        }

        return ret;
    }
}
