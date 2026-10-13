using System;
using System.Collections.Generic;
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

    private readonly HashSet<StarMovement> overlappingStars = new HashSet<StarMovement>();
    private PlayerInput playerInput;

    private void Awake()
    {
        // Ensure the collider is a trigger.
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        playerInput = GetComponentInParent<PlayerInput>();
    }

    private void Update()
    {
        if (playerInput == null || !playerInput.CollectPressed) return;

        // Copy first because Caught() destroys stars, which removes them from
        // the overlap set through OnTriggerExit2D.
        var stars = new List<StarMovement>(overlappingStars);
        foreach (StarMovement star in stars)
            TryCatch(star);
    }

    /// <summary>
    /// Called when a star overlaps this zone. Contact alone never collects it.
    /// </summary>
    public void RegisterStar(StarMovement star)
    {
        if (star != null) overlappingStars.Add(star);
    }

    public void UnregisterStar(StarMovement star)
    {
        if (star != null) overlappingStars.Remove(star);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        StarMovement star = other.GetComponent<StarMovement>();
        if (star != null) RegisterStar(star);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        StarMovement star = other.GetComponent<StarMovement>();
        if (star != null) UnregisterStar(star);
    }

    private void TryCatch(StarMovement star)
    {
        if (star == null || !overlappingStars.Contains(star)) return;
        StarCaught?.Invoke(star);
        star.Caught();
        overlappingStars.Remove(star);
    }
}
