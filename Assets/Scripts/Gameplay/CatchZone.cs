using System;
using UnityEngine;

/// <summary>
/// Trigger collider on a child of the Player that detects star pickups.
/// This is intentionally separate from the Player body collider because
/// later it will represent the hand position from pose tracking.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CatchZone : MonoBehaviour
{
    /// <summary>Fires when a star is caught (for local listeners on the player).</summary>
    public event Action<StarMovement> StarCaught;

    private void Awake()
    {
        // Ensure the collider is a trigger.
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    /// <summary>
    /// Called by StarMovement.OnTriggerEnter2D when a star overlaps this zone.
    /// The star detects the player, not the other way around.
    /// </summary>
    public void TryCatch(StarMovement star)
    {
        if (star == null) return;
        StarCaught?.Invoke(star);
        star.Caught();
    }
}
