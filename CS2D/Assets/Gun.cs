using System;
using UnityEngine;

public class Gun : MonoBehaviour
{

    public float damage = 10f;
    public float range = 100f;
    public SimulationClient simulationClient;

    private void Start()
    {
        simulationClient = FindObjectOfType<SimulationClient>();
    }

    public void Shoot(Transform _transform)
    {
        RaycastHit hit;
        Vector3 originRay = _transform.position + _transform.forward * 0.6f;
        var ray = new Ray(originRay, _transform.forward);
        if (Physics.Raycast(ray, out hit, range))
        {
            simulationClient.TakeDamage(hit.transform.name, damage);
        }
    }
}