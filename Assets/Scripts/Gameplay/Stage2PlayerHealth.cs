using UnityEngine;

/// <summary>Tracks the three-hit limit in the Stage 2 turret encounter.</summary>
public class Stage2PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(0f)] private float hitInvulnerability = 0.9f;
    private Stage2TurretEncounter encounter;
    private float invulnerabilityTimer;

    private void Start()
    {
        encounter = FindAnyObjectByType<Stage2TurretEncounter>();
    }

    private void Update()
    {
        invulnerabilityTimer = Mathf.Max(0f, invulnerabilityTimer - Time.deltaTime);
    }

    public void TakeHit()
    {
        if (encounter == null || encounter.IsFinished || invulnerabilityTimer > 0f) return;
        invulnerabilityTimer = hitInvulnerability;
        encounter.OnPlayerHit();
    }
}
