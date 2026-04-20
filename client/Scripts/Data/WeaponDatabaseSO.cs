using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Game/Weapon Database")]
public class WeaponDatabaseSO : ScriptableObject
{
    // 所有的武器图纸都在这里登记
    public List<WeaponDataSO> allWeapons;

    // 随机获取一把武器的方法
    public WeaponDataSO GetRandomWeapon()
    {
        if (allWeapons == null || allWeapons.Count == 0) return null;
        return allWeapons[Random.Range(0, allWeapons.Count)];
    }
}