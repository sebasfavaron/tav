using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cooldown
{
    private float resetValue;
    private float RemainingSeconds;

    public Cooldown(float seconds, bool startAtZero = false)
    {
        if (startAtZero)
        {
            RemainingSeconds = 0;
        }
        else
        {
            RemainingSeconds = seconds;
        }
        resetValue = seconds;
    }

    public bool IsOver()
    {
        return RemainingSeconds <= 0;
    }

    public void RestartCooldown()
    {
        RemainingSeconds = resetValue;
    }

    public float RemainingCooldown()
    {
        return RemainingSeconds;
    }

    public void UpdateCooldown()
    {
        if(!IsOver()) RemainingSeconds -= Time.deltaTime;
    }

    public void EndCooldown()
    {
        RemainingSeconds = 0;
    }

}