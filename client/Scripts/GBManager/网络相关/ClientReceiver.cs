using UnityEngine;

public class ClientReceiver : MonoBehaviour
{
    public static ClientReceiver Instance;

    [Header("°ó¶¨")]
    [SerializeField] private Rigidbody2D playerRb;

    [Header("µ÷ÊÔ")]
    [SerializeField] private bool debugSnapshotLog = true;
    [SerializeField] private float followSpeed = 15f;

    private Vector2 targetPos;
    private bool hasTarget = false;

    private void Awake()
    {
        Instance = this;

        if (playerRb == null)
        {
            playerRb = GetComponent<Rigidbody2D>();
        }

        if (playerRb != null)
        {
            targetPos = playerRb.position;
            hasTarget = true;
        }
    }

    public void OnReceiveSnapshot(MatchSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        if (debugSnapshotLog)
        {
            Debug.Log($"[ClientReceiver:{name}] apply snapshot tick={snapshot.tick} pos=({snapshot.posX:F2},{snapshot.posY:F2})");
        }

        targetPos = new Vector2(snapshot.posX, snapshot.posY);
        hasTarget = true;
    }

    private void FixedUpdate()
    {
        if (playerRb == null || !hasTarget)
            return;

        Vector2 next = Vector2.Lerp(playerRb.position, targetPos, followSpeed * Time.fixedDeltaTime);
        playerRb.MovePosition(next);
    }
}