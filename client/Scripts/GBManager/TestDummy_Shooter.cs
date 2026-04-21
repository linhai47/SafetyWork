using System.Collections.Generic; // 记得加上这个
using UnityEngine;

// 强制要求挂载实体和属性组件，完美融入你的底层框架
[RequireComponent(typeof(Entity))]
[RequireComponent(typeof(Entity_Stats))]
public class TestDummy_Shooter : MonoBehaviour
{
    [Header("射击配置")]
    [Tooltip("你想让木桩发射什么武器的子弹？")]
    public WeaponDataSO weaponData;
    public GameObject projectilePrefab;
    public Transform firePoint;
    [Tooltip("每隔几秒开一枪？")]
    public float fireInterval = 2.0f;

    [Header("测试目标")]
    [Tooltip("把你的玩家拖到这里，木桩会自动瞄准")]
    public Transform target;
    public LayerMask whatIsTarget;

    private float timer;
    private Entity myEntity;
    private Weapon weapon;

    private void Awake()
    {
        // 自动获取木桩自身的 Entity 身份，子弹才知道是谁开的枪
        myEntity = GetComponent<Entity>();

        // 🌟 修复 1：不要去拿 Player！直接尝试在自己（或者子物体）身上找 Weapon 组件
        weapon = GetComponentInChildren<Weapon>();
    }

    private void Update()
    {
        // 如果没配好参数，或者玩家死了/被销毁了，就不开火
        if (target == null || weaponData == null || projectilePrefab == null) return;

        timer += Time.deltaTime;

        // CD到了就开火
        if (timer >= fireInterval)
        {
            Shoot();
            timer = 0f; // 重置计时器
        }
    }

    private void Shoot()
    {
        // 1. 计算出枪口到玩家的精确方向向量
        Vector2 direction = (target.position - firePoint.position).normalized;

        // 2. 在枪口位置生成子弹
        GameObject projObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        // 3. 修正子弹的贴图朝向（让弹头对准玩家）
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projObj.transform.rotation = Quaternion.Euler(0, 0, angle);

        // 4. 调用你那套完美的 Projectile 初始化逻辑！
        Projectile proj = projObj.GetComponent<Projectile>();
        if (proj != null)
        {
            // ==========================================
            // 🌟 修复 2：极其安全的特效传递机制
            // ==========================================
            List<WeaponEffectSO> effectsToPass = new List<WeaponEffectSO>();

            if (weapon != null)
            {
                // 情况 A：木桩身上挂了真实的 Weapon 脚本，拿大写的 RuntimeEffects！
                effectsToPass = weapon.RuntimeEffects;
            }
            else if (weaponData != null && weaponData.weaponEffects != null)
            {
                // 情况 B：木桩就是个空壳，只配了 weaponData，那就用静态配置保底
                effectsToPass.AddRange(weaponData.weaponEffects);
            }

            // 把木桩自己作为 shooter 传进去，完美闭环！
            proj.SetupProjectile(myEntity, weaponData, direction, whatIsTarget, effectsToPass);
        }
    }
}