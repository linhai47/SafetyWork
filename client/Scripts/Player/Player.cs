using System;
using System.Collections;
using System.Globalization;
using PixelCrushers.DialogueSystem;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : Entity
{
    //public Player_VFX vfx { get; private set; }
    public static Player instance;
    public Entity_Health health { get; private set; }
    public Entity_StatusHandler statusHandler { get; private set; }
    public Player_Combat combat { get; private set; }



    public Inventory_Player inventory { get; private set; }
    public PlayerInputSet input {  get; private set; }

    public static event Action OnPlayerDeath;
    public Player_IdleState idleState { get; private set; }
    public Player_MoveState moveState { get; private set; }

    public Player_JumpState jumpState { get; private set; }

    public Player_FallState fallState { get; private set; }

    public Player_WallJumpState wallJumpState { get; private set; }

    public Player_WallSlideState wallSlideState { get; private set; }

    public Player_DeadState deadState { get; private set; }

    public Player_DashState dashState { get; private set; }

    public Player_BasicAttackState basicAttackState { get; private set; }

    public Player_JumpAttackState jumpAttackState { get; private set; }

    public Player_CastingState castingState { get; private set; }

    public ProximitySelector selector { get; private set; }

    public Player_QuestManager questManager { get; private set; }

    public Player_Exp playerExp { get; private set; }
    public UI ui { get; private set; }
    [Header("Movement Details")]

    public float moveSpeed = 1.0f;
    public float jumpForce = 15f;
    public Vector2 wallJumpForce;
    public float dashDuration = .3f;
    public float dashSpeed = 25f;

    public Vector2 moveInput { get; private set; }
    public Vector2 mousePosition { get; private set; }
    [Range(0, 1)]
    public float inAirMoveMultiplier = .7f;
    [Range(0, 1)]
    public float wallSlideSlowMultiplier = .7f;

    [Header("Attack Details")]
    public Vector2[] attackVelocity;
    public Vector2 JumpAttackVelocity;

    public float attackVelocityDuration = .1f;
    public float comboResetTime = 1;
    private Coroutine queuedAttackCo;


    private PlayerCombatController weaponController;
    protected override void Awake()
    {
        base.Awake();
        instance = this;
        input = new PlayerInputSet();
        stats = GetComponent<Player_Stats>();
        //vfx = GetComponent<Player_VFX>();
        ui = FindAnyObjectByType<UI>();
        statusHandler = GetComponent<Entity_StatusHandler>();
        health = GetComponent<Entity_Health>();
        combat = GetComponent<Player_Combat>();
        //ui.SetupControlsUI(input);
        inventory = GetComponent<Inventory_Player>();
        selector = GetComponent<ProximitySelector>();
        questManager = GetComponent<Player_QuestManager>();
        playerExp = GetComponent<Player_Exp>();
        weaponController = GetComponent<PlayerCombatController>();

        idleState = new Player_IdleState(this,stateMachine, "idle");
        moveState = new Player_MoveState(this, stateMachine, "move");
        jumpState = new Player_JumpState(this, stateMachine, "jumpfall");
        fallState = new Player_FallState(this, stateMachine, "jumpfall");
        wallJumpState = new Player_WallJumpState(this, stateMachine, "jumpfall");
        wallSlideState = new Player_WallSlideState(this, stateMachine, "wallSlide");
        dashState = new Player_DashState(this, stateMachine, "dash");
        deadState = new Player_DeadState(this, stateMachine, "dead");
        basicAttackState = new Player_BasicAttackState(this, stateMachine, "basicAttack");
        jumpAttackState = new Player_JumpAttackState(this, stateMachine, "jumpAttack");
        castingState = new Player_CastingState(this, stateMachine, "casting");

    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
    public void OnPlayerSpawned(int playerID)
    {
        int targetLayer;

        // 如果是房主（Player 1）
        if (playerID == 1)
        {
            targetLayer = LayerMask.NameToLayer("Player1");
        }
        // 如果是客户端（Player 2）
        else
        {
            targetLayer = LayerMask.NameToLayer("Player2");
        }

        // 把这个角色全身上下的皮都换成对应的 Layer
        SetLayerRecursively(gameObject, targetLayer);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
        if (currentWeaponData != null) EquipWeapon(currentWeaponData);

    }
    public void EquipWeapon(WeaponDataSO newWeaponData)
    {
        // 🌟 新增 1：在替换新武器数据前，先将【旧武器】的属性从面板上扣除
        if (currentWeaponData != null)
        {
            // 注意：这里需要把 stats 强转为 Player_Stats 或直接调用基类 Entity_Stats 的方法
            // 如果你的 stats 变量类型就是 Player_Stats，直接调用即可
            ((Entity_Stats)stats).RemoveWeaponModifiers(currentWeaponData);
        }

        currentWeaponData = newWeaponData;

        // 1. 如果手上已经有武器了，先销毁旧的
        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance.gameObject);
        }

        Transform targetHoldPoint = newWeaponData.isRanged ? gunHoldPoint : bladeHoldePoint;

        if (targetHoldPoint == null)
        {
            Debug.LogError($"[Player] 警告：没有找到 {(newWeaponData.isRanged ? "Gun" : "Blade")} Hold Point！");
            return;
        }

        GameObject weaponObj = Instantiate(newWeaponData.weaponPrefab, targetHoldPoint);
        weaponObj.transform.localPosition = Vector3.zero;
        weaponObj.transform.localRotation = Quaternion.identity;

        currentWeaponInstance = weaponObj.GetComponent<Weapon>();
        if (currentWeaponInstance != null)
        {
            currentWeaponInstance.SetupWeapon(newWeaponData, this);
            weaponController.EquipWeapon(currentWeaponInstance);
        }

        // 🌟 新增 2：武器装配完成后，将【新武器】的属性加到面板上
        if (currentWeaponData != null)
        {
            ((Entity_Stats)stats).ApplyWeaponModifiers(currentWeaponData);
        }
    }

    private void AimTowardsMouse()
    {
        if (currentWeaponData == null || Mouse.current == null) return;

        // 🌟 提取通用的瞄准逻辑，无论是拿刀还是拿枪，都获取鼠标位置
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
        mousePosition.z = 0f;

        // 找到当前正在使用的挂载点，和闲置的挂载点
        Transform activeHoldPoint = currentWeaponData.isRanged ? gunHoldPoint : bladeHoldePoint;
        Transform idleHoldPoint = currentWeaponData.isRanged ? bladeHoldePoint : gunHoldPoint;

        if (activeHoldPoint != null)
        {
            // 🌟 统一计算瞄准角度
            Vector3 aimDirection = mousePosition - activeHoldPoint.position;

            // localX 乘以 facingDir 是神来之笔，完美兼容你 FlipCharacterTowardsMouse 的翻转
            float localX = aimDirection.x * facingDir;
            float localY = aimDirection.y;

            float angle = Mathf.Atan2(localY, localX) * Mathf.Rad2Deg;

            // 让当前的挂载点指向鼠标！
            activeHoldPoint.localRotation = Quaternion.Euler(0, 0, angle);
        }

        // 把没在用的那个挂载点老老实实归零
        if (idleHoldPoint != null)
        {
            idleHoldPoint.localRotation = Quaternion.identity;
        }
    }
    public override void HandleFlip(float xVelocity)
    {
        // 留空即可！什么都不写。
    }
    private void FlipCharacterTowardsMouse()
    {
        if (Mouse.current == null) return;

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);

        // 如果鼠标在右边，但角色当前朝左，则翻转
        if (mousePosition.x > transform.position.x && !facingRight)
        {
            Flip(); // 调用 Entity 基类现成的 Flip()，自动处理 Rotate(0,180,0) 和 事件派发
        }
        // 如果鼠标在左边，但角色当前朝右，则翻转
        else if (mousePosition.x < transform.position.x && facingRight)
        {
            Flip(); // 同上
        }
    }
    protected override IEnumerator SpeedUpEntityCo(float duration, float accMultiplier)
    {
        Debug.Log("Speed up!" + accMultiplier);
        
        float originalMoveSpeed = moveSpeed;
        float originalJumpForce = jumpForce;
        float originalAnimSpeed = anim.speed;
        Vector2 originalWallJump = wallJumpForce;
        Vector2 originalJumpAttack = JumpAttackVelocity;
        //Vector2[] originalAttackVelocity = attackVelocity;

        float speedMultiplier = 1 + accMultiplier;

        moveSpeed = speedMultiplier * moveSpeed;
        jumpForce = speedMultiplier * jumpForce;
        anim.speed = speedMultiplier * anim.speed;
        wallJumpForce = speedMultiplier * wallJumpForce;
        JumpAttackVelocity = speedMultiplier * JumpAttackVelocity;
        //for (int i = 0; i < attackVelocity.Length; i++)
        //{
        //    attackVelocity[i] = attackVelocity[i] * speedMultiplier;
        //}

        yield return new WaitForSeconds(duration);
        moveSpeed = originalMoveSpeed;
        jumpForce = originalJumpForce;
        anim.speed = originalAnimSpeed;
        wallJumpForce = originalWallJump;
        JumpAttackVelocity = originalJumpAttack;
        //for (int i = 0; i < attackVelocity.Length; i++)
        //{
        //    attackVelocity[i] = originalAttackVelocity[i];
        //}
        SpeedUpCo = null;

    }
    public override void EntityDeath()
    {
        base.EntityDeath();

        OnPlayerDeath?.Invoke();

        // 1. 不需要切到 deadState（除非你的 deadState 是个完全不接收输入的空状态）
        // stateMachine.ChangeState(deadState); 

        // 2. 大乱斗式死亡：直接隐藏角色实体
        sr.enabled = false; // 隐藏贴图 (sr 是你在 Entity 里定义的 SpriteRenderer)

        Collider2D cd = GetComponent<Collider2D>();
        if (cd != null) cd.enabled = false; // 关闭碰撞，防止死后还能挡子弹

        // 3. (可选) 在这里播放一个爆炸特效
      
    }

    public override void EntityRespawn()
    {
        base.EntityRespawn();
        sr.enabled = true;

        Collider2D cd = GetComponent<Collider2D>();
        if (cd != null) cd.enabled = true;

        // 2. 状态机切回 Idle，准备重新开打
        stateMachine.ChangeState(idleState);
    }
    public void EnterAttackStateWithDelay()
    {

        if(queuedAttackCo != null)
        {
            StopCoroutine(queuedAttackCo);
        }

        queuedAttackCo = StartCoroutine(EnterAttackStateWithDelayCo());
    }
    public IEnumerator EnterAttackStateWithDelayCo()
    {
        yield return new WaitForEndOfFrame();
        stateMachine.ChangeState(basicAttackState);
    }
    private void TryInteract()
    {
        Transform closest = null;
        float closestDistance = Mathf.Infinity;
        Collider2D[] objectsAround = Physics2D.OverlapCircleAll(transform.position, 1f);

        foreach (var target in objectsAround)
        {
            IInteractable interactable = target.GetComponent<IInteractable>();
            if (interactable == null) continue;

            float distance = Vector2.Distance(transform.position, target.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = target.transform;
            }
        }

        if (closest == null)
            return;

        closest.GetComponent<IInteractable>().Interact();

    }


    private void OnEnable()
    {
        input.Enable();

        input.Player.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Movement.canceled += ctx => moveInput = Vector2.zero;

        input.Player.Interact.performed += ctx => TryInteract();

        input.Player.QuickItemSlot_1.performed += ctx => inventory.TryUseQuickItemInSlot(1);
        input.Player.QuickItemSlot_2.performed += ctx => inventory.TryUseQuickItemInSlot(2);
    }
    public void TeleportPlayer(Vector3 position) => transform.position = position;
    protected override void Update()
    {
        base.Update();
       
        if (!isLocalPlayer) return;
        FlipCharacterTowardsMouse();
        AimTowardsMouse();
    }


    private void OnDisable()
    {
        input?.Disable();   
    }
    //public SkillBase GetSkillByIndex(int index)
    //{



    //}
    


    //private void TryCastSkill(int skillIndex)
    //{
    //    SkillBase skill = GetSkillByIndex(skillIndex); // 你自己实现的技能列表
    //    if (skill == null) return;

    //    // 设置 CastingState 的触发技能
    //    castingState.SetTriggerName(skill.triggerName);

    //    // 切换到 Casting 状态
    //    stateMachine.ChangeState(castingState);
    //}
}
