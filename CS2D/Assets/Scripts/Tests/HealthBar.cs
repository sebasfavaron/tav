using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider healthBar;

    private void Start()
    {
        healthBar = GetComponent<Slider>();
        healthBar.maxValue = 100f;
        healthBar.value = 30f;
    }

    public float SetHealth(float hp)
    {
        print($"setting hp {hp}");
        healthBar.value = hp;
        return hp;
    }
}