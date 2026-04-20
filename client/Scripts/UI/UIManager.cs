using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public TextMeshProUGUI p1PercentageText;
    public TextMeshProUGUI p2PercentageText;
    [Header("动态命数 UI")]
    public GameObject stockIconPrefab; // 只需要拖入一个命数图标的预制体 (Prefab)
    public Transform p1StockContainer; //  P1 的 Layout Group 容器
    public Transform p2StockContainer; //  P2 的 Layout Group 容器
    private List<GameObject> p1Icons = new List<GameObject>();
    private List<GameObject> p2Icons = new List<GameObject>();
    [Header("动效设置")]
    [SerializeField] private float punchDuration = 0.2f;   // 缩放持续时间
    [SerializeField] private float maxShakeStrength = 20f; // 最大抖动强度
    [SerializeField] private float redThreshold = 150f;   // 到达多少百分比时全红


    private void Awake() => Instance = this;

    public void UpdateStocks(int p1Stocks, int p2Stocks)
    {
        SyncStockIcons(p1Icons, p1StockContainer, p1Stocks);
        SyncStockIcons(p2Icons, p2StockContainer, p2Stocks);
    }
    private void SyncStockIcons(List<GameObject> iconList, Transform container, int targetCount)
    {
        // 1. 如果数量不够（比如开局），动态生成补齐
        while (iconList.Count < targetCount)
        {
            GameObject newIcon = Instantiate(stockIconPrefab, container);
            iconList.Add(newIcon);
        }

        // 2. 如果数量多了（比如角色刚死了一次），动态销毁多余的
        while (iconList.Count > targetCount)
        {
            // 总是销毁列表里的最后一个图标（最右边的）
            int lastIndex = iconList.Count - 1;
            GameObject iconToRemove = iconList[lastIndex];

            // 从列表中移除，并销毁物体
            iconList.RemoveAt(lastIndex);
            Destroy(iconToRemove);

            // 💡 进阶动效：
            // 如果你想让图标死亡时有个特效，可以不要立刻 Destroy
            // 而是 Instantiate 一个“图标炸裂”的粒子特效在 iconToRemove.transform.position
            // 然后再 Destroy 掉图标。
        }
    }
    public void UpdatePercentageUI(string playerTag, float percentage)
    {
        TextMeshProUGUI targetText = (playerTag == "Player1") ? p1PercentageText : p2PercentageText;

   
        string rawText = percentage.ToString("F1");

        // 2. 按照小数点切分成两半: parts[0] 是 "57", parts[1] 是 "7"
        string[] parts = rawText.Split('.');

        //  3. 神奇的富文本拼接：
        // 让小数部分和百分号的大小变成原来的 60%
        string formattedText = $"{parts[0]}<size=60%>.{parts[1]}%</size>";

        // 赋值给 UI
        targetText.text = formattedText;

        // 2. 停止该文本正在进行的旧动效，防止冲突
        StopCoroutine("AnimateTextRoutine");
        StartCoroutine(AnimateTextRoutine(targetText, percentage));
    }

    private IEnumerator AnimateTextRoutine(TextMeshProUGUI uiText, float percentage)
    {
        RectTransform rectTransform = uiText.rectTransform;
        Vector2 originalPos = rectTransform.anchoredPosition; // 记录初始位置，抖完要回来
        Vector3 originalScale = Vector3.one;

        // --- A. 颜色 Lerp 逻辑 ---
        // 随着百分比从 0 到 redThreshold，颜色从白色渐变为红色
        float colorT = Mathf.Clamp01(percentage / redThreshold);
        uiText.color = Color.Lerp(Color.white, Color.red, colorT);

        // --- B. 抖动强度计算 ---
        // 伤害越高，抖得越狠。起始就有基础抖动，高百分比时变狂暴
        float currentShakeStrength = Mathf.Lerp(2f, maxShakeStrength, colorT);

        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / punchDuration;

            // 1. 缩放爆破：先变大到 1.5 倍再缩回 1.0 (使用平滑曲线)
            float scaleCurve = Mathf.Sin(normalizedTime * Mathf.PI); // 0 -> 1 -> 0 的曲线
            uiText.transform.localScale = originalScale + Vector3.one * (scaleCurve * 0.5f);

            // 2. 随机位移（抖动）：每一帧都往随机方向跳一下
            if (percentage > 20f) // 低于 20% 就不抖了，保持稳重
            {
                Vector2 shakeOffset = Random.insideUnitCircle * currentShakeStrength;
                rectTransform.anchoredPosition = originalPos + shakeOffset;
            }

            yield return null;
        }

        // --- C. 还原状态 ---
        rectTransform.anchoredPosition = originalPos;
        uiText.transform.localScale = originalScale;
    }
}