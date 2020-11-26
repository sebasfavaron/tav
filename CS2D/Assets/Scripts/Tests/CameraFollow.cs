using System.Runtime.InteropServices;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    public Transform camera;
    public Transform target = null;
    public float smoothSpeed = 0.3f;
    public Vector3 offset;
    private bool clientConnected = false;
    
    void LateUpdate()
    {
        if (!clientConnected)
        {
            var clientId = GameManager.clientId;
            if(clientId == -1) return;
            
            var client = GameObject.Find($"client-{clientId}");
            if (client != null)
            {
                target = client.transform;
                camera = transform;
                clientConnected = true;
            }
        }
        else
        {
            var desiredPos = target.position + target.rotation * offset;
            Vector3 velocity = Vector3.zero;
            var smoothPos = Vector3.SmoothDamp(camera.position, desiredPos, ref velocity, smoothSpeed);
            camera.position = smoothPos;
            
            camera.LookAt(target);
        }
    }
}
