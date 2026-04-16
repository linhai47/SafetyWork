using UnityEngine;

public class Entity_AnimationTriggers : MonoBehaviour
{
    private Entity entity;
    private Entity_Combat entityCombat;
    public bool animationOver;
    private RelayChatClient networkClient;
    protected virtual void Awake()
    {
        entity = GetComponentInParent<Entity>();
        entityCombat = GetComponentInParent<Entity_Combat>();
        networkClient = GetComponentInParent<RelayChatClient>();
        animationOver = false;
    }
    public void AnimationEvent_FireWeapon()
    {
        // 获取玩家手里的枪
        Weapon currentWeapon = GetComponentInParent<Player>().currentWeaponInstance;
     
        if (currentWeapon != null)
        {
           
            // 调用枪的攻击方法
            currentWeapon.ExecuteAttack();
            if (networkClient != null)
            {
                // 告诉服务器：我开火了！(这里可以顺便带上武器名字或ID)
                Debug.Log("I fire");
                _ = networkClient.SendChat($"ACTION:ATTACK_WITH_{currentWeapon.name}");
            }
            else
            {
                // 容错处理：如果是在纯单机测试场景里没挂网络组件，给出提示但不报错
                Debug.LogWarning("未找到 RelayChatClient，当前为纯单机开火模式。");
            }
        }
    }
    private void CurrentStateTrigger()
    {
        entity.CurrentStatteAnimationTrigger();
    }

    private void AttackTrigger()
    {
        entityCombat.PerformAttack();
    }
}
