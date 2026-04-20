using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Effect_Parry", menuName = "Weapon Effects/Melee/Parry (终极动态图层版)")]
public class Effect_Parry : WeaponEffectSO
{
    [Header("冷却配置")]
    public float cooldown = 3.0f;

    [Header("弹反判定参数")]
    public float parryWindowFrames = 0.2f;

    // 🌟 删除了所有的 LayerMask！把选择权完全交还给使用者！

    [Header("反击强化参数")]
    public float deflectSpeedMultiplier = 1.5f;
    public float hitStopDuration = 0.15f;

    [Header("视觉效果")]
    public GameObject parrySuccessVFX;

    private Dictionary<Entity, float> nextAvailableTimeDict = new Dictionary<Entity, float>();

    public override void OnAttackExecute(Entity wielder, WeaponDataSO sourceData, Transform hitPoint, float radius)
    {
        if (nextAvailableTimeDict.TryGetValue(wielder, out float nextTime))
        {
            if (Time.time < nextTime) return;
        }

        if (wielder.GetComponent<Behavior_Parry>() != null) return;

        // ==========================================
        // 🌟 按照你的思路：在这里获取持有者的 whatIsTarget
        // ==========================================
        LayerMask dynamicTargetMask = 0;
        Player_Combat combat = wielder.GetComponent<Player_Combat>();
        if (combat != null)
        {
            dynamicTargetMask = combat.whatIsTarget;
        }

        Behavior_Parry behavior = wielder.gameObject.AddComponent<Behavior_Parry>();

        // 只需要传一个 dynamicTargetMask 过去！它既是“抓捕网”，也是“反击目标”
        behavior.SetupBehavior(wielder, this, hitPoint, radius, dynamicTargetMask);
    }

    public void StartCooldown(Entity wielder)
    {
        nextAvailableTimeDict[wielder] = Time.time + cooldown;
    }
}