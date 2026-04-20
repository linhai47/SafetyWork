using UnityEngine;

public abstract class WeaponEffectSO : ScriptableObject
{
    [Header("类型限制")]
    [Tooltip("这个特效可以挂在哪些类型的武器上？（支持多选）")]
    public WeaponCategory allowedWeaponTypes = WeaponCategory.All;
    [Header("UI 表现")]
    public Sprite buffIcon;       // 状态栏显示的图标
    public bool showInUI = true;  // 是否需要在状态栏显示（有些暗改数据的被动可以隐藏）

    [Header("Effect 属性")]
    public float duration = 10f;
    protected LayerMask GetDynamicTargetMask(Entity wielder)
    {
        if (wielder == null) return 0;

        Player_Combat combat = wielder.GetComponent<Player_Combat>();
        if (combat != null)
        {
            return combat.whatIsTarget;
        }

        // 预留给未来的怪物：
        // Enemy_Combat enemyCombat = wielder.GetComponent<Enemy_Combat>();
        // if (enemyCombat != null) return enemyCombat.whatIsTarget;

        return 0; // 如果都没找到，返回 0 (Nothing)
    }
    public virtual void OnAttackExecute(Entity wielder, WeaponDataSO sourceData, Transform hitPoint, float radius)
    {
    }

    public virtual void OnTargetHit(Entity wielder, WeaponDataSO sourceData, Entity target)
    {
    }
    public virtual void OnProjectileSpawned(Projectile proj, Entity wielder, WeaponDataSO sourceData) { }
    public bool IsCompatibleWith(WeaponCategory targetCategory)
    {
        // 位运算魔法：检查 targetCategory 是否在 allowedWeaponTypes 的允许范围内
        return (allowedWeaponTypes & targetCategory) != 0;
    }

}