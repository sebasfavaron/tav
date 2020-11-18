using UnityEngine;

public class Commands
{

    public int inputNumber;
    public float forwards;
    public float rotate;
    public float timestamp;
    public bool shoot;

    public Commands(int inputNumber, float forwards, float rotate, bool shoot)
    {
        this.inputNumber = inputNumber;
        this.forwards = forwards;
        this.rotate = rotate;
        this.shoot = shoot;
    }
    
    public Commands(int inputNumber, float forwards, float rotate, bool shoot, float timestamp)
    {
        this.inputNumber = inputNumber;
        this.forwards = forwards;
        this.rotate = rotate;
        this.shoot = shoot;
        this.timestamp = timestamp;
    }

    public Commands() { }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(inputNumber);
        buffer.PutFloat(forwards);
        buffer.PutFloat(rotate);
        buffer.PutBit(shoot);
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        inputNumber = buffer.GetInt();
        forwards = buffer.GetFloat();
        rotate = buffer.GetFloat();
        shoot = buffer.GetBit();
    }
}
