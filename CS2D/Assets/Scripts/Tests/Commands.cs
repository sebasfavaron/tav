using UnityEngine;

public class Commands
{

    public int time;
    public bool up, down, left, right, space;
    public float timestamp;

    public Commands(int time, bool up, bool down, bool left, bool right, bool space)
    {
        this.time = time;
        this.up = up;
        this.down = down;
        this.left = left;
        this.right = right;
        this.space = space;
    }
    
    public Commands(int time, bool up, bool down, bool left, bool right, bool space, float timestamp)
    {
        this.time = time;
        this.up = up;
        this.down = down;
        this.left = left;
        this.right = right;
        this.space = space;
        this.timestamp = timestamp;
    }

    public Commands() { }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(time);
        buffer.PutBit(up);
        buffer.PutBit(down);
        buffer.PutBit(left);
        buffer.PutBit(right);
        buffer.PutBit(space);
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        time = buffer.GetInt();
        up = buffer.GetBit();
        down = buffer.GetBit();
        left = buffer.GetBit();
        right = buffer.GetBit();
        space = buffer.GetBit();
    }
}
