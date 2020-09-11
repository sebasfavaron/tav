using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ConnectionTest : MonoBehaviour {

    private Channel channel1;
    private Channel channel2;

    void Start() {
        channel1 = new Channel(9000);
        channel2 = new Channel(9001);
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.A)) {
            var packet = Packet.Obtain();
            packet.buffer.PutString("hello");
            packet.buffer.Flush();
            string serverIP = "127.0.0.1";
            int port = 9001;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel1.Send(packet, remoteEp);
            Debug.Log("hello sent to channel2");
            packet.Free();
        }

        if (Input.GetKeyDown(KeyCode.B)) {
            var packet = Packet.Obtain();
            packet.buffer.PutString("hello");
            packet.buffer.Flush();
            string serverIP = "127.0.0.1";
            int port = 9000;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel2.Send(packet, remoteEp);
            Debug.Log("hello sent to channel1");
            packet.Free();
        }

        var receivedPacket = channel1.GetPacket();
        if (receivedPacket != null) {
            var message = receivedPacket.buffer.GetString();
            Debug.Log("channel1 received message "+ message +" from "+receivedPacket.fromEndPoint);
        }

        receivedPacket = channel2.GetPacket();
        if (receivedPacket != null) {
            var message = receivedPacket.buffer.GetString();
            Debug.Log("channel2 received message "+ message +" from "+receivedPacket.fromEndPoint);
        }
    }

    private void OnDestroy() {
        channel1.Disconnect();
        channel2.Disconnect();
    }
}
