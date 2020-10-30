using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Snapshot
{
    public List<CubeEntity> cubeEntities;
    public int packetNumber;
    public int inputNumber;  // last input processed

    public Snapshot(int packetNumber, int inputNumber, List<CubeEntity> cubeEntities)
    {
        this.cubeEntities = cubeEntities;
        this.packetNumber = packetNumber;
        this.inputNumber = inputNumber;
    }
    
    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(packetNumber);
        buffer.PutInt(inputNumber);
        cubeEntities.ForEach(c => c.Serialize(buffer));
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        packetNumber = buffer.GetInt();
        inputNumber = buffer.GetInt();
        cubeEntities.ForEach(c =>
        {
            if (buffer.HasRemaining())  // In case new players haven't been added to the client yet
            {
                c.Deserialize(buffer);
            }
        });
    }
    
    public static Snapshot CreateInterpolated(Snapshot previous, Snapshot next, float t)
    {
        List<CubeEntity> cubeEntities = new List<CubeEntity>();
        for (int i = 0; i < previous.cubeEntities.Count; i++)
        {
            cubeEntities.Add(CubeEntity.createInterpolated(previous.cubeEntities[i], next.cubeEntities[i], t));
        }
        return new Snapshot(-1, -1, cubeEntities);
    }

    public void Apply(int clientId)
    {
        cubeEntities.ForEach(c =>
        {
            if (c.id != clientId) // Do not interpolate client
            {
                c.Apply();
            }
        });
    }

}
