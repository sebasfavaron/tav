using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class GeneralUIManager : MonoBehaviour
{
    public Text pointsText;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetUI(int points)
    {
        pointsText.text = $"Points: {points}";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}