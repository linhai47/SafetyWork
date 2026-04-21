using System;

[Serializable]
public class MatchSnapshot
{
    public int tick;      // 服务器当前的帧号
    public float posX;    // 权威 X 坐标
    public float posY;

}