using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitPackage
{
    public string hitName;
    public int shooterId;
    public float damage;

    public HitPackage(string hitName, int shooterId, float damage)
    {
        this.hitName = hitName;
        this.shooterId = shooterId;
        this.damage = damage;
    }
}
