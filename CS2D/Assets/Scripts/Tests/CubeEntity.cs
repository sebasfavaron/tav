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
    public GameObject cubeGameObject;

    // Connection info
    public int port;
    public int maxInputReceived;
    
    public CubeEntity(GameObject cubeGameObject, int id, bool isBot = false)
    {
        this.cubeGameObject = cubeGameObject;
        this.id = id;
        this.port = Utils.GetPortFromId(id);
        this.maxInputReceived = -1;
        this.isBot = isBot;
    }

    public CubeEntity(Vector3 position, Quaternion rotation, GameObject cubeGameObject)
    {
        this.position = position;
        this.rotation = rotation;
        this.cubeGameObject = cubeGameObject;
        this.port = -1;
        this.maxInputReceived = -1;
        this.isBot = false;
    }
    
    public CubeEntity(CubeEntity original)
    {
        this.cubeGameObject = original.cubeGameObject;
        this.id = original.id;
        this.position = new Vector3(original.position.x, original.position.y, original.position.z);
        this.rotation = new Quaternion(original.rotation.x, original.rotation.y, original.rotation.z, original.rotation.w);
        this.port = -1;
        this.maxInputReceived = -1;
    }

    public void Serialize(BitBuffer buffer)
    {
        var position = cubeGameObject.transform.position;
        var rotation = cubeGameObject.transform.rotation;
        buffer.PutInt(maxInputReceived);
        buffer.PutFloat(position.x);
        buffer.PutFloat(position.y);
        buffer.PutFloat(position.z);
        buffer.PutFloat(rotation.w);
        buffer.PutFloat(rotation.x);
        buffer.PutFloat(rotation.y);
        buffer.PutFloat(rotation.z);
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
    }

    public static CubeEntity createInterpolated(CubeEntity previous, CubeEntity next, float t)
    {
        var cubeEntity = new CubeEntity(previous.cubeGameObject, previous.id);
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
        cubeGameObject.transform.position = position;
        cubeGameObject.transform.rotation = rotation;
    }
    
}
