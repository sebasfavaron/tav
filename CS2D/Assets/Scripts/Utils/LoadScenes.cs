using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadScenes : MonoBehaviour
{
    public void LoadClientScene()
    {
        var input = GameObject.FindWithTag("ClientIPInput");
        if (input != null)
        {
            var ip = input.GetComponent<InputField>().text;
            if(!ip.Equals("")) GameManager.IPAddress = ip;
        }
        
        SceneManager.LoadScene("ClientScene");
    }
    
    public void LoadServerScene()
    {
        SceneManager.LoadScene("ServerScene");
    }
}
