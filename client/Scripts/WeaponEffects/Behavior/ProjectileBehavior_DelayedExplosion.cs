using System.Collections;
using UnityEngine;

public class ProjectileBehavior_DelayedExplosion : MonoBehaviour, IProjectileBehavior
{
    private Entity attacker;                // 谁开的枪
    private WeaponDataSO weaponData;        // 枪的数据
    private Effect_DelayedExplosion effect; // 爆炸范围、时间的配置

    // 🌟 1. 新增：存储外部传来的动态目标图层
    private LayerMask targetMask;

    public LayerMask whatIsObstacle;
    private bool hasExploded = false;

    // ==========================================
    // 🌟 2. 接收第四个参数 targetMask！
    // ==========================================
    public void SetupBehavior(Entity wielder, WeaponDataSO sourceData, Effect_DelayedExplosion effectData, LayerMask targetMask)
    {
        this.attacker = wielder;
        this.weaponData = sourceData;
        this.effect = effectData;

        // 存下动态图层！
        this.targetMask = targetMask;

        // 子弹生成瞬间，开始死亡倒计时
        StartCoroutine(ExplosionCountdown());
    }

    private IEnumerator ExplosionCountdown()
    {
        yield return new WaitForSeconds(effect.delayTime);
        TriggerExplosion();
    }

    // （可选）如果打中敌人或墙壁，提前引爆！
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasExploded) return;

        // 1. 先检查撞到的是不是子弹
        // 如果撞到的是子弹（不管是谁发的），我们直接无视，不触发爆炸
        if (collision.GetComponent<Projectile>() != null)
        {
            return;
        }

        // 2. 只有不是子弹的东西，才去判定 LayerMask
        int objLayerMask = (1 << collision.gameObject.layer);
        bool hitTarget = (objLayerMask & targetMask) != 0;
        bool hitGround = (objLayerMask & effect.whatIsGround) != 0;

        if (hitTarget || hitGround)
        {
            TriggerExplosion();
        }
    }

    private void TriggerExplosion()
    {
        if (hasExploded) return;
        hasExploded = true;

        // 1. 播放纯视觉特效（只需要炸开好看就行，伤害不归它管）
        if (effect.explosionVFXPrefab != null)
        {
            // 随便设个 1 秒销毁这个视觉特效
            GameObject vfx = Instantiate(effect.explosionVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 1f);
        }

        // 2. 直接在此处结算 AOE 伤害！
        if (attacker != null)
        {
            Entity_Stats attackerStats = attacker.GetComponent<Entity_Stats>();

            if (attackerStats != null)
            {
                // ==========================================
                // 🌟 4. AOE 范围判定时，使用动态 targetMask！
                // ==========================================
                Collider2D[] hitTargets = Physics2D.OverlapCircleAll(transform.position, effect.explosionRadius, targetMask);

                foreach (var target in hitTargets)
                {
                    Entity_Health targetHealth = target.GetComponent<Entity_Health>();
                    if (targetHealth != null)
                    {
                        // 完美调用你的这 5 个硬核参数
                        bool isCrit;
                        float physDamage = attackerStats.GetPhyiscalDamage(out isCrit, effect.damageMultiplier);
                        ElementType element = attackerStats.nowElement;
                        float eleDamage = attackerStats.GetElementalDamage(element, effect.damageMultiplier);
                        Transform attackerTransform = attackerStats.transform;

                        targetHealth.TakeDamage(physDamage, eleDamage, element, attackerTransform, isCrit);
                    }
                }
            }
        }

        // 3. 自我毁灭
        Destroy(gameObject);
    }

    // 辅助功能：在 Scene 窗口画出爆炸的红圈，方便你调半径
    private void OnDrawGizmos()
    {
        if (effect != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, effect.explosionRadius);
        }
    }

    public void OnOwnershipTransferred(Entity newOwner, LayerMask newTargetMask)
    {
        // 认贼作父！
        this.attacker = newOwner;        // 以后用新主人的面板算伤害
        this.targetMask = newTargetMask; // 以后去炸新主人想打的敌人

        // 可选：你甚至可以重置爆炸倒计时，或者增加弹反后的爆炸倍率
        // this.effect.damageMultiplier *= 1.5f; 
    }
}