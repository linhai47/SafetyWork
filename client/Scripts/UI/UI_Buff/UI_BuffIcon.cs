using UnityEngine;
using UnityEngine.UI;

public class UI_BuffIcon : MonoBehaviour
{
    public Image iconImage;
    public Image cooldownMask;

    // 🌟 新增：记住自己代表的特效是谁
    public WeaponEffectSO currentEffect;

    private float timeLeft;
    private float maxTime;

    // 🌟 修改 Setup：直接接收整个 Effect
    public void Setup(WeaponEffectSO effect)
    {
        currentEffect = effect;
        iconImage.sprite = effect.buffIcon;

        float duration = effect.duration <= 0 ? 0.1f : effect.duration;
        maxTime = duration;
        timeLeft = duration;
        UpdateUI();
    }

    // 🌟 新增：刷新倒计时的方法
    public void RefreshTime()
    {
        // 重新填满时间
        timeLeft = maxTime;
        UpdateUI();

        // 进阶提示：你甚至可以在这里加个小动画，让图标闪一下表示“刷新成功”
    }

    private void Update()
    {
        if (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            UpdateUI();

            if (timeLeft <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    private void UpdateUI()
    {
        float ratio = timeLeft / maxTime;
        cooldownMask.fillAmount = 1 - ratio; // 慢慢变黑
    }
}