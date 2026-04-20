using UnityEngine;

public class BlastZone : MonoBehaviour
{
    [Header("复活点配置")]
   
    public Transform[] respawnPoints;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 任何人掉进来，物理引擎都会报信。我们先看看是不是玩家
        if (collision.CompareTag("Player1") || collision.CompareTag("Player2"))
        {
            Entity entity = collision.GetComponent<Entity>();
            Entity_Health health = collision.GetComponent<Entity_Health>();
            Debug.Log("Player entered blast zone: " + collision.name); 
            if (entity != null && health != null)
            {
    
                if (!entity.isLocalPlayer)
                {
                    
                    return;
                }

      
                if (!health.isDead)
                {
                    int idx = Random.Range(0, respawnPoints.Length-1);
                    Vector3 targetRespawnPos = respawnPoints[idx].position;

                    // 本地执行复活
                    health.OutOfBoundsDie(targetRespawnPos);

                    // 发送网络消息告诉别人：“我死了，我要在 idx 点复活”
                    // networkClient.SendChat(...) 
                }
            }
        }
    }
}