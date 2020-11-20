﻿using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    public Transform target = null;
    public float smoothSpeed = 0.9f;
    public Vector3 offset;
    
    void LateUpdate()
    {
        if (target == null)
        {
            var clientId = GameManager.clientId;
            if(clientId == -1) return;
            
            var client = GameObject.Find($"client-{clientId}");
            if (client != null)
            {
                target = client.transform;
            }
        }
        else
        {
            var desiredPos = target.position + target.rotation * offset;
            Vector3 velocity = Vector3.zero;
            var smoothPos = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothSpeed);
            transform.position = smoothPos;
            
            transform.LookAt(target);
        }
    }
}