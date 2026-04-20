using System.Collections;
using UnityEngine;

public class GameJuiceManager : MonoBehaviour
{
    public static GameJuiceManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 调用这个方法触发顿帧！
    // 传入弹反者和被弹反的子弹
    public void TriggerHitStopLocal(Animator playerAnim, Rigidbody2D bulletRb, float duration = 0.15f)
    {
        StartCoroutine(HitStopLocalRoutine(playerAnim, bulletRb, duration));
    }

    private IEnumerator HitStopLocalRoutine(Animator playerAnim, Rigidbody2D bulletRb, float duration)
    {
        // 1. 记录子弹原本的速度，并把子弹刹停
        Vector2 savedVelocity = bulletRb.linearVelocity;
        bulletRb.linearVelocity = Vector2.zero;

        // 2. 把玩家的挥刀动画暂停
        float savedAnimSpeed = 1f;
        if (playerAnim != null)
        {
            savedAnimSpeed = playerAnim.speed;
            playerAnim.speed = 0f; // 动画静止
        }

        // 3. 等待零点几秒（此时全局时间和其他玩家都在正常移动）
        yield return new WaitForSeconds(duration);

        // 4. 恢复子弹速度和动画
        bulletRb.linearVelocity = savedVelocity;
        if (playerAnim != null)
        {
            playerAnim.speed = savedAnimSpeed;
        }
    }
}