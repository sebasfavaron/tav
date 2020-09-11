using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
{
	private static T instance;
	public static T Instance {
		get {
			if (instance == null) {
				instance = FindObjectOfType<T> ();
			}
			return instance;
		}
	}

	public virtual void Awake ()
	{
		if (instance == null) {
			instance = this as T;
		} else {
			Destroy (gameObject);
		}
	}

	public virtual void OnDestroy() {
		if (instance == this) {
			instance = null;
		}
	}
}