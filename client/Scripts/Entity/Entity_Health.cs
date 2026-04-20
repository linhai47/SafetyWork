using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Entity_Health : MonoBehaviour, IDamagable
{
    private Entity_Stats entityStats;
    private Entity_DropManager dropManager;
    private Slider healthBar;
    [Header("on Damage Knockback")]
  

    [SerializeField] private Vector2 onDamageKnockback = new Vector2(1.5f, 2f);

    [Header("on Heavy Damage Knockback")]
    [SerializeField]
    private float heavyKnockBackDuration = .5f;
    [SerializeField] private float heavyKnockBackThreshold = .5f;
    [SerializeField] private Vector2 onHeavyDamageKnockback = new Vector2(7, 7);

    [Header("Health Regen")]
    [SerializeField] private float regenInterval = 1;
    [SerializeField] private bool canRegenerateHealth = true;
    public event Action OnTakingDamage;
    public event Action OnHealthUpdate;
    public event Action OnPercentageUpdate;
    private Entity entity;
    private Entity_VFX entityVfx;

    [Header("Smash Bros Mechanics")]
    public float damagePercentage = 0f;    // 当前累计百分比 (代替原有的 currentHealth)
    public float weight = 100f;            // 角色重量 (越重越难飞)
    public float knockbackGrowth = 50f;    // 击飞放大系数 (全局控制击飞爽快感)
    private bool isInvincible = false;     // 复活时的无敌状态保护

    [Header("Base Knockback (0%时的基础击飞)")]
    [SerializeField] private float knockBackDuration = .2f;
    [SerializeField] private Vector2 baseKnockback = new Vector2(2f, 2f);


    public float currentHealth;

    public float lastDamageTaken { get; private set; }
    public bool isDead { get; private set; }
    protected bool canTakeDamage = true;

    public bool canBeKnockout = false;
    public float GetcurrentHealth() => currentHealth;
    public float GetDamagePercentage() => damagePercentage;

    [Header("击杀特效")]
    public GameObject fireworksDeathPrefab;
    protected virtual void Awake()
    {
        entity = GetComponent<Entity>();
        entityVfx = GetComponent<Entity_VFX>();
        entityStats = GetComponent<Entity_Stats>();
        //healthBar = GetComponentInChildren<Slider>();
        dropManager = GetComponent<Entity_DropManager>();
        SetupHealth();
    }

    protected virtual void Start()
    {
        OnPercentageUpdate += () =>
        {
            if (UIManager.Instance != null)
            {
                // 通过 entity 获取 Tag（Player1 或 Player2）
                // 如果你的 Entity 脚本里有储存 tag 的变量就用那个
                UIManager.Instance.UpdatePercentageUI(entity.tag, damagePercentage);
              
            }
        };

        // 刚进入游戏时先强制刷新一次 0%
        OnPercentageUpdate?.Invoke();

    }

    private void SetupHealth()
    {
        if (entityStats != null)
        {

            currentHealth = entityStats.GetMaxHealth();
            //OnHealthUpdate += UpdateHealthBar;

            //UpdateHealthBar();
            InvokeRepeating(nameof(RegenerateHealth), 0, regenInterval);
        }
    }


    public void EnableHealthBar(bool enable) => healthBar?.transform.parent.gameObject.SetActive(enable);
    private void UpdateHealthBar()
    {
        if (healthBar == null && healthBar.transform.parent.gameObject.activeSelf == false) return;
        healthBar.value = GetHealthPercent();
    }
    public float GetHealthPercent() => currentHealth / entityStats.GetMaxHealth();
    private void RegenerateHealth()
    {

        if (canRegenerateHealth == false) return;
        float regenAmount = entityStats.resources.healthRegen.GetValue();
        IncreaseHealth(regenAmount);
    }
    public void IncreaseHealth(float healAmount)
    {
        if (isDead) return;


        float newHealth = currentHealth + healAmount;


        currentHealth = Mathf.Min(newHealth, entityStats.GetMaxHealth());
        OnHealthUpdate?.Invoke();

    }

    public void ReduceHealth(float damage)
    {

        currentHealth -= damage;
        OnHealthUpdate?.Invoke();
        entityVfx?.PlayOnDamageVfx();
        if (currentHealth <= 0) Die();

    }
    protected virtual void Die()
    {
        if (canBeKnockout)
        {
            entity?.EntityKnockOut();
            return;
        }
        isDead = true;
        canTakeDamage = false;
    
        entity?.EntityDeath();
        dropManager?.DropItems();

    }

    public virtual bool TakeDamage(float damage,float ElementalDamage,ElementType element,  Transform damageDealer, bool isCrit = false)
    {
        if (isDead || !canTakeDamage) return false;
        if (AttackEvaded())
        {
            Debug.Log($"{gameObject.name} evaded the attack!");
            return false;
        }
        //HitStopController.Instance.StopTime(0.02f, 0.0f);
        Entity_Stats attackerStats = damageDealer.GetComponent<Entity_Stats>();
        float armorReduction = attackerStats != null ? attackerStats.GetArmorReduction() : 0;
        float mitigation = entityStats != null ? entityStats.GetArmorMitigation(armorReduction) : 0;
        float resistance = entityStats != null ? entityStats.GetElementalResistance(element) : 0;

        float physicalDamageTaken = damage*(1 - mitigation);

        float elementalDamageTaken = ElementalDamage *( 1 - resistance);
        float finalDamage = physicalDamageTaken + elementalDamageTaken;
        TakeKnockback(damageDealer, physicalDamageTaken);
        //ReduceHealth(physicalDamageTaken + elementalDamageTaken);

        lastDamageTaken = physicalDamageTaken + elementalDamageTaken;

        float scale =UnityEngine.Random.Range(.9f, 1.1f);
        finalDamage *= scale;
        AddDamagePercentage(finalDamage);

        // 2. 触发大乱斗动态击飞
        TakeSmashKnockback(damageDealer, finalDamage);
        lastDamageTaken = finalDamage;
        OnTakingDamage?.Invoke();
        return true;
    }
    private void AddDamagePercentage(float damage)
    {
        damagePercentage += damage;
        OnPercentageUpdate?.Invoke(); // 通知 UI 更新百分比
        entityVfx?.PlayOnDamageVfx();

        // 注意：这里删除了 if (health <= 0) Die()，因为大乱斗只有出界才算死
    }
    private void TakeSmashKnockback(Transform damageDealer, float finalDamage)
    {
        // 判定伤害来源方向 (1 为右，-1 为左)
        int directionX = transform.position.x > damageDealer.position.x ? 1 : -1;

        // 构造基础方向 (通常大乱斗的击飞都有一个向上的 Y 轴力，防止贴地滑行)
        Vector2 knockbackDir = new Vector2(directionX, 1f).normalized;

        // 【核心大乱斗公式】
        // 最终击飞力 = 基础击飞力 + (当前百分比 * 本次伤害 * 放大系数 / 体重)
        float percentageFactor = (damagePercentage * finalDamage * knockbackGrowth) / weight;
        float finalForce = baseKnockback.magnitude + percentageFactor;

        Vector2 finalKnockbackVector = knockbackDir * finalForce;

        // 随着力度增大，被击飞的硬直时间也延长
        float dynamicDuration = knockBackDuration + (percentageFactor * 0.01f);

        entity?.ReceiveKnockback(finalKnockbackVector, dynamicDuration);
    }
    private void TakeKnockback(Transform damageDealer, float finalDamage)
    {
        Vector2 knockback = CalculateKnockback(finalDamage, damageDealer);
        float duration = CalculateDuration(finalDamage);
        entity?.ReceiveKnockback(knockback, duration);
    }
    private Vector2 CalculateKnockback(float damage,Transform damageDealer)
    {
        int direction = transform.position.x > damageDealer.position.x ? 1 : -1;
        Vector2 knockback = IsHeavyDamage(damage) ? onHeavyDamageKnockback : onDamageKnockback;
        knockback.x = knockback.x * direction;
        return knockback;


    }
    private bool AttackEvaded()
    {
        if (entityStats == null)
            return false;
        else
            return UnityEngine.Random.Range(0, 100) < entityStats.GetEvasion();



    }
    public void SetCanTakeDamage(bool canTakeDamage) => this.canTakeDamage = canTakeDamage;
    private float CalculateDuration(float damage)
    {
        return IsHeavyDamage(damage) ? heavyKnockBackDuration : knockBackDuration;


    }


    private bool IsHeavyDamage( float damage)
    {
        
        return damage/ entityStats.GetMaxHealth() > heavyKnockBackThreshold;

    }

    public void UpdateCameraWeight(Transform playerTransform, float weight)
    {
        // 1. 获取组件
        var targetGroup = FindFirstObjectByType<CinemachineTargetGroup>();
        if (targetGroup == null) return;

        // 2. 找到这个玩家在列表里的索引
        // 在 3.x 中，targetGroup.Targets 是一个 List<CinemachineTargetGroup.Target>
        int index = -1;
        for (int i = 0; i < targetGroup.Targets.Count; i++)
        {
            if (targetGroup.Targets[i].Object == playerTransform)
            {
                index = i;
                break;
            }
        }

        // 3. 执行更新
        if (weight > 0)
        {
            if (index == -1) // 没在列表里，就加进去
            {
                targetGroup.Targets.Add(new CinemachineTargetGroup.Target
                {
                    Object = playerTransform,
                    Weight = weight,
                    Radius = 1f
                });
            }
            else // 已经在列表里，就更新权重
            {
                // 注意：因为 Target 是结构体，需要整体重新赋值
                var t = targetGroup.Targets[index];
                t.Weight = weight;
                targetGroup.Targets[index] = t;
            }
        }
        else
        {
            // 如果权重设为 0，直接从列表里移除（这样相机就彻底不看他了）
            if (index != -1)
            {
                targetGroup.Targets.RemoveAt(index);
            }
        }
    }
    public virtual void OutOfBoundsDie(Vector3 respawnPosition)
    {
        if (isDead) return;

        isDead = true;

        // 1. 播放死亡高爆粒子特效（你的 HSV 极简特效代码保持不变）
        if (fireworksDeathPrefab != null)
        {
            GameObject fxInstance = Instantiate(fireworksDeathPrefab, transform.position, Quaternion.identity);
            ParticleSystem ps = fxInstance.GetComponent<ParticleSystem>();

            if (ps != null)
            {
                var colModule = ps.colorOverLifetime;
                colModule.enabled = true;

                float randomHue = UnityEngine.Random.value;
                Color randomVibrantColor = Color.HSVToRGB(randomHue, 0.8f, 1.0f);

                Gradient randomGradient = new Gradient();
                randomGradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(randomVibrantColor, 0.15f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 0.7f), new GradientAlphaKey(0.0f, 1.0f) }
                );

                colModule.color = new ParticleSystem.MinMaxGradient(randomGradient);
            }
        }

        canTakeDamage = false;
        var targetGroup = FindFirstObjectByType<CinemachineTargetGroup>();

        entity?.EntityDeath();      // 播放你原有的死亡动画/特效

        // 🌟🌟🌟 新增逻辑：通知 GameManager 扣命！
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDeath(gameObject.tag);
        }

        // 🌟🌟🌟 检查剩余命数，决定生死
        bool canRespawn = false;
        if (GameManager.Instance != null)
        {
            if (gameObject.tag == "Player1" && GameManager.Instance.p1Stocks > 0) canRespawn = true;
            if (gameObject.tag == "Player2" && GameManager.Instance.p2Stocks > 0) canRespawn = true;
        }
        else
        {
            // 如果场景里没放 GameManager（比如测试场景），默认允许复活
            canRespawn = true;
        }

        if (canRespawn)
        {
            // 还有命，执行复活流程
            StartCoroutine(DelayCameraWeightRoutine(1f));
            StartCoroutine(RespawnRoutine(respawnPosition));
        }
        else
        {
            //// 没命了，Game Over！
            //// 1. 取消该角色的相机权重（防止镜头一直盯着一个死人看）
            //// 假设你有 UpdateCameraWeight 方法，没有的话可以直接操作 targetGroup
            //// UpdateCameraWeight(this.transform, 0f); 

            //// 2. 彻底隐藏模型
            //if (entity != null && entity.GraphicsChild != null)
            //{
            //    entity.GraphicsChild.gameObject.SetActive(false);
            //}

            Debug.Log($"{gameObject.tag} 彻底出局！");
        }
    }
    private IEnumerator DelayCameraWeightRoutine(float delayTime)
    {
        //  避坑指南：这里强烈建议使用 WaitForSecondsRealtime！
        // 如果你之前采纳了我的建议，在死亡时修改了 Time.timeScale 做了慢动作，
        // 普通的 WaitForSeconds 会跟着变慢，而 Realtime 哪怕时间静止了，0.5秒后也会准时执行。
        yield return new WaitForSecondsRealtime(delayTime);

        // 等待结束，更新相机权重，让镜头平滑移走
        UpdateCameraWeight(this.transform, 0f);
    }
    private IEnumerator RespawnRoutine(Vector3 spawnPos)
    {
        // 等待 2 秒 (播放死亡动画的时间)
        yield return new WaitForSeconds(2.0f);

        //  1. 绝对优先：先传送回出生点，并强制清除物理惯性！
        // 必须在 isDead = false 之前做，防止在出界区瞬间连死两遍！
        transform.position = spawnPos;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        //  2. 镜头切回来
        UpdateCameraWeight(this.transform, 1f);

        //  3. 再重置状态开关
        damagePercentage = 0f;
        isDead = false;  // 现在安全了，玩家已经在台上了
        canTakeDamage = true;
        OnPercentageUpdate?.Invoke();

        // 4. 复活无敌时间 (3秒)
        StartCoroutine(InvincibilityRoutine(3.0f));
        entity?.EntityRespawn();
    }

    private IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;
        // 这里可以调用 entityVfx 播放闪烁效果
        yield return new WaitForSeconds(duration);
        isInvincible = false;
    }
}
