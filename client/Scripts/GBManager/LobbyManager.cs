using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
// using TMPro; // 如果你用的是 TextMeshPro，如果用普通 Text 就换成 UnityEngine.UI

public class LobbyManager : MonoBehaviour
{
    [Header("UI 按钮引用")]
    public Button p1ReadyButton;
    public Button p2ReadyButton;
    public Button startButton;

    [Header("管理器引用")]
    // 🌟 新增：引用主菜单管理器，用来调用返回动画
    public MainMenuManager mainMenuManager;

    [Header("状态记录")]
    public bool isPlayer1Ready = false;
    public bool isPlayer2Ready = false;

    private void OnEnable()
    {
        // 每次这个 Panel 显示时，重置状态
        ResetLobby();
    }

    private void ResetLobby()
    {
        isPlayer1Ready = false;
        isPlayer2Ready = false;

        // UI 初始化
        UpdateButtonVisuals(p1ReadyButton, false);
        if (p2ReadyButton != null) UpdateButtonVisuals(p2ReadyButton, false);

        // Start 按钮初始必须是置灰（不可点击）的
        startButton.interactable = false;
    }


    public void OnClickP1Ready()
    {
        isPlayer1Ready = !isPlayer1Ready;
        UpdateButtonVisuals(p1ReadyButton, isPlayer1Ready);

        // [网络联机 TODO]: 在这里通过 WebSocket 给服务器发消息
        // e.g., NetworkManager.Send("{'type':'ready', 'state':" + isPlayer1Ready + "}");

        CheckStartCondition();
    }

    public void OnClickStartGame()
    {
        if (isPlayer1Ready && isPlayer2Ready)
        {
            Debug.Log("P1 房主点击了 Start！通知服务器开启游戏...");
            SceneManager.LoadScene("MainGame");
        }
    }

    // ==========================================
    // 🔙 新增：返回主菜单逻辑
    // ==========================================
    public void OnClickBackButton()
    {
        Debug.Log("点击了返回主菜单，退出房间...");

        // 1. 重置当前房间的所有状态
        ResetLobby();

        // 2. [网络联机 TODO]: 通知服务器我退出了房间，或者断开连接
        // e.g., NetworkManager.Send("{'type':'leave_room'}");

        // 3. 调用 MainMenuManager 里的动画，让 UI 滑回去
        if (mainMenuManager != null)
        {
            mainMenuManager.OnClickBackToMain();
        }
        else
        {
            Debug.LogError("老板，你忘了把 MenuController 拖给 LobbyManager 啦！");
        }
    }

    // ==========================================
    // 🌐 网络接口预留
    // ==========================================
    public void NetworkReceiveP2ReadyState(bool isReady)
    {
        isPlayer2Ready = isReady;
        Debug.Log("收到网络消息：P2 准备状态变为 " + isReady);

        if (p2ReadyButton != null) UpdateButtonVisuals(p2ReadyButton, isPlayer2Ready);

        CheckStartCondition();
    }

    // ==========================================
    // 🛠️ 内部辅助方法
    // ==========================================
    private void CheckStartCondition()
    {
        startButton.interactable = (isPlayer1Ready && isPlayer2Ready);
    }

    private void UpdateButtonVisuals(Button btn, bool isReady)
    {
        ColorBlock colors = btn.colors;
        colors.normalColor = isReady ? Color.green : Color.white;
        colors.selectedColor = isReady ? Color.green : Color.white;
        btn.colors = colors;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            NetworkReceiveP2ReadyState(!isPlayer2Ready);
        }
    }
}