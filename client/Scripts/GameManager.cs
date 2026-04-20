using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro; // 如果你用 TextMeshPro 做 UI
using DG.Tweening;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public int maxStocks = 3;
    public float respawnDelay = 2.0f;

    [Header("Player Data")]
    public int p1Stocks;
    public int p2Stocks;

    [Header("UI Reference")]
    public TextMeshProUGUI countdownText; // 拖入显示倒计时的文字
    public GameObject gameOverPanel;     // 游戏结束的面板

    private bool isGameActive = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(GameSetupRoutine());
    }

    //  1. 开场逻辑

    IEnumerator GameSetupRoutine()
    {
        p1Stocks = maxStocks;
        p2Stocks = maxStocks;
        isGameActive = false; 

        UpdateStockUI();

        countdownText.gameObject.SetActive(true);

        // --- 第 1 秒：Get Ready 缓动放大 ---
        countdownText.text = "Get Ready?";
        countdownText.color = Color.yellow;
        // 先把字变小，然后用 1 秒时间平滑放大，营造蓄力感
        countdownText.transform.localScale = Vector3.one * 0.5f;
        countdownText.transform.DOScale(1.2f, 1f).SetEase(Ease.OutQuad);

        yield return new WaitForSecondsRealtime(1f);

        // --- 第 2 秒：Fight 爆裂弹射 ---
        // 杀气腾腾的红色
        countdownText.color = Color.red;
        countdownText.text = "Fight!";
        isGameActive = true; // 🔓 闸门开！玩家可以动了！

        // 杀手锏：DOPunchScale (拳击缩放)。瞬间撑大并带回弹余震，打击感拉满！
        countdownText.transform.localScale = Vector3.one * 1.5f;
        countdownText.transform.DOPunchScale(new Vector3(0.5f, 0.5f, 0), 0.5f, 10, 1f);

        yield return new WaitForSecondsRealtime(1f);

        // --- 收尾：优雅淡出 ---
        countdownText.DOFade(0f, 0.3f).OnComplete(() =>
        {
            countdownText.gameObject.SetActive(false);
            // 记得把透明度恢复，不然下局开局字就看不见了
            countdownText.DOFade(1f, 0f);
        });
    }

    //  2. 玩家死亡回调 (由 Entity_Health 调用)
    public void OnPlayerDeath(string playerTag)
    {
        if (!isGameActive) return;

        if (playerTag == "Player1")
        {
            p1Stocks--;
            Debug.Log($"P1 丢了一命！剩余: {p1Stocks}");
        }
        else if (playerTag == "Player2")
        {
            p2Stocks--;
            Debug.Log($"P2 丢了一命！剩余: {p2Stocks}");
        }

        UpdateStockUI();

        // 检查胜负
        if (p1Stocks <= 0 || p2Stocks <= 0)
        {
            EndGame(p1Stocks <= 0 ? "Player 2" : "Player 1");
        }
    }

    private void UpdateStockUI()
    {
        // 这里去调用你 UI 脚本里显示小头像或星星的方法
        UIManager.Instance?.UpdateStocks(p1Stocks, p2Stocks);
    }

    // 🌟 3. 游戏结束
    void EndGame(string winnerName)
    {
        isGameActive = false;
        Debug.Log("游戏结束！获胜者是: " + winnerName);

        // 慢动作特写
        Time.timeScale = 0.5f;

        // 显示结束面板
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
          
        }
    }

    public bool IsGameActive() => isGameActive;
}