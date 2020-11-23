using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Text text;
    public HealthBar healthBar;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetUI(string name, float health)
    {
        text.text = name;
        healthBar.GetComponent<Slider>().value = health;
    }

    public void UpdateHealthUI(float health)
    {
        healthBar.GetComponent<Slider>().value = health;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
