using UnityEngine;

/// <summary>
/// Multi-layer parallax background for Stage 1.
///
/// Creates 4 SpriteRenderer layers at runtime, each moving at a different
/// fraction of camera speed. Slower layers feel farther away.
///
/// Each layer is tiled 3 times (left, center, right) so that the background
/// loops seamlessly no matter how far the camera travels.
///
/// Also disables any existing "Background" SpriteRenderer in the scene
/// so the old placeholder doesn't block the new layers.
/// </summary>
public class ParallaxLayerSetup : MonoBehaviour
{
    [Header("Layer Sprites (auto-loaded in Editor)")]
    [SerializeField] private Sprite layer1Sky;
    [SerializeField] private Sprite layer2Hills;
    [SerializeField] private Sprite layer3Wreckage;
    [SerializeField] private Sprite layer4Glow;

    [Header("Parallax Speeds (0 = static, 1 = same as camera)")]
    [SerializeField] private float speed1 = 0.05f;
    [SerializeField] private float speed2 = 0.15f;
    [SerializeField] private float speed3 = 0.35f;
    [SerializeField] private float speed4 = 0.55f;

    private Transform cam;
    private float camStartX;
    private LoopingParallaxUtility.Layer[] layers;

    private void Start()
    {
        cam = Camera.main != null ? Camera.main.transform : null;
        if (cam == null)
        {
            Debug.LogWarning("[ParallaxLayerSetup] No main camera found.");
            return;
        }

        camStartX = cam.position.x;

        // Disable the old Background SpriteRenderer so it doesn't block us.
        LoopingParallaxUtility.DisablePlaceholderBackground(nameof(ParallaxLayerSetup));

        // Auto-load sprites in the Editor if not already assigned.
        LoadSpritesInEditor();

        // Sorting orders: use -9 to -6 so they render in front of the
        // old Background (which was at -10) but behind gameplay sprites (0+).
        Camera mainCamera = Camera.main;
        layers = new LoopingParallaxUtility.Layer[4];
        layers[0] = LoopingParallaxUtility.CreateLayer(transform, mainCamera, "BG_L1_Sky", layer1Sky, speed1, 1f, -9, nameof(ParallaxLayerSetup));
        layers[1] = LoopingParallaxUtility.CreateLayer(transform, mainCamera, "BG_L2_Hills", layer2Hills, speed2, 2f, -8, nameof(ParallaxLayerSetup));
        layers[2] = LoopingParallaxUtility.CreateLayer(transform, mainCamera, "BG_L3_Wreckage", layer3Wreckage, speed3, 3f, -6, nameof(ParallaxLayerSetup));
        layers[3] = LoopingParallaxUtility.CreateLayer(transform, mainCamera, "BG_L4_Glow", layer4Glow, speed4, 4f, -7, nameof(ParallaxLayerSetup));

        Debug.Log("[ParallaxLayerSetup] Background layers created (looping).");
    }

    private void LateUpdate() => LoopingParallaxUtility.UpdateLayers(cam, camStartX, layers);

    // ------------------------------------------------------------------ //

    // ------------------------------------------------------------------ //
    //  Editor-only sprite loading
    // ------------------------------------------------------------------ //

    private void LoadSpritesInEditor()
    {
#if UNITY_EDITOR
        if (layer1Sky == null)
            layer1Sky = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage1/Layer1_Sky.png");
        if (layer2Hills == null)
            layer2Hills = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage1/Layer 2_Hills.png");
        if (layer3Wreckage == null)
            layer3Wreckage = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage1/Layer3_Wreckage.png");
        if (layer4Glow == null)
            layer4Glow = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage1/Layer4_Glow.png");

        Debug.Log($"[ParallaxLayerSetup] Sprites loaded — Sky:{layer1Sky != null} Hills:{layer2Hills != null} Wreckage:{layer3Wreckage != null} Glow:{layer4Glow != null}");
#else
        if (layer1Sky == null || layer2Hills == null || layer3Wreckage == null || layer4Glow == null)
            Debug.LogWarning("[ParallaxLayerSetup] Background sprites must be assigned in the scene for builds.");
#endif
    }
}
