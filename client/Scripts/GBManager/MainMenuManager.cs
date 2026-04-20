using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening; // 🌟 必须引入 DoTween 命名空间

public class MainMenuManager : MonoBehaviour
{
    [Header("UI 面板引用 (注意：这里改成了 RectTransform)")]
    public RectTransform mainPanel;   // 主界面的左侧面板
    public RectTransform lobbyPanel;  // 房间UI面板

    [Header("动画参数")]
    public float animDuration = 0.5f; // UI 进出动画的持续时间

    private void Start()
    {
        // 🚨 极其关键的一步：做 DoTween 动画的 UI 必须处于 Active 状态！
        // 如果它们被 SetActive(false) 隐藏了，动画是跑不起来的。
        mainPanel.gameObject.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.gameObject.SetActive(true);

        // 初始化位置：
        // 1. 主面板在屏幕正中间
        //mainPanel.anchoredPosition = Vector2.zero;
        // 2. 房间面板藏在屏幕正上方（假设高度为1200，刚好在屏幕外）
        if (lobbyPanel != null) lobbyPanel.anchoredPosition = new Vector2(0, 1200);
    }

    // ==========================================
    // 🔘 给按钮调用的公开方法 (必须是 public)
    // ==========================================

    // 1. 点击“训练模式”按钮调用：直接进单机游戏场景
    public void OnClickTrainingMode()
    {
        Debug.Log("进入训练模式...");
        // 跳转到你指定的战斗场景
        SceneManager.LoadScene("MainGame");
    }

    // 2. 点击“双人对战”按钮调用：执行 DoTween 切换动画
    public void OnClickMultiplayer()
    {
        Debug.Log("打开双人联机房间...");

        // 主面板向左滑出，使用 InBack 蓄力特效
        mainPanel.DOAnchorPos(new Vector2(-1500, 200), animDuration)
                 .SetEase(Ease.InBack);

        // 房间面板从上方掉下来，延迟0.2秒制造层次感，使用 OutBack 落地回弹特效
        lobbyPanel.DOAnchorPos(Vector2.zero, animDuration)
                 .SetEase(Ease.OutBack)
                 .SetDelay(0.2f);
    }

    // 🌟 额外补充：从房间返回主界面的方法（你可以给 Lobby 面板里加个 [返回] 按钮连上这个）
    public void OnClickBackToMain()
    {
        Debug.Log("返回主菜单...");

        // 房间面板原路返回上方
        lobbyPanel.DOAnchorPos(new Vector2(0, 1200), animDuration)
                 .SetEase(Ease.InBack);

        // 主面板从左边滑回屏幕中央
        mainPanel.DOAnchorPos(new Vector2(-800, 200), animDuration)
                 .SetEase(Ease.OutBack)
                 .SetDelay(0.2f);
    }

    // 3. 点击“退出游戏”按钮调用
    public void OnClickQuitGame()
    {
        Debug.Log("退出游戏！");
        Application.Quit(); // 注意：这行代码在 Unity 编辑器里按了没反应，打包成 exe 后才有用
    }
}