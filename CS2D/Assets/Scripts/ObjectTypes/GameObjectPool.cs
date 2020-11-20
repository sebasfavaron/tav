using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Profiling;

public class GameObjectPool : MonoBehaviour {

    private List<PoolableGameObject> items = new List<PoolableGameObject>();

    protected virtual void Awake() {
    	foreach (Transform child in transform) {
    		PoolableGameObject pgo = child.GetComponent<PoolableGameObject>();
    		pgo.pool = this;
    		items.Add(pgo);
    	}

        if(items.Count == 0) {
            Debug.LogError("ObjectPool has no base object "+gameObject.name);
        }
    }

	public GameObject GetObject(Transform parent, bool resetLocalPositionRotation = true) {
		Profiler.BeginSample("GameObjectPool::GetObject");
		PoolableGameObject pgo = null;
        GameObject go = null;

        if (items.Count == 1) {
        	Clone(1);
        }

        if (items.Count > 0) {
        	pgo = items[items.Count - 1];        
        	go = pgo.gameObject;        	   
        }

        if(pgo == null) {
			Profiler.EndSample();
        	return null;
        }

		Transform pgoTransform = go.transform;
        pgoTransform.parent = parent;
        if (resetLocalPositionRotation) {
        	pgoTransform.localPosition = Vector3.zero;
        	pgoTransform.localRotation = Quaternion.identity;
        }
		go.SetActive(true);        
        pgo.Active = true;

        items.RemoveAt(items.Count - 1);
        Profiler.EndSample();
        return go;
    }

    public void Clone(int copies) {
		for (int i = 0; i < copies; i++) {
    		GameObject clone = Instantiate(items[0].gameObject, transform);
			PoolableGameObject pgo = clone.GetComponent<PoolableGameObject>();
    		pgo.pool = this;
    		items.Add(pgo);
		}
    }

    public void ReturnObject(PoolableGameObject pgo) {
    	pgo.transform.parent = transform;
        items.Add(pgo);
    }

    public void TransferObjectsTo(GameObjectPool pool) {
		for (int i = 0; i < items.Count; i++) {
    		PoolableGameObject pgo = items[i];
    		pgo.pool = pool;
    		pgo.ReturnToPool();
		}
		items.Clear();
    }
}