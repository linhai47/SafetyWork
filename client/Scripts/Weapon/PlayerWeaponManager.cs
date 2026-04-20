using UnityEngine;

public class PlayerWeaponManager : MonoBehaviour
{
    public Transform weaponHoldPoint; // 枪在玩家手里的位置
    public Weapon currentWeaponEntity; // 当前手里的武器实例

    // 🌟 1. 新增：引用你的战斗控制器
    private PlayerCombatController combatController;

    private void Awake()
    {
        // 假设这两个脚本都挂在 Player 身上，直接获取
        combatController = GetComponent<PlayerCombatController>();
    }

    public void SwitchWeapon(WeaponDataSO newData)
    {
        // 1. 销毁旧武器
        if (currentWeaponEntity != null)
        {
            Destroy(currentWeaponEntity.gameObject);
        }

        // 2. 根据数据里的 Prefab 生成新武器
        GameObject weaponObj = Instantiate(newData.weaponPrefab, weaponHoldPoint.position, weaponHoldPoint.rotation);
        weaponObj.transform.SetParent(weaponHoldPoint);

        // 3. 初始化武器
        currentWeaponEntity = weaponObj.GetComponent<Weapon>();
        Entity playerEntity = GetComponent<Entity>();
        currentWeaponEntity.SetupWeapon(newData, playerEntity);

        // ==========================================
        // 🌟 2. 核心桥梁：把装好的武器，喂给负责检测输入的 CombatController！
        // ==========================================
        if (combatController != null)
        {
            combatController.EquipWeapon(currentWeaponEntity);
        }

        Debug.Log($"成功替换武器为：{newData.name}");
    }
}