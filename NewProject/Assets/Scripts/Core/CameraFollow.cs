using UnityEngine;

/// <summary>
/// Camera follows the player's X position while keeping its own Y and Z.
/// Runs in LateUpdate so the player has already moved for that frame.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public Transform player;

    private void LateUpdate()
    {
        if (player == null) return;

        transform.position = new Vector3(
            player.position.x,
            transform.position.y,
            transform.position.z
        );
    }
}
