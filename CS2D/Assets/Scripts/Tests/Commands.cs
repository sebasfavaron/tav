using System.Collections.Generic;
using UnityEngine;

public class Commands
{

    public int inputNumber;
    public float forwards;
    public float rotate;
    public List<HitPackage> hitPackages;
    public bool shoot;
    public float timestamp;

    public Commands(int inputNumber, float forwards, float rotate, List<HitPackage> hitPackages, bool shoot)
    {
        this.inputNumber = inputNumber;
        this.forwards = forwards;
        this.rotate = rotate;
        this.hitPackages = hitPackages ?? new List<HitPackage>();
        this.shoot = shoot;
    }
    
    public Commands(int inputNumber, float forwards, float rotate, List<HitPackage> hitPackages, bool shoot, float timestamp)
    {
        this.inputNumber = inputNumber;
        this.forwards = forwards;
        this.rotate = rotate;
        this.hitPackages = hitPackages ?? new List<HitPackage>();
        this.shoot = shoot;
        this.timestamp = timestamp;
    }

    public Commands()
    {
        hitPackages = new List<HitPackage>();
    }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(inputNumber);
        buffer.PutFloat(forwards);
        buffer.PutFloat(rotate);
        buffer.PutBit(shoot);
        buffer.PutInt(hitPackages.Count);
        hitPackages.ForEach(h => h.Serialize(buffer));
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        inputNumber = buffer.GetInt();
        forwards = buffer.GetFloat();
        rotate = buffer.GetFloat();
        shoot = buffer.GetBit();
        var hitCount = buffer.GetInt();
        for (int i = 0; i < hitCount; i++)
        {
            var newHitPackage = new HitPackage();
            newHitPackage.Deserialize(buffer);
            hitPackages.Add(newHitPackage);
        }
    }
}
