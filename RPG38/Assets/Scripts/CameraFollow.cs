using Mirror;
using UnityEngine;

/// <summary>
/// Simple smooth camera follow for a 2D platformer.
/// Attach this to your Main Camera.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("Target to follow (usually the Player).")]
    public Transform target;

    [Tooltip("How fast the camera catches up to the target.")]
    public float smoothTime = 0.2f;

    [Tooltip("follow target position offset.")]
    public Vector2 offset;

    [Tooltip("Optional bounds to clamp camera position.")]
    public Vector2 minBounds;
    public Vector2 maxBounds;
    public bool enableBoundsClamping;

    private Vector3 velocity = Vector3.zero;

    // void Start()
    // {
    //     // 如果 Inspector 里没拖，就用 Tag 查一次场景里的 Player
    //     if (target == null)
    //     {
    //         var go = GameObject.FindGameObjectWithTag("Player");
    //         if (go != null) target = go.transform;
    //     }
    // }
    private void LateUpdate()
    {
        // 如果没有 target，尝试自动找本地玩家
        if (target == null)
        {
            var players = GameObject.FindObjectsOfType<NetworkBehaviour>();
            foreach (var player in players)
            {
                if (player.isLocalPlayer)
                {
                    target = player.transform;
                    break;
                }
            }
        }

        
        if (target == null) return;

        // 1. 计算目标位置（保持 Z 不变）
        Vector3 targetPos = new Vector3(
            target.position.x+offset.x,
            target.position.y+offset.y,
            transform.position.z
        );

        // 2. 平滑移动到目标位置
        Vector3 smoothPos = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            smoothTime
        );

        // 3. 在可选范围内进行限制
        if (enableBoundsClamping)
        {
            smoothPos.x = Mathf.Clamp(smoothPos.x, minBounds.x, maxBounds.x);
            smoothPos.y = Mathf.Clamp(smoothPos.y, minBounds.y, maxBounds.y);
        }

        // 4. 应用到摄像机
        transform.position = smoothPos;
    }
}