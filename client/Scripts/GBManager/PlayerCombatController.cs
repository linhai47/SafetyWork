using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    // 你的输入动作集合
    private PlayerInputSet input;

    [Header("状态记录")]
    private bool isHoldingShoot = false;    // 玩家是否正按住开火键
    private bool hasFiredSingleShot = false;// 单发锁：这次按压是否已经开过枪了

    // 假设你有对当前武器的引用
    public Weapon currentWeapon;
    public void EquipWeapon(Weapon newWeapon)
    {
        currentWeapon = newWeapon;

        // 如果你需要在这里重置单发锁，可以加上：
        hasFiredSingleShot = false;

        Debug.Log($"成功装备了新武器: {newWeapon.data.name}");
    }
    private void Awake()
    {
        input = new PlayerInputSet();

        // 1. 当玩家【按下】开火键的瞬间 (started)
        input.Player.Attack.started += ctx =>
        {
            isHoldingShoot = true;
            hasFiredSingleShot = false; // 每次重新按下，解锁单发锁
        };

        // 2. 当玩家【松开】开火键的瞬间 (canceled)
        input.Player.Attack.canceled += ctx =>
        {
            isHoldingShoot = false;
        };
    }
    
    private void OnEnable() => input.Enable();
    private void OnDisable() => input.Disable();

    private void Update()
    {
        if (currentWeapon == null || currentWeapon.data == null) return;

        // 获取当前武器的数据 (这里假设你没有拆分 SO，直接读取 isAutomatic)
        bool isAuto = currentWeapon.data.isAutomatic;

        // 如果玩家当前正按着开火键
        if (isHoldingShoot)
        {
            if (isAuto)
            {
                // 【机枪逻辑】
                // 只要按住，每一帧都无脑呼叫 ExecuteAttack
                // 别担心会卡爆，因为武器内部的 Time.time < lastAttackTime + CD 已经帮你完美拦截了！
                currentWeapon.ExecuteAttack();
            }
            else
            {
                // 【霰弹枪/狙击枪逻辑】
                // 必须检查“单发锁”。只有按下的第一下才会通过
                if (!hasFiredSingleShot)
                {
                    currentWeapon.ExecuteAttack();
                    hasFiredSingleShot = true; // 上锁！必须等玩家松开手(canceled)重新按下(started)才能解锁
                }
            }
        }
    }
}