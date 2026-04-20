using System;
using System.Collections;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [Header("是否是本机操控角色")]
    public bool isLocalPlayer = true;

    public event Action OnFlipped;
    public Entity_VFX vfx { get; private set; }

    public Entity_Combat entity_Combat { get; private set; }

    public SpriteRenderer sr;
    public Rigidbody2D rb { get; private set; }


    public Animator anim;

    public bool facingRight = true;
    public int facingDir = 1;
    private bool isKnocked;
    private Coroutine knockbackCo;
    protected Coroutine SpeedUpCo;
    public StateMachine stateMachine;
    [Header("Collision detection")]
    public LayerMask whatisGround;
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private Transform primaryWallCheck;
    [SerializeField] private Transform secondaryWallCheck;
    public bool groundDetected { get; private set; }
    public bool wallDetected { get; private set; }

    public static event Action<Entity> onEnemySpawnedStatic;
    public event Action<Entity> onEnemyDied;

    public bool useTopAnimator = false;

    public Entity_Stats stats;

    [Header("武器挂载")]
    public Transform gunHoldPoint;
    public WeaponDataSO currentWeaponData;
    public Weapon currentWeaponInstance;
    public Transform bladeHoldePoint;

    protected virtual void Start()
    {
        onEnemySpawnedStatic?.Invoke(this);

    }

    protected virtual void Awake()
    {



        anim = GetComponentInChildren<Animator>();
        if (anim != null && anim.gameObject == gameObject)
        {
            // 找到的是自己，说明本体也有 Animator
            // 那就再手动找子物体
            Animator[] all = GetComponentsInChildren<Animator>(true);
            foreach (var a in all)
            {
                if (a.gameObject != gameObject)
                {
                    anim = a;
                    break;
                }
            }
        }
        stats = GetComponent<Entity_Stats>();

        rb = GetComponent<Rigidbody2D>();
        vfx = GetComponent<Entity_VFX>();
        sr = GetComponentInChildren<SpriteRenderer>();
        entity_Combat = GetComponentInChildren<Entity_Combat>();
        stateMachine = new StateMachine();
    }

    protected virtual void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }
         if (GameManager.Instance != null && !GameManager.Instance.IsGameActive())
        {
            // 如果你有刚体，强制让速度归零，防止带入之前的惯性
            rb.linearVelocity= Vector2.zero;
            return;
        }
        HandleCollisionDetection();
        stateMachine.UpdateActiveState();


    }

    public void SpeedUpEntity(float accMultiplier, float duration, bool canBeOverride = false)
    {
        if (SpeedUpCo != null)
        {
            if (canBeOverride)
                StopCoroutine(SpeedUpCo);
            else
                return;
        }

        SpeedUpCo = StartCoroutine(SpeedUpEntityCo(duration, accMultiplier));


    }
    protected virtual IEnumerator SpeedUpEntityCo(float duration, float accMultiplier)
    {

        yield return null;
    }
    public void CurrentStatteAnimationTrigger()
    {
        stateMachine.currentState.AnimationTrigger();

    }
    public virtual void EntityDeath()
    {
        onEnemyDied?.Invoke(this);
    }

    public virtual void EntityKnockOut()
    {

    }

    public void SetVelocity(float xVelocity, float yVelocity)
    {

        rb.linearVelocity = new Vector2(xVelocity, yVelocity);
        HandleFlip(xVelocity);
    }
    public void SetVelocityWithAcceleration(float targetX, float targetY, float acceleration)
    {
        Vector2 currentVelocity = rb.linearVelocity;
        Vector2 targetVelocity = new Vector2(targetX, targetY);

        // 计算需要变化的速度向量
        Vector2 velocityChange = targetVelocity - currentVelocity;

        // 限制每帧最大速度变化量
        Vector2 clampedChange = Vector2.ClampMagnitude(velocityChange, acceleration * Time.fixedDeltaTime);

        rb.linearVelocity = currentVelocity + clampedChange;

        HandleFlip(targetX);
    }
    public virtual void HandleFlip(float xVelocity)
    {
        if (facingRight == true && xVelocity < 0)
        {
            Flip();
        }
        else if (facingRight == false && xVelocity > 0)
        {
            Flip();
        }

    }
    public void ReceiveKnockback(Vector2 knockback, float duration)
    {
        if (knockbackCo != null) StopCoroutine(knockbackCo);

        knockbackCo = StartCoroutine(KnockbackCo(knockback, duration));
    }
    private IEnumerator KnockbackCo(Vector2 knockback, float duration)
    {
        isKnocked = true;
        rb.linearVelocity = knockback;
        yield return new WaitForSeconds(duration);
        rb.linearVelocity = Vector2.zero;
        isKnocked = false;
    }

    public void Flip()
    {
        facingDir = facingDir * -1;
        transform.Rotate(0, 180, 0);

        facingRight = !facingRight;

        OnFlipped?.Invoke();
    }

    public void HandleCollisionDetection()
    {
        groundDetected = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatisGround);

        if (secondaryWallCheck != null)
        {
            wallDetected = Physics2D.Raycast(primaryWallCheck.position, facingDir * Vector2.right, wallCheckDistance, whatisGround)
                && Physics2D.Raycast(secondaryWallCheck.position, facingDir * Vector2.right, wallCheckDistance, whatisGround);


        }
        else wallDetected = Physics2D.Raycast(primaryWallCheck.position, facingDir * Vector2.right, wallCheckDistance, whatisGround);


    }

    protected virtual void OnDrawGizmos()
    {
        Gizmos.DrawLine(groundCheck.position, groundCheck.position + new Vector3(0, -groundCheckDistance));
        Gizmos.DrawLine(primaryWallCheck.position, primaryWallCheck.position + new Vector3(wallCheckDistance * facingDir, 0));

        if (secondaryWallCheck != null) Gizmos.DrawLine(secondaryWallCheck.position, secondaryWallCheck.position + new Vector3(wallCheckDistance * facingDir, 0));


    }


    public virtual void EntityRespawn()
    {
        // 1. 中止所有位移和物理协程，防止死前的击飞力带到下辈子
        if (knockbackCo != null)
        {
            StopCoroutine(knockbackCo);
            knockbackCo = null;
        }
        if (SpeedUpCo != null)
        {
            StopCoroutine(SpeedUpCo);
            SpeedUpCo = null;
        }
        isKnocked = false;

        // 2. 彻底清空物理速度
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // 3. 强制重置动画机 (非常关键！)
        // Rebind() 会直接把 Animator 拔掉电源重启，强制回到你设置的 Default State（通常是 Idle）
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        // 4. 确保碰撞体是开启的 (如果你在死亡时关掉了它的话)
        Collider2D cd = GetComponent<Collider2D>();
        if (cd != null)
        {
            cd.enabled = true;
        }

        // 5. 触发重生的事件 (可用于后续通知 UI 或特效)
        // onEnemySpawnedStatic?.Invoke(this); // 如果你需要复活时也触发生成特效，可以解开这句

        Debug.Log($"【系统】{gameObject.name} 基础物理与动画已重置，完成复活！");
    }
}
