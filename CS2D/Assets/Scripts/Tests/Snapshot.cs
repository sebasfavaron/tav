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
        int cnt = 0;
        foreach (var kv in previous.cubeEntities)
        {
            if (kv.Value.id != clientId) // Do not interpolate client
            {
                // Debug.Log(kv.Value.id);
                if(kv.Value.cubeGameObject.name.Contains("client")) Debug.Log("GOT YOU! createinterp");
                cubeEntities[kv.Key] = CubeEntity.createInterpolated(kv.Value, next.cubeEntities[kv.Key], t);
            }
            else
            {
                cnt++;
                // Debug.Log($"dont interpolate me, im client");
            }
        }
        // Debug.Log(cnt);
        return new Snapshot(cubeEntities, -1);
    }

    public void Apply(int clientId)
    {
        foreach (var cubeEntity in cubeEntities)
        {
            if (cubeEntity.Value.id != clientId) // Do not interpolate client
            {
                if(cubeEntity.Value.cubeGameObject.name.Contains("client")) Debug.Log("GOT YOU! apply");
                cubeEntity.Value.Apply();
            }
        }
    }

}
