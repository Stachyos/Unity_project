using Mirror;
using UnityEngine;

/// <summary>
/// Simple smooth camera follow for a 2D platformer.
/// Attach this to your Main Camera.
/// </summary>
public class CameraFollow : MonoBehaviour
{

    /// This script is written by myself. 
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
    //     if (target == null)
    //     {
    //         var go = GameObject.FindGameObjectWithTag("Player");
    //         if (go != null) target = go.transform;
    //     }
    // }
    private void LateUpdate()
    {
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

        // Calculate the target position (keep Z unchanged)
        Vector3 targetPos = new Vector3(
            target.position.x+offset.x,
            target.position.y+offset.y,
            transform.position.z
        );

        // Smoothly move to the target position
        Vector3 smoothPos = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            smoothTime
        );

        // Place restrictions within the available options
        if (enableBoundsClamping)
        {
            smoothPos.x = Mathf.Clamp(smoothPos.x, minBounds.x, maxBounds.x);
            smoothPos.y = Mathf.Clamp(smoothPos.y, minBounds.y, maxBounds.y);
        }

        // Applied to the camera
        transform.position = smoothPos;
    }
}