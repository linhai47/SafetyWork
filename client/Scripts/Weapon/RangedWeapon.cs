using System.Collections.Generic;
using UnityEngine;

public class RangedWeapon : Weapon
{
    [SerializeField] private Transform firePoint;

    WeaponProceduralAnimator animator;
    private Player_Combat combat;

    // 🌟 注意这里：把 private 改成 protected（如果你原来写的是 private 的话）
    // 不过 Unity 的生命周期方法通常用 protected virtual 会更好扩展
    protected override void Start()
    {
        // ==========================================
        // 🌟 极其重要：必须调用 base.Start()！
        // 否则父类 Weapon.cs 里把初始特效塞进 runtimeEffects 的逻辑就不会执行了！
        // ==========================================
        base.Start();

        animator = GetComponentInChildren<WeaponProceduralAnimator>();
        Player_Combat playerCombat = ownerEntity.GetComponent<Player_Combat>();
        if (playerCombat != null)
        {
            targetMask = playerCombat.whatIsTarget;
        }
    }

    public override void ExecuteAttack()
    {
        // 1. 检查 CD
        if (Time.time < lastAttackTime + data.baseAttackCooldown) return;

        // 2. 调用父类的攻击逻辑（更新冷却时间，并触发攻击瞬间的动态特效）
        base.ExecuteAttack();

        // 触发程序化动画
        if (animator != null) animator.ApplyRecoil();

        int bulletCount = data.bulletsPerShot > 0 ? data.bulletsPerShot : 1; // 防呆

        for (int i = 0; i < bulletCount; i++)
        {
            // 1. 计算随机偏移角度
            float randomAngle = Random.Range(-data.spreadAngle, data.spreadAngle);

            // 2. 计算子弹实际的飞行方向 (将枪口正前方旋转 randomAngle 度)
            Vector2 fireDirection = Quaternion.Euler(0, 0, randomAngle) * firePoint.right;

            // 3. 计算子弹的初始旋转角度 (让子弹的贴图也朝着飞行的方向)
            Quaternion bulletRotation = firePoint.rotation * Quaternion.Euler(0, 0, randomAngle);

            // 4. 生成并发射！
            GameObject newBullet = Instantiate(this.data.bulletPrefab, firePoint.position, bulletRotation);
            Projectile script = newBullet.GetComponent<Projectile>();

            if (script != null)
            {
                // ==========================================
                // 🌟 核心修改：删除了乱码 v，并把 runtimeEffects 传给子弹！
                // ==========================================
                script.SetupProjectile(ownerEntity, data, fireDirection, targetMask, runtimeEffects);
            }
        }
    }
}