using UnityEngine;

/// <summary>Forwards player damage to the active Stage 2 or Stage 3 encounter.</summary>
public class Stage2PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(0f)] private float hitInvulnerability = 0.9f;
    private Stage2TurretEncounter encounter;
    private Stage3DroneEncounter stage3Encounter;
    private float invulnerabilityTimer;

    private void Start()
    {
        encounter = FindAnyObjectByType<Stage2TurretEncounter>();
        stage3Encounter = FindAnyObjectByType<Stage3DroneEncounter>();
    }

    private void Update()
    {
        invulnerabilityTimer = Mathf.Max(0f, invulnerabilityTimer - Time.deltaTime);
    }

    public void TakeHit()
    {
        if ((encounter == null && stage3Encounter == null) ||
            (encounter != null && encounter.IsFinished) ||
            (stage3Encounter != null && stage3Encounter.IsFinished) ||
            invulnerabilityTimer > 0f) return;
        invulnerabilityTimer = hitInvulnerability;
        if (encounter != null) encounter.OnPlayerHit();
        else stage3Encounter.OnPlayerHit();
    }
}
