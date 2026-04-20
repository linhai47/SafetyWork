using System.Collections;
using System.Collections.Generic; // 别忘了引入 List
using UnityEngine;

// 动态挂载到子弹身上的外挂脚本
public class ProjectileBehavior_HoverSplit : MonoBehaviour
{
    private Projectile proj;
    private Rigidbody2D rb;
    private Entity shooter;
    private WeaponDataSO sourceData;
    private Effect_HoverSplit config;
    private GameObject dynamicSplitPrefab;

    public void SetupBehavior(Entity shooter, WeaponDataSO sourceData, Effect_HoverSplit config, GameObject prefabToUse)
    {
        this.shooter = shooter;
        this.sourceData = sourceData;
        this.config = config;

        this.proj = GetComponent<Projectile>();
        this.rb = GetComponent<Rigidbody2D>();
        this.dynamicSplitPrefab = prefabToUse;

        StartCoroutine(HoverAndSplitRoutine());
    }

    private IEnumerator HoverAndSplitRoutine()
    {
        float timer = 0f;
        Vector2 startVelocity = rb.linearVelocity;

        if (startVelocity == Vector2.zero) yield break;

        while (timer < config.slowDuration)
        {
            timer += Time.deltaTime;
            rb.linearVelocity = Vector2.Lerp(startVelocity, Vector2.zero, timer / config.slowDuration);
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        ExecuteSplit(startVelocity.normalized);
        Destroy(gameObject);
    }

    private void ExecuteSplit(Vector2 baseDir)
    {
        if (dynamicSplitPrefab == null) return;

        float angleStep = 360f / config.splitCount;

        // 🌟 1. 提前从母子弹身上拿出它当下的“目标层级”和“特效快照”
        LayerMask inheritedMask = proj.whatIsTarget;
        List<WeaponEffectSO> inheritedEffects = proj.activeEffects;

        for (int i = 0; i < config.splitCount; i++)
        {
            float angle = i * angleStep;
            Vector2 splitDir = Quaternion.Euler(0, 0, angle) * baseDir;

            GameObject splitObj = Instantiate(dynamicSplitPrefab, transform.position, Quaternion.identity);
            float rotAngle = Mathf.Atan2(splitDir.y, splitDir.x) * Mathf.Rad2Deg;
            splitObj.transform.rotation = Quaternion.Euler(0, 0, rotAngle);

            Projectile newProj = splitObj.GetComponent<Projectile>();
            if (newProj != null)
            {
                // ========================================================
                // 🌟 2. 完美继承！传给小子弹的是母子弹的数据，而不是去找 shooter 拿
                // ========================================================
                newProj.SetupProjectile(shooter, sourceData, splitDir, inheritedMask, inheritedEffects, true);

                // ========================================================
                // 🌟 3. 遍历母子弹带的动态特效（而不是去读死板的 sourceData）
                // ========================================================
                if (inheritedEffects != null && inheritedEffects.Count > 0)
                {
                    foreach (var weaponEffect in inheritedEffects)
                    {
                        // ⚠️ 极其重要防套娃机制：跳过自己
                        if (weaponEffect == this.config)
                        {
                            continue;
                        }

                        // 将其他特效挂载给这颗新生的小子弹
                        weaponEffect.OnProjectileSpawned(newProj, shooter, sourceData);
                    }
                }
            }
        }
    }
}