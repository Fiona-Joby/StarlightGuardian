using UnityEngine;

/// <summary>
/// Thin trigger zone placed just above the ground.
/// When a star enters, it is told to land. This does NOT collect the star —
/// collection is handled exclusively by CatchZone on the player.
///
/// Architecture:
///   GroundDetector detects landing ONLY.
///   CatchZone detects pickup ONLY.
///   These must remain separate so Normal stars stay on the ground.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GroundDetector : MonoBehaviour
{
    private Collider2D landingTrigger;

    private void Awake()
    {
        landingTrigger = GetComponent<Collider2D>();
        landingTrigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        StarMovement star = other.GetComponent<StarMovement>();
        if (star == null) return;

        // Place the star just above the upper edge of this thin trigger.
        float starHalfHeight = other.bounds.extents.y;
        Vector2 restingPosition = new Vector2(
            star.transform.position.x,
            landingTrigger.bounds.max.y + starHalfHeight
        );
        star.ReachGround(restingPosition);
    }
}
