using UnityEngine;

public class UI_BuffManager : MonoBehaviour
{
    public GameObject buffIconPrefab;
    public Transform buffContainer;

    // 🌟 将原来的 AddBuffUI 改成 AddOrRefreshBuffUI，直接接收整个特效数据
    public void AddOrRefreshBuffUI(WeaponEffectSO effect)
    {
        // 防呆检查：如果没传东西、不该显示、或者没配图标，直接不处理
        if (effect == null || !effect.showInUI || effect.buffIcon == null) return;

        // ==========================================
        // 核心逻辑 1：先找找有没有一样的“老面孔”
        // ==========================================
        foreach (Transform child in buffContainer)
        {
            UI_BuffIcon existingIcon = child.GetComponent<UI_BuffIcon>();

            // 如果找到了同一个特效的图标 (认脸成功)
            if (existingIcon != null && existingIcon.currentEffect == effect)
            {
                existingIcon.RefreshTime(); // 刷新它的时间（让黑影弹回全白）
                return;                     // 🌟 核心：直接退出方法，不再生成新图标！
            }
        }

        // ==========================================
        // 核心逻辑 2：如果是没见过的新 Buff，才新建图标
        // ==========================================
        GameObject newBuffObj = Instantiate(buffIconPrefab, buffContainer);
        UI_BuffIcon buffUI = newBuffObj.GetComponent<UI_BuffIcon>();

        // 注意：这里调用的是修改后的 Setup 方法，直接把 effect 传给它
        buffUI.Setup(effect);
    }
}