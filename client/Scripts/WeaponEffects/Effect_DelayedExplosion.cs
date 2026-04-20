using UnityEngine;

[CreateAssetMenu(fileName = "Effect_DelayedExplosion", menuName = "Weapon Effects/Delayed Explosion (延时爆炸)")]
public class Effect_DelayedExplosion : WeaponEffectSO 
{
    [Header("爆炸参数")]
    public float delayTime = 2f;
    public float explosionRadius = 2.5f;


    public LayerMask whatIsGround;

    public float damageMultiplier = 1.5f;

    [Header("纯视觉特效预制体")]
    public GameObject explosionVFXPrefab;

    public override void OnProjectileSpawned(Projectile proj, Entity wielder, WeaponDataSO sourceData)
    {
        if (proj == null) return;

        // 🌟 1. 呼叫老爸（基类），一键获取动态图层！
        LayerMask targetMask = GetDynamicTargetMask(wielder);

        ProjectileBehavior_DelayedExplosion behavior = proj.gameObject.AddComponent<ProjectileBehavior_DelayedExplosion>();

        // 🌟 2. 把提取出来的 targetMask 一起喂给行为脚本
        behavior.SetupBehavior(wielder, sourceData, this, targetMask);
    }


}