using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

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
            kv.Value.Serialize(buffer);
        }
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        packetNumber = buffer.GetInt();
        foreach (var kv in cubeEntities)
        {
            if (buffer.HasRemaining())  // In case new players haven't been added to the client yet
            {
                kv.Value.Deserialize(buffer);
            }
        }
    }
    
    public static Snapshot CreateInterpolated(Snapshot previous, Snapshot next, float t, int clientId)
    {
        Dictionary<int, CubeEntity> cubeEntities = new Dictionary<int, CubeEntity>();
        foreach (var kv in previous.cubeEntities)
        {
            if (kv.Value.id != clientId) // Do not interpolate client
            {
                // Debug.Log(kv.Value.id);
                cubeEntities[kv.Key] = CubeEntity.createInterpolated(previous.cubeEntities[kv.Key], next.cubeEntities[kv.Key], t);
            }
        }
        return new Snapshot(cubeEntities, -1);
    }

    public void Apply()
    {
        foreach (var cubeEntity in cubeEntities)
        {
            cubeEntity.Value.Apply();
        }
    }

}
