using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    public Transform target;
    public float smoothSpeed = 0.125f;
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
            else
            {
                client = GameObject.Find($"player-{clientId}");
                if (client != null)
                {
                    target = client.transform;
                }    
            }
        }
        else
        {
            transform.position = target.position;
            transform.rotation = target.rotation;
            
            transform.position += offset;
            // var desiredPos = target.position + offset;
            // Vector3 velocity = Vector3.zero;
            // var smoothPos = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothSpeed);
            // transform.position = smoothPos;
            
            // transform.LookAt(target);
        }
    }
}
