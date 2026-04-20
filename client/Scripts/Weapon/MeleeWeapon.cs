using UnityEngine;

public class MeleeWeapon : Weapon
{
    [SerializeField] private Transform hitPoint;



    // 和远程武器一样，初始化时获取该打谁
    public void SetupWeaponLayers()
    {
        if (ownerEntity == null) return;
        var combat = ownerEntity.GetComponent<Player_Combat>();
        if (combat != null)
        {
            targetMask = combat.whatIsTarget;
        }
    }

    private void Start()
    {
        SetupWeaponLayers();
    }

    public override void ExecuteAttack()
    {
        if (Time.time < lastAttackTime + data.baseAttackCooldown) return;
        base.ExecuteAttack();

        // ==========================================
        // 🌟 2. 触发近战武器的专属特效（比如弹反 Effect_Parry！）
        // ==========================================
        if (data.weaponEffects != null)
        {
            foreach (var effect in data.weaponEffects)
            {
                // 这行代码会激活我们写好的弹反机制，张开判定网！
                effect.OnAttackExecute(ownerEntity, data, hitPoint, data.hitboxRadius);
            }
        }

        // ==========================================
        // 🌟 3. 传入 targetMask！只检测敌方图层，完美防止误伤和性能浪费
        // ==========================================
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(hitPoint.position, data.hitboxRadius, targetMask);

        foreach (Collider2D enemy in hitEnemies)
        {
            // 有了 LayerMask 过滤，这里其实连排除自己的代码都可以省了（不过留着双重保险也行）
            if (enemy.gameObject == ownerEntity.gameObject) continue;

            ApplyDamageToTarget(enemy);

            // 🌟 4. 如果有“击中特效”（比如吸血、挂毒），在这里触发
            if (data.weaponEffects != null)
            {
                // 获取敌方的 Entity 组件传入
                Entity enemyEntity = enemy.GetComponentInParent<Entity>();
                if (enemyEntity != null)
                {
                    foreach (var effect in data.weaponEffects)
                    {
                        effect.OnTargetHit(ownerEntity, data, enemyEntity);
                    }
                }
            }
        }
    }

    protected virtual void ApplyDamageToTarget(Collider2D targetCollider, float damageScaleNumber = 1f)
    {
        IDamagable damagable = targetCollider.GetComponentInParent<IDamagable>();
        if (damagable == null) return;

        Entity_Stats ownerStats = ownerEntity.GetComponent<Entity_Stats>();
        if (ownerStats == null)
        {
            Debug.LogError($"{name} 找不到 ownerStats！");
            return;
        }

        AttackData attackData = ownerStats.GetAttackData(data.damageScaleData);

        bool targetGotHit = damagable.TakeDamage(
            attackData.physicalDamage * damageScaleNumber,
            attackData.elementalDamage * damageScaleNumber,
            attackData.element,
            ownerEntity.transform,
            attackData.isCrit);

        if (targetGotHit)
        {
            targetCollider.GetComponentInParent<Entity_StatusHandler>()?.ApplyStatusEffect(
                attackData.element,
                attackData.effectData,
                ownerEntity);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (hitPoint == null || data == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hitPoint.position, data.hitboxRadius);
    }
}