using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public WeaponDataSO weaponData;
    public string dropId; // 联机唯一ID

    [SerializeField] private SpriteRenderer spriteRenderer;

    public void Init(WeaponDataSO data, string id = "")
    {
        weaponData = data;
        // 如果没有传入ID，测试阶段随机生成一个
        dropId = string.IsNullOrEmpty(id) ? System.Guid.NewGuid().ToString() : id;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 直接获取你写好的 Player 组件
        Player player = collision.GetComponent<Player>();

        if (player != null)
        {
            // 🌟 2. 网络拦截：只有“本地玩家的实体”撞到包裹，才允许发起请求！
            if (player.isLocalPlayer)
            {
                Debug.Log($"[网络同步] 本地玩家碰到了包裹 {dropId}，准备向服务器发请求！");

                // ==========================================
                // 🌐 WebSocket 联机预留区
                // ==========================================
                // 未来接入 WebSocket 时，这里只负责发消息：
                // WebSocketManager.Instance.SendPickupRequest(dropId, player.gameObject.name);

                // ==========================================
                // 🚧 【单机测试阶段的临时代码】
                // 注意：真正联机后，下面这两行必须删掉！
                // 它们要转移到“收到服务器成功确认”的网络回调函数里去执行！
                // ==========================================
                player.EquipWeapon(weaponData); // 调用你 Player.cs 里的现成方法
                Destroy(gameObject);
            }
            else
            {
                // 如果是 Player 2（别人的克隆体）在你的屏幕上撞到了包裹
                // 你的客户端什么都不做！老老实实等服务器下发指令。
                Debug.Log("[网络同步] 其他玩家碰到了包裹，本地客户端忽略，等待服务器仲裁广播...");
            }
        }
    }
}