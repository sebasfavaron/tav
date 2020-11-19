using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public float health = 50f;

    public float TakeDamage(float amount)
    {
        health -= amount;
        return health;
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
