using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class CubeEntity
{

    public bool isBot;
    public int id;
    public Vector3 position;
    public Quaternion rotation;
    public GameObject GO;
    
    public float health = 50f;
    public float gunRange = 100f;
    public float gunDamage = 5f;
    
    // Connection info
    public int port;
    public int maxInputReceived;
    public int points;

    public CubeEntity(GameObject go, int id, bool isBot = false)
    {
        GO = go;
        this.id = id;
        port = Utils.GetPortFromId(id);
        maxInputReceived = -1;
        this.isBot = isBot;
        points = 0;
    }

    public CubeEntity(Vector3 position, Quaternion rotation, GameObject go)
    {
        this.position = position;
        this.rotation = rotation;
        GO = go;
        id = -1;
        port = -1;
        maxInputReceived = -1;
        isBot = false;
        points = 0;
    }
    
    public CubeEntity(CubeEntity original)
    {
        GO = original.GO;
        id = original.id;
        position = new Vector3(original.position.x, original.position.y, original.position.z);
        rotation = new Quaternion(original.rotation.x, original.rotation.y, original.rotation.z, original.rotation.w);
        port = original.port;
        maxInputReceived = original.maxInputReceived;
        points = original.points;
    }

    public void Serialize(BitBuffer buffer)
    {
        var position = GO.transform.position;
        var rotation = GO.transform.rotation;
        buffer.PutInt(maxInputReceived);
        buffer.PutFloat(position.x);
        buffer.PutFloat(position.y);
        buffer.PutFloat(position.z);
        buffer.PutFloat(rotation.w);
        buffer.PutFloat(rotation.x);
        buffer.PutFloat(rotation.y);
        buffer.PutFloat(rotation.z);
        buffer.PutFloat(health);
    }
    
    public void Deserialize(BitBuffer buffer) {
        position = new Vector3();
        rotation = new Quaternion();
        maxInputReceived = buffer.GetInt();
        position.x = buffer.GetFloat();
        position.y = buffer.GetFloat();
        position.z = buffer.GetFloat();
        rotation.w = buffer.GetFloat();
        rotation.x = buffer.GetFloat();
        rotation.y = buffer.GetFloat();
        rotation.z = buffer.GetFloat();
        health = buffer.GetFloat();
    }

    public static CubeEntity createInterpolated(CubeEntity previous, CubeEntity next, float t)
    {
        var cubeEntity = new CubeEntity(previous.GO, previous.id);
        cubeEntity.position = cubeEntity.position + Vector3.Lerp(previous.position, next.position, t);
        next.rotation = new Quaternion(next.rotation.x + 0.001f, next.rotation.y + 0.001f, next.rotation.z + 0.001f, next.rotation.w + 0.001f);
        var deltaRot= Quaternion.Lerp(previous.rotation, next.rotation, t);
        var rot = new Quaternion
        {
            x = previous.rotation.x + deltaRot.x,
            w = previous.rotation.w + deltaRot.w,
            y = previous.rotation.y + deltaRot.y,
            z = previous.rotation.z + deltaRot.z
        };
        cubeEntity.rotation = rot;
        cubeEntity.maxInputReceived = Mathf.Max(previous.maxInputReceived, next.maxInputReceived);
        return cubeEntity;
    }

    public void Apply()
    {
        GO.transform.position = position;
        GO.transform.rotation = rotation;
    }
    
    public HitPackage Shoot()
    {
        RaycastHit hit;
        var _transform = GO.transform;
        Vector3 originRay = _transform.position + _transform.forward * 0.6f;
        var ray = new Ray(originRay, _transform.forward);
        if (Physics.Raycast(ray, out hit, gunRange))
        {
            return new HitPackage(hit.transform.name, gunDamage);
        }

        return null;
    }
    
    public float TakeDamage(float amount)
    {
        health -= amount;
        return health;
    }
    
}
