using UnityEngine;

/// <summary>Moves a platform gently from side to side around its spawn point.</summary>
public class PlatformMover : MonoBehaviour
{
    [SerializeField, Min(0f)] private float travelDistance = 0.8f;
    [SerializeField, Min(0.1f)] private float cyclesPerSecond = 0.22f;

    private float centerX;
    private float phase;

    private void Awake()
    {
        centerX = transform.position.x;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float x = centerX + Mathf.Sin(Time.time * cyclesPerSecond * Mathf.PI * 2f + phase) * travelDistance;
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }
}
