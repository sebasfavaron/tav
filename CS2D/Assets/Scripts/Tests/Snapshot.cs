using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snapshot
{
    private List<CubeEntity> cubeEntities;
    public int packetNumber;

    public Snapshot(int packetNumber, List<CubeEntity> cubeEntities)
    {
        this.cubeEntities = cubeEntities;
        this.packetNumber = packetNumber;
    }
    
    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(packetNumber);
        cubeEntities.ForEach(c => c.Serialize(buffer));
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        packetNumber = buffer.GetInt();
        cubeEntities.ForEach(c => c.Deserialize(buffer));
    }
    
    public static Snapshot CreateInterpolated(Snapshot previous, Snapshot next, float t)
    {
        List<CubeEntity> cubeEntities = new List<CubeEntity>();
        for (int i = 0; i < previous.cubeEntities.Count; i++)
        {
            cubeEntities.Add(CubeEntity.createInterpolated(previous.cubeEntities[i], next.cubeEntities[i], t));
        }
        return new Snapshot(-1, cubeEntities);
    }

    public void Apply()
    {
        cubeEntities.ForEach(c => c.Apply());
    }

}
