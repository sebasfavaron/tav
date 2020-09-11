using UnityEngine;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

public class Channel {

	private const int CONNECTION_CLOSED_CODE = 10054;

	private UdpClient udpClient;
	private System.Object bufferLock = new System.Object();
	private List<Packet> packetBuffer = new List<Packet>();

	public Channel(string ip, int receivePort, int sendPort) {
		try {
			udpClient = new UdpClient(receivePort);
			if (ip != null) {
				udpClient.Connect(new IPEndPoint(IPAddress.Parse(ip), sendPort));
			}
			Thread receiveThread = new Thread(Receive);
			receiveThread.Start();
		} catch (Exception e) {
			Debug.Log("could not connect socket: " + e.Message);
		}
	}

    public Channel(int port) {
        try {
            udpClient = new UdpClient(port);
            Thread receiveThread = new Thread(Receive);
            receiveThread.Start();
        } catch (Exception e) {
            Debug.Log("could not connect socket: " + e.Message);
        }
    }

	public void Disconnect() {
		if (udpClient != null) {
			Debug.Log("socket closed");
			udpClient.Close();
			udpClient = null;
		}
	}

	public Packet GetPacket() {
		Packet packet = null;
		lock (bufferLock) {
			if (packetBuffer.Count > 0) {
				packet = packetBuffer[0];
				packetBuffer.RemoveAt(0);
			}
		}
		return packet;
	}

	private void Receive() {
		IPEndPoint endPoint = new IPEndPoint(IPAddress.None, 0);
		EndPoint remoteEndPoint = (EndPoint) endPoint;
		while (udpClient != null) {
			try {
				Packet packet = Packet.Obtain();
				int byteCount = udpClient.Client.ReceiveFrom(packet.buffer.GetBuffer().GetBuffer(), ref remoteEndPoint);
				packet.buffer.SetAvailableByteCount(byteCount);
				packet.fromEndPoint = remoteEndPoint as IPEndPoint;
				lock (bufferLock) {
					packetBuffer.Add(packet);
				}
			} catch (SocketException e) {
				if (e.ErrorCode != CONNECTION_CLOSED_CODE) {
					Debug.Log("SocketException while reading from socket: " + e + " (" + e.ErrorCode + ")");
				}
			} catch (Exception e) {
				Debug.Log("Exception while reading from socket: " + e);
			}
		}
	}

	public void Send(Packet packet, IPEndPoint endPoint = null) {		
		if (udpClient != null) {	
			if (endPoint == null) {
				udpClient.Send (packet.buffer.GetBuffer().GetBuffer(), packet.buffer.GetAvailableByteCount ());
			} else {
				udpClient.Send (packet.buffer.GetBuffer().GetBuffer(), packet.buffer.GetAvailableByteCount (), endPoint);
			}
		}
	}

	private string ByteArrayToString(byte[] data, int length) {
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < length; i++) {
			sb.Append(data[i]).Append(", ");
		}
		return sb.ToString();
	}
}
