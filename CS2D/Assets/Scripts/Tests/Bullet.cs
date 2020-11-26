using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet
{
    public Vector3 position;
    public Vector3 forward;
    public Quaternion rotation;

    public Bullet(Vector3 position, Vector3 forward, Quaternion rotation)
    {
        this.position = position;
        this.forward = forward;
        this.rotation = rotation;
    }

    public void Cast()
    {
        if(GameManager.laserPrefab != null) Object.Instantiate(GameManager.laserPrefab, position + forward * 0.6f, rotation);
    }
}
