using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitPackage
{
    public string hitName;
    public float damage;

    public HitPackage(string hitName, float damage)
    {
        this.hitName = hitName;
        this.damage = damage;
    }

    public HitPackage()
    {
        hitName = "";
        damage = 0;
    }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutString(hitName);
        buffer.PutFloat(damage);
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        hitName = buffer.GetString();
        damage = buffer.GetFloat();
    }
}
