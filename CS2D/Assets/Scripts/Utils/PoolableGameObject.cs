using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PoolableGameObject : MonoBehaviour {

	private bool active = false;
	public bool Active {
		get {
			return active;
		}
		set {
			active = value;
		}
	}

	[HideInInspector] public GameObjectPool pool;

	public Action onReturned;

	public void ReturnToPool() {
		active = false;
		gameObject.SetActive(false);
		pool.ReturnObject(this);
		if (onReturned != null) {
			onReturned();
		}
	}

	public void ReturnToPoolDelayed(float seconds) {
		StartCoroutine(ReturnAfterDelay(seconds));
	}

	private IEnumerator ReturnAfterDelay(float seconds) {
		yield return new WaitForSeconds(seconds);

		if (active) {
			ReturnToPool();
		}
	}
}
