using UnityEngine;

public abstract class PoolObject<T> : MonoBehaviour{

    public abstract void Initialize(T t);

}