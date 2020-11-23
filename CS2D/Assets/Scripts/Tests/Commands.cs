using UnityEngine;

public class Commands
{

    public int inputNumber;
    public float forwards;
    public float rotate;
    public HitPackage hitPackage;
    public bool jump;
    public float timestamp;

    public Commands(int inputNumber, float forwards, float rotate, HitPackage hitPackage, bool jump)
    {
        this.inputNumber = inputNumber;
        this.forwards = forwards;
        this.rotate = rotate;
        this.hitPackage = hitPackage ?? new HitPackage();
        this.jump = jump;
    }
    
    public Commands(int inputNumber, float forwards, float rotate, HitPackage hitPackage, bool jump, float timestamp)
    {
        this.inputNumber = inputNumber;
        this.forwards = forwards;
        this.rotate = rotate;
        this.hitPackage = hitPackage ?? new HitPackage();
        this.jump = jump;
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
        buffer.PutBit(jump);
        hitPackage.Serialize(buffer);
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        inputNumber = buffer.GetInt();
        forwards = buffer.GetFloat();
        rotate = buffer.GetFloat();
        jump = buffer.GetBit();
        hitPackage.Deserialize(buffer);
    }
}
