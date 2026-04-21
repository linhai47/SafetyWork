using System;

[Serializable]
public struct PlayerInputCmd
{
    public int seq;
    public float moveX;
    public float moveY;
    public bool jumpPressed;
    public bool attackPressed;
    public float aimX;
    public float aimY;
}