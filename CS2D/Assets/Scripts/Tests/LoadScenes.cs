using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScenes : MonoBehaviour
{
    public void LoadClientScene()
    {
        SceneManager.LoadScene("ClientScene");
    }
    
    public void LoadServerScene()
    {
        SceneManager.LoadScene("ServerScene");
    }
    
    public void LoadClientServerScene()
    {
        SceneManager.LoadScene("ClientServerScene(debugging)");
    }
}
