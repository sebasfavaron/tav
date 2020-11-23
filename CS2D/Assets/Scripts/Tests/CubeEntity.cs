using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class CubeEntity
{
    // Serialized fields
    public Vector3 position;
    public Quaternion rotation;
    public float health = 100f;
    public UIManager uiManager;
    
    // Connection info
    public int port;
    public int points;

    // Extra
    public GameObject GO;
    public bool isBot;
    public int id;
    public float gunRange = 100f;
    public float gunDamage = 10f;
    private Cooldown shootingCooldown;
    
    public CubeEntity(GameObject go, int id, bool isBot = false)
    {
        GO = go;
        this.id = id;
        port = Utils.GetPortFromId(id);
        this.isBot = isBot;
        points = 0;
        shootingCooldown = new Cooldown(.2f, true);
        uiManager = null;
    }

    public CubeEntity(Vector3 position, Quaternion rotation, GameObject go)
    {
        this.position = position;
        this.rotation = rotation;
        GO = go;
        id = -1;
        port = -1;
        isBot = false;
        points = 0;
        shootingCooldown = new Cooldown(1f, true);
        uiManager = null;
    }
    
    public CubeEntity(CubeEntity original)
    {
        GO = original.GO;
        id = original.id;
        position = new Vector3(original.position.x, original.position.y, original.position.z);
        rotation = new Quaternion(original.rotation.x, original.rotation.y, original.rotation.z, original.rotation.w);
        port = original.port;
        points = original.points;
        shootingCooldown = original.shootingCooldown;
        uiManager = original.uiManager;
    }

    public void SetUIManager(UIManager uiManager, String name, float health)
    {
        this.uiManager = uiManager;
        this.uiManager.SetUI(name, health);
    }

    public void Serialize(BitBuffer buffer)
    {
        var position = GO.transform.position;
        var rotation = GO.transform.rotation;
        buffer.PutFloat(position.x);
        buffer.PutFloat(position.y);
        buffer.PutFloat(position.z);
        buffer.PutFloat(rotation.w);
        buffer.PutFloat(rotation.x);
        buffer.PutFloat(rotation.y);
        buffer.PutFloat(rotation.z);
        buffer.PutFloat(health);
        buffer.PutInt(points);
        
        if(uiManager != null) uiManager.UpdateHealthUI(health);
    }
    
    public void Deserialize(BitBuffer buffer) {
        position = new Vector3();
        rotation = new Quaternion();
        position.x = buffer.GetFloat();
        position.y = buffer.GetFloat();
        position.z = buffer.GetFloat();
        rotation.w = buffer.GetFloat();
        rotation.x = buffer.GetFloat();
        rotation.y = buffer.GetFloat();
        rotation.z = buffer.GetFloat();
        health = buffer.GetFloat();
        points = buffer.GetInt();
        
        if(uiManager != null) uiManager.UpdateHealthUI(health);
    }

    public static CubeEntity createInterpolated(CubeEntity previous, CubeEntity next, float t)
    {
        var cubeEntity = new CubeEntity(previous.GO, previous.id);
        cubeEntity.position = cubeEntity.position + Vector3.Lerp(previous.position, next.position, t);
        next.rotation = new Quaternion(next.rotation.x + 0.001f, next.rotation.y + 0.001f, next.rotation.z + 0.001f, next.rotation.w + 0.001f);
        var deltaRot= Quaternion.Lerp(previous.rotation, next.rotation, t);
        var rot = new Quaternion
        {
            x = previous.rotation.x + deltaRot.x,
            w = previous.rotation.w + deltaRot.w,
            y = previous.rotation.y + deltaRot.y,
            z = previous.rotation.z + deltaRot.z
        };
        cubeEntity.rotation = rot;
        return cubeEntity;
    }

    public void Apply()
    {
        GO.transform.position = position;
        GO.transform.rotation = rotation;
    }
    
    public HitPackage Shoot()
    {
        if (!shootingCooldown.IsOver()) return null;  // TODO: aca podria indicarle al usuario que no puede disparar todavia (o mostrar en la UI el remaining cooldown)
        shootingCooldown.RestartCooldown();

        RaycastHit hit;
        var _transform = GO.transform;
        Vector3 originRay = _transform.position + _transform.forward * 0.6f;
        var ray = new Ray(originRay, _transform.forward);
        if (Physics.Raycast(ray, out hit, gunRange))
        {
            Debug.Log($"shot {hit.transform.name}");
            return new HitPackage(hit.transform.name, gunDamage);
        }

        return null;
    }

    public void UpdateShootingCooldown()
    {
        shootingCooldown.UpdateCooldown();
    }
    
    public float TakeDamage(float amount)
    {
        health -= amount;
        return health;
    }
    
}
