using UnityEngine;

public interface IProjectileBehavior
{
    // 当子弹易主（被弹反）时调用
    void OnOwnershipTransferred(Entity newOwner, LayerMask newTargetMask);
}