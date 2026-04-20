using System.Collections;
using UnityEngine;

public class Behavior_Parry : MonoBehaviour
{
    private Entity wielder;
    private Effect_Parry effect;
    private Animator wielderAnim;
    private Transform hitPoint;
    private float radius;
    private bool hasParried = false;

    // 🌟 只需要一个 Mask
    private LayerMask targetMask;

    public void SetupBehavior(Entity wielder, Effect_Parry effect, Transform hitPoint, float radius, LayerMask targetMask)
    {
        this.wielder = wielder;
        this.effect = effect;
        this.hitPoint = hitPoint;
        this.radius = radius;
        this.targetMask = targetMask;

        this.wielderAnim = wielder.GetComponentInChildren<Animator>();

        StartCoroutine(ParryWindowRoutine());
    }

    private IEnumerator ParryWindowRoutine()
    {
        float timer = 0f;
        while (timer < effect.parryWindowFrames)
        {
            timer += Time.deltaTime;
            if (!hasParried) CheckForProjectiles();
            yield return null;
        }
        Destroy(this);
    }

    private void CheckForProjectiles()
    {
        // 🌟 1. 用 targetMask 画圈，抓取判定范围内的敌方物体（包括子弹）
        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(hitPoint.position, radius, targetMask);

        foreach (var coll in hitObjects)
        {
            // 虽然用 targetMask 也会抓到敌人的肉体，但不用担心！
            // GetComponent<Projectile>() 会完美过滤掉那些不是子弹的东西。
            Projectile bullet = coll.GetComponent<Projectile>();
            if (bullet != null)
            {
                hasParried = true;
                ExecuteParry(bullet);
                break;
            }
        }
    }

    private void ExecuteParry(Projectile bullet)
    {
        effect.StartCooldown(wielder);

        if (effect.parrySuccessVFX != null)
        {
            GameObject vfx = Instantiate(effect.parrySuccessVFX, bullet.transform.position, Quaternion.identity);
            Destroy(vfx, 1f);
        }

        // 🌟 2. 用同样的 targetMask，让子弹叛变，打向敌人阵营！
        bullet.Deflect(wielder, targetMask, effect.deflectSpeedMultiplier);

        StartCoroutine(HitStopLocalRoutine(bullet));
    }

    private IEnumerator HitStopLocalRoutine(Projectile bullet)
    {
        // ... 顿帧代码保持不变 ...
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        Vector2 targetVelocity = bulletRb.linearVelocity;
        bulletRb.linearVelocity = Vector2.zero;

        float originalAnimSpeed = 1f;
        if (wielderAnim != null)
        {
            originalAnimSpeed = wielderAnim.speed;
            wielderAnim.speed = 0f;
        }

        yield return new WaitForSeconds(effect.hitStopDuration);

        if (bulletRb != null) bulletRb.linearVelocity = targetVelocity;
        if (wielderAnim != null) wielderAnim.speed = originalAnimSpeed;
    }

    private void OnDrawGizmos()
    {
        // 只有在这个脚本存活时（也就是弹反帧期间）才会画圈
        if (hitPoint != null)
        {
            // 用一个极其醒目的青色，防止和红色的攻击判定圈混淆
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(hitPoint.position, radius);
        }
    }
}