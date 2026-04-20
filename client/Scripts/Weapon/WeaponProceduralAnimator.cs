using UnityEngine;

public class WeaponProceduralAnimator : MonoBehaviour
{
    [Header("后坐力平移参数")]
    [Tooltip("开火时武器向后退的方向和距离 (通常是本地坐标的 -X 或 -Y)")]
    public Vector3 recoilOffset = new Vector3(-0.2f, 0f, 0f);
    [Tooltip("武器退回原位的平滑速度 (值越大回弹越快)")]
    public float returnSpeed = 15f;

    [Header("可选：枪口上跳 (旋转)")]
    [Tooltip("开火时枪口随机上扬的最大角度")]
    public float maxRecoilAngle = 5f;
    [Tooltip("角度恢复的平滑速度")]
    public float rotationReturnSpeed = 10f;

    private Vector3 restPosition;     // 武器默认的本地位置
    private Quaternion restRotation;  // 武器默认的本地旋转

    private void Start()
    {
        // 记录武器装配到角色手上时的初始位置和角度
        restPosition = transform.localPosition;
        restRotation = transform.localRotation;
    }

    private void Update()
    {

        transform.localPosition = Vector3.Lerp(transform.localPosition, restPosition, Time.deltaTime * returnSpeed);

  
        transform.localRotation = Quaternion.Slerp(transform.localRotation, restRotation, Time.deltaTime * rotationReturnSpeed);
    }


    public void ApplyRecoil()
    {
     
        transform.localPosition = restPosition + recoilOffset;

      
        float randomAngle = Random.Range(-maxRecoilAngle, maxRecoilAngle);
        transform.localRotation = restRotation * Quaternion.Euler(0, 0, randomAngle);
    }
}