using UnityEngine;
using System.Collections.Generic;
using System.Collections; // 🌟 必须加上这个命名空间才能用协程

public class LootManager : MonoBehaviour
{
    public WeaponDatabaseSO weaponDatabase;
    public GameObject pickupPrefab;

    [Header("空投点集合")]
    public List<Transform> dropHeights;

    [Header("定时空投设置")]
    public float spawnInterval = 10f; // 🌟 每隔多少秒掉落一次
    public bool autoSpawn = true;     // 🌟 是否开启自动掉落开关

    private void Start()
    {
        // 游戏一开始，如果开关是开着的，就启动“定时空投”协程
        if (autoSpawn)
        {
            StartCoroutine(AutoDropRoutine());
        }
    }

    // 🌟 这就是那个“打工人”协程
    private IEnumerator AutoDropRoutine()
    {
        // 死循环：只要这个脚本还在运行，就会一直执行
        while (true)
        {
            // 1. 先睡 10 秒（等待设定的间隔时间）
            yield return new WaitForSeconds(spawnInterval);

            // 2. 时间到！扔个包裹下去！
            SpawnRandomDrop();
        }
    }

    [ContextMenu("Spawn Random Drop")]
    public void SpawnRandomDrop()
    {
        if (dropHeights == null || dropHeights.Count == 0)
        {
            Debug.LogError("老板，空投点列表是空的！");
            return;
        }

        Transform selectedDropPoint = dropHeights[Random.Range(0, dropHeights.Count)];
        Vector3 spawnPos = selectedDropPoint.position;

        GameObject drop = Instantiate(pickupPrefab, spawnPos, Quaternion.identity);

        WeaponPickup pickup = drop.GetComponent<WeaponPickup>();
        pickup.Init(weaponDatabase.GetRandomWeapon());

        Rigidbody2D rb = drop.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 1.0f;
        }
    }

    // 🌟 额外赠送：提供给其他脚本调用的开关方法（比如游戏结束时停止空投）
    public void StopAutoDrop()
    {
        StopAllCoroutines();
    }
}