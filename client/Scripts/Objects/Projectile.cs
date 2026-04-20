using UnityEngine;
using System.Collections.Generic; // 🌟 必须引入这个才能用 List

// 确保挂载物体有刚体和碰撞体
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("发送者状态（自动获取）")]
    protected Entity_Stats shooterStats;
    protected Entity shooterEntity;

    [Header("基类配置")]
    [Tooltip("子弹飞向哪个层级会造成伤害")]
    public LayerMask whatIsTarget;

    [Header("视觉与物理（自动获取）")]
    protected Rigidbody2D rb;
    [SerializeField] protected GameObject onHitVfx;

    // 子弹自身属性（由枪注入）
    [HideInInspector] public float speed;
    [HideInInspector] public WeaponDataSO weaponData;

    // ==========================================
    // 🌟 新增：子弹的“特效口袋”
    // ==========================================
    public List<WeaponEffectSO> activeEffects = new List<WeaponEffectSO>();

    protected bool targetGotHit;
    [Header("碰撞设置")]
    [Tooltip("哪些图层会被判定为障碍物？")]
    public LayerMask obstacleLayer;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().isTrigger = true;
    }

    // ==========================================
    // 🌟 核心修改：SetupProjectile 增加 activeEffects 参数
    // ==========================================
    public virtual void SetupProjectile(Entity shooter, WeaponDataSO data, Vector2 direction, LayerMask targetMask, List<WeaponEffectSO> weaponEffectsSnapshot, bool isSubProjectile = false)
    {
        shooterEntity = shooter;
        shooterStats = shooter.GetComponent<Entity_Stats>();
        weaponData = data;
        speed = data.bulletSpeed;
        float lifetime = data.bulletLifeTime;
        whatIsTarget = targetMask;

        // 🌟 将武器传来的“特效快照”保存到子弹自己身上
        if (weaponEffectsSnapshot != null)
        {
            activeEffects = new List<WeaponEffectSO>(weaponEffectsSnapshot);
        }

        rb.linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);

        // 🌟 触发生成特效时，遍历子弹自己的 activeEffects
        if (!isSubProjectile && activeEffects.Count > 0)
        {
            foreach (var effect in activeEffects)
            {
                effect.OnProjectileSpawned(this, shooterEntity, weaponData);
            }
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if ((obstacleLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            HitObstacle();
            return;
        }
        if (((1 << collision.gameObject.layer) & whatIsTarget.value) != 0)
        {
            ApplyDamageToTarget(collision);

            if (targetGotHit)
            {
                if (onHitVfx) Instantiate(onHitVfx, collision.transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
        }
    }

    protected virtual void ApplyDamageToTarget(Collider2D targetCollider, float damageScaleNumber = 1f)
    {
        IDamagable damagable = targetCollider.GetComponent<IDamagable>();
        if (damagable == null) return;

        if (shooterStats == null) { Debug.LogError($"{name} ShooterStats is null"); return; }

        AttackData attackData = shooterStats.GetAttackData(weaponData.damageScaleData);

        targetGotHit = damagable.TakeDamage(
            attackData.physicalDamage * damageScaleNumber,
            attackData.elementalDamage * damageScaleNumber,
            attackData.element,
            transform,
            attackData.isCrit);

        if (targetGotHit)
        {
            targetCollider.GetComponent<Entity_StatusHandler>()?.ApplyStatusEffect(attackData.element, attackData.effectData, shooterEntity);

            Entity targetEntity = targetCollider.GetComponentInParent<Entity>();

            // ==========================================
            // 🌟 核心修改：命中触发特效时，遍历子弹自己的 activeEffects
            // ==========================================
            if (activeEffects.Count > 0)
            {
                foreach (var effect in activeEffects)
                {
                    effect.OnTargetHit(shooterEntity, weaponData, targetEntity);
                }
            }
        }
    }

    private void HitObstacle()
    {
        Debug.Log("子弹撞到了障碍物，被阻挡了！");
        DestroyProjectile();
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }

    public virtual void Deflect(Entity newOwner, LayerMask newTargetMask, float speedMultiplier = 1.5f, float damageMultiplier = 1.5f)
    {
        shooterEntity = newOwner;
        shooterStats = newOwner.GetComponent<Entity_Stats>();
        whatIsTarget = newTargetMask;

        rb.linearVelocity = -rb.linearVelocity * speedMultiplier;

        float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        CancelInvoke();
        Destroy(gameObject, weaponData.bulletLifeTime);

        IProjectileBehavior[] behaviors = GetComponents<IProjectileBehavior>();
        foreach (var behavior in behaviors)
        {
            behavior.OnOwnershipTransferred(newOwner, newTargetMask);
        }
    }
}