using UnityEngine;

[CreateAssetMenu(fileName = "Effect_HoverSplit", menuName = "Weapon Effects/Hover Split (悬停分裂)")]
public class Effect_HoverSplit : WeaponEffectSO
{
    [Header("分裂参数")]
    public float slowDuration = 0.5f;
    public int splitCount = 3;

    [Header("可选：特殊分裂子弹")]

    public GameObject overrideSplitPrefab;

    public override void OnProjectileSpawned(Projectile proj, Entity wielder, WeaponDataSO sourceData)
    {
        if (proj == null) return;

        // 核心：给这颗普通的子弹，强行塞入一个临时的“悬停分裂AI”脚本！
        ProjectileBehavior_HoverSplit behavior = proj.gameObject.AddComponent<ProjectileBehavior_HoverSplit>();

        // 🌟 动态决定预制体：优先使用覆盖的，否则直接用当前武器数据里的！
        GameObject prefabToUse = overrideSplitPrefab != null ? overrideSplitPrefab : sourceData.bulletPrefab;

        // 把数据和动态获取的预制体一起喂给它
        behavior.SetupBehavior(wielder, sourceData, this, prefabToUse);
    }
}