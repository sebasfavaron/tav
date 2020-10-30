using UnityEngine;

public class Commands
{

    public int inputNumber;
    public Vector3 moveVector;
    public float timestamp;

    public Commands(int inputNumber, Vector3 moveVector)
    {
        this.inputNumber = inputNumber;
        this.moveVector = moveVector;
    }
    
    public Commands(int inputNumber, Vector3 moveVector, float timestamp)
    {
        this.inputNumber = inputNumber;
        this.moveVector = moveVector;
        this.timestamp = timestamp;
    }

    public Commands() { }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(inputNumber);
        buffer.PutFloat(moveVector.x);
        buffer.PutFloat(moveVector.y);
        buffer.PutFloat(moveVector.z);
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        inputNumber = buffer.GetInt();
        moveVector = new Vector3(buffer.GetFloat(), buffer.GetFloat(), buffer.GetFloat());
    }
}
