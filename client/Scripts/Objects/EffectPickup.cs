using UnityEngine;

public class EffectPickup : MonoBehaviour
{
    private WeaponEffectSO containedEffect;
    public SpriteRenderer iconRenderer; // 掉在地上时显示的图标

    public void Init(WeaponEffectSO effect)
    {
        containedEffect = effect;
        // 如果你的 EffectSO 里有图标，这里可以顺便换个皮
        if (iconRenderer != null && effect.buffIcon != null)
        {
            iconRenderer.sprite = effect.buffIcon;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 判定是不是玩家捡到了
        Player_Combat playerCombat = collision.GetComponent<Player_Combat>();

        if (playerCombat != null)
        {
            // 🌟 核心逻辑：告诉玩家的武器系统，新增一个特效
            // 假设你的 Player_Combat 有个方法叫 AddEffectToCurrentWeapon
            playerCombat.AddEffectToCurrentWeapon(containedEffect);

            // 播放个捡东西特效，然后销毁
            Destroy(gameObject);
        }
    }
}