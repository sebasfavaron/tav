using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static string IPAddress = "127.0.0.1";
    public static int clientId = -1;

    private void Start()
    {
        DontDestroyOnLoad(transform.gameObject);
    }
}
