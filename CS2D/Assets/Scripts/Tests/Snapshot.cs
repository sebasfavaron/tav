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
    public List<Bullet> bullets;

    public Snapshot(Dictionary<int, CubeEntity> cubeEntities, int packetNumber, List<Bullet> bullets)
    {
        this.cubeEntities = cubeEntities;
        this.packetNumber = packetNumber;
        this.bullets = bullets;
    }
    
    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(bullets.Count);
        bullets.ForEach(bullet =>
        {
            buffer.PutFloat(bullet.position.x);
            buffer.PutFloat(bullet.position.y);
            buffer.PutFloat(bullet.position.z);
            
            buffer.PutFloat(bullet.forward.x);
            buffer.PutFloat(bullet.forward.y);
            buffer.PutFloat(bullet.forward.z);

            buffer.PutFloat(bullet.rotation.x);
            buffer.PutFloat(bullet.rotation.y);
            buffer.PutFloat(bullet.rotation.z);
            buffer.PutFloat(bullet.rotation.w);
        });
        
        buffer.PutInt(packetNumber);
        foreach (var kv in cubeEntities)
        {
            buffer.PutInt(kv.Key);
            kv.Value.Serialize(buffer);
        }
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        var bulletCount = buffer.GetInt();
        for (int i = 0; i < bulletCount; i++)
        {
            var pos = new Vector3(buffer.GetFloat(), buffer.GetFloat(), buffer.GetFloat());
            var fwd = new Vector3(buffer.GetFloat(), buffer.GetFloat(), buffer.GetFloat());
            var rot = new Quaternion(buffer.GetFloat(), buffer.GetFloat(), buffer.GetFloat(), buffer.GetFloat());
            bullets.Add(new Bullet(pos, fwd, rot));
        }
        
        packetNumber = buffer.GetInt();
        while (buffer.HasRemaining())
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
        return new Snapshot(cubeEntities, -1, new List<Bullet>());
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
