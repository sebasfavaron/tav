using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class Snapshot
{
    public Dictionary<int, CubeEntity> cubeEntities;
    public int packetNumber;

    public Snapshot(Dictionary<int, CubeEntity> cubeEntities, int packetNumber)
    {
        this.cubeEntities = cubeEntities;
        this.packetNumber = packetNumber;
    }
    
    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(packetNumber);
        foreach (var kv in cubeEntities)
        {
            buffer.PutInt(kv.Key);
            kv.Value.Serialize(buffer);
        }
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        packetNumber = buffer.GetInt();
        while (buffer.HasRemaining())  // In case new players haven't been added to the client yet
        {
            var key = buffer.GetInt();
            cubeEntities[key].Deserialize(buffer);
        }
    }
    
    public static Snapshot CreateInterpolated(Snapshot previous, Snapshot next, float t, int clientId)
    {
        Dictionary<int, CubeEntity> cubeEntities = new Dictionary<int, CubeEntity>();
        foreach (var kv in previous.cubeEntities)
        {
            if (kv.Value.id != clientId) // Do not interpolate client
            {
                // var step = Vector3.Distance(kv.Value.cubeGameObject.transform.position,
                //     next.cubeEntities[kv.Key].cubeGameObject.transform.position);
                // if (step > 0f) Debug.Log(step);
                // if (step > 0.5f) Debug.Log("something wrong");
                cubeEntities[kv.Key] = CubeEntity.createInterpolated(kv.Value, next.cubeEntities[kv.Key], t);
            }
        }
        return new Snapshot(cubeEntities, -1);
    }

    public void Apply(int clientId)
    {
        foreach (var cubeEntity in cubeEntities)
        {
            if (cubeEntity.Value.id != clientId) // Do not interpolate client
            {
                cubeEntity.Value.Apply();
            }
        }
    }

}
