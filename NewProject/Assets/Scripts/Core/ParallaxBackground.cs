using UnityEngine;

/// <summary>
/// Scrolls the background at a fraction of camera movement speed to create depth.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    public Transform cameraTransform;
    [Range(0f, 1f)] public float parallaxSpeed = 0.3f;

    private float startCameraX;
    private float startBackgroundX;

    private void Start()
    {
        if (cameraTransform == null) return;
        startCameraX = cameraTransform.position.x;
        startBackgroundX = transform.position.x;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        float cameraMovement = cameraTransform.position.x - startCameraX;
        transform.position = new Vector3(
            startBackgroundX + cameraMovement * parallaxSpeed,
            transform.position.y,
            transform.position.z
        );
    }
}
