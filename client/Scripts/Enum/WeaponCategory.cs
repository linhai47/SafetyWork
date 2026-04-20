using UnityEngine;

// [Flags] 标签是灵魂！它允许你进行“多选”
[System.Flags]
public enum WeaponCategory
{
    None = 0,
    Melee = 1 << 0,       // 近战类 (1)
    Ranged = 1 << 1,      // 远程类 (2)
    Magic = 1 << 2,       // 魔法类 (4)
    Shield = 1 << 3,      // 盾牌类 (8)

    // 你可以组合它们：
    All = ~0              // 通用（所有武器都能用，比如加攻击力的Buff）
}