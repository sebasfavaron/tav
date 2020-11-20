using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericPool<T> where T : GenericPoolableObject, new() {

	private Stack<T> freeObjects = new Stack<T>();

	public T Obtain() {
		return freeObjects.Count == 0 ? new T() : freeObjects.Pop();
	}

	public void Free(T poolObject) {
		freeObjects.Push(poolObject);
		poolObject.Reset();
	}
}
