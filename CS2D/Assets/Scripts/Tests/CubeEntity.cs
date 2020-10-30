using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeEntity
{

    public int id;
    public Vector3 position;
    public Quaternion rotation;
    public GameObject cubeGameObject;

    public CubeEntity(GameObject cubeGameObject, int id)
    {
        this.cubeGameObject = cubeGameObject;
        this.id = id;
    }

    public CubeEntity(Vector3 position, Quaternion rotation, GameObject cubeGameObject)
    {
        this.position = position;
        this.rotation = rotation;
        this.cubeGameObject = cubeGameObject;
    }
    
    public CubeEntity(CubeEntity original)
    {
        this.cubeGameObject = original.cubeGameObject;
        this.id = original.id;
        this.position = new Vector3(original.position.x, original.position.y, original.position.z);
        this.rotation = new Quaternion(original.rotation.x, original.rotation.y, original.rotation.z, original.rotation.w);
    }

    public void Serialize(BitBuffer buffer)
    {
        position = cubeGameObject.transform.position;
        rotation = cubeGameObject.transform.rotation;
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
        var deltaRot=  Quaternion.Lerp(previous.rotation, next.rotation, t);
        var rot = new Quaternion();
        rot.x = previous.rotation.x + deltaRot.x;
        rot.w = previous.rotation.w + deltaRot.w;
        rot.y = previous.rotation.y + deltaRot.y;
        rot.z = previous.rotation.z + deltaRot.z;
        cubeEntity.rotation = rot;
        return cubeEntity;
    }

    public void Apply()
    {
        cubeGameObject.transform.position = position;
        cubeGameObject.transform.rotation = rotation;
    }
    
}
