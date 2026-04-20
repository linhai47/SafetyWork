using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectLootManager : MonoBehaviour
{
    public EffectDatabaseSO effectDatabase; // 🌟 换成特效库
    public GameObject effectPickupPrefab;   // 🌟 换成特效预制体

    [Header("空投配置")]
    public List<Transform> dropHeights;
    public float spawnInterval = 15f;
    public bool autoSpawn = true;

    private void Start()
    {
        if (autoSpawn) StartCoroutine(AutoDropRoutine());
    }

    private IEnumerator AutoDropRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnRandomEffectDrop();
        }
    }

    [ContextMenu("Force Spawn Effect")]
    public void SpawnRandomEffectDrop()
    {
        if (dropHeights.Count == 0) return;

        Transform selectedPoint = dropHeights[Random.Range(0, dropHeights.Count)];
        GameObject drop = Instantiate(effectPickupPrefab, selectedPoint.position, Quaternion.identity);

        // 初始化：塞入一个随机特效
        EffectPickup pickup = drop.GetComponent<EffectPickup>();
        if (pickup != null)
        {
            pickup.Init(effectDatabase.GetRandomEffect());
        }

        // 给个重力让它掉下来
        Rigidbody2D rb = drop.GetComponent<Rigidbody2D>();
        if (rb != null) rb.gravityScale = 1.0f;
    }
}