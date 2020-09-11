using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net;

public class Packet : GenericPoolableObject {

	private static GenericPool<Packet> pool = new GenericPool<Packet>();
	private static System.Object poolLock = new System.Object();

	public const int BUFFER_CAPACITY = 1024 * 1024;

	public BitBuffer buffer = new BitBuffer(new MemoryStream(BUFFER_CAPACITY));
	public IPEndPoint fromEndPoint;

	public static Packet Obtain() {
		Packet packet = null;
		lock (poolLock) {
			packet = pool.Obtain();
		}
		return packet;
	}

	public void Reset() {
		buffer.Clear();
	}

	public void Free() {
		lock (poolLock) {
			pool.Free(this);
		}
	}
}