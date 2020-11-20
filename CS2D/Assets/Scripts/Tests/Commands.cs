using UnityEngine;

public class Commands
{

    public int inputNumber;
    public float forwards;
    public float rotate;
    public HitPackage hitPackage;
    public float timestamp;

    public Commands(int inputNumber, float forwards, float rotate, HitPackage hitPackage)
    {
        this.inputNumber = inputNumber;
        this.forwards = forwards;
        this.rotate = rotate;
        this.hitPackage = hitPackage ?? new HitPackage();
    }
    
    public Commands(int inputNumber, float forwards, float rotate, HitPackage hitPackage, float timestamp)
    {
        this.inputNumber = inputNumber;
        this.forwards = forwards;
        this.rotate = rotate;
        this.hitPackage = hitPackage ?? new HitPackage();
        this.timestamp = timestamp;
    }

    public Commands()
    {
        hitPackage = new HitPackage();
    }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(inputNumber);
        buffer.PutFloat(forwards);
        buffer.PutFloat(rotate);
        hitPackage.Serialize(buffer);
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        inputNumber = buffer.GetInt();
        forwards = buffer.GetFloat();
        rotate = buffer.GetFloat();
        hitPackage.Deserialize(buffer);
    }
}
