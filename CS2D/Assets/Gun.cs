using System;
using UnityEngine;

public class Gun : MonoBehaviour
{

    public float damage = 5f;
    public float range = 10f;
    public SimulationServer simulationServer;

    private void Start()
    {
        simulationServer = FindObjectOfType<SimulationServer>();
    }
}