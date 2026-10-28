using UnityEngine;

/// <summary>
/// Multi-layer parallax background for Stage 2 and Stage 3.
///
/// Creates 3 SpriteRenderer layers at runtime from the Stage 2 background
/// art (Sky, Buildings, Fence), each moving at a different fraction of
/// camera speed. Slower layers feel farther away.
///
/// Each layer is tiled 3 times (left, center, right) so the background
/// loops seamlessly no matter how far the camera travels.
///
/// Also disables any existing "Background" SpriteRenderer in the scene.
/// </summary>
public class Stage2ParallaxSetup : MonoBehaviour
{
    [Header("Layer Sprites (auto-loaded in Editor)")]
    [SerializeField] private Sprite layer1Sky;
    [SerializeField] private Sprite layer2Buildings;
    [SerializeField] private Sprite layer3Fence;

    [Header("Parallax Speeds (0 = static, 1 = same as camera)")]
    [SerializeField] private float speed1 = 0.05f;
    [SerializeField] private float speed2 = 0.25f;
    [SerializeField] private float speed3 = 0.55f;

    private Transform cam;
    private float camStartX;
    private LoopingParallaxUtility.Layer[] layers;

    private void Start()
    {
        cam = Camera.main != null ? Camera.main.transform : null;
        if (cam == null)
        {
            Debug.LogWarning("[Stage2ParallaxSetup] No main camera found.");
            return;
        }

        camStartX = cam.position.x;

        // Disable the old Background SpriteRenderer so it doesn't block us.
        LoopingParallaxUtility.DisablePlaceholderBackground(nameof(Stage2ParallaxSetup));

        // Auto-load sprites in the Editor if not already assigned.
        LoadSpritesInEditor();

        // Sorting orders: -9 to -7 so they render behind gameplay sprites (0+).
        bool isStage3 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Stage3";
        string stagePrefix = isStage3 ? "S3" : "S2";
        Camera mainCamera = Camera.main;
        layers = new LoopingParallaxUtility.Layer[3];
        layers[0] = LoopingParallaxUtility.CreateLayer(transform, mainCamera, $"BG_{stagePrefix}_L1_Sky", layer1Sky, speed1, 1f, -9, nameof(Stage2ParallaxSetup));
        layers[1] = LoopingParallaxUtility.CreateLayer(transform, mainCamera, $"BG_{stagePrefix}_L2_Buildings", layer2Buildings, speed2, 2f, -8, nameof(Stage2ParallaxSetup));
        layers[2] = LoopingParallaxUtility.CreateLayer(transform, mainCamera, isStage3 ? "BG_S3_L3_Railings" : "BG_S2_L3_Fence", layer3Fence, speed3, 3f, -7, nameof(Stage2ParallaxSetup));

        Debug.Log("[Stage2ParallaxSetup] Background layers created (looping).");
    }

    private void LateUpdate() => LoopingParallaxUtility.UpdateLayers(cam, camStartX, layers);

    // ------------------------------------------------------------------ //

    // ------------------------------------------------------------------ //
    //  Editor-only sprite loading
    // ------------------------------------------------------------------ //

    private void LoadSpritesInEditor()
    {
        bool isStage3 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Stage3";
        if (isStage3)
        {
#if UNITY_EDITOR
            if (layer1Sky == null) layer1Sky = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage3/Layer 1_Sky Stars Background.png");
            if (layer2Buildings == null) layer2Buildings = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage3/Layer 2_Buildings Midground.png");
            if (layer3Fence == null) layer3Fence = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage3/Layer 3_Railings Foreground.png");
#endif
            Debug.Log($"[Stage2ParallaxSetup] Stage3 sprites loaded — Sky:{layer1Sky != null} Buildings:{layer2Buildings != null} Railings:{layer3Fence != null}");
            return;
        }

#if UNITY_EDITOR
        if (layer1Sky == null)
            layer1Sky = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage2/Layer1_Sky.png");
        if (layer2Buildings == null)
            layer2Buildings = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage2/Layer2_Buildings.png");
        if (layer3Fence == null)
            layer3Fence = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage2/Layer3_Fence.png");

        Debug.Log($"[Stage2ParallaxSetup] Sprites loaded — Sky:{layer1Sky != null} Buildings:{layer2Buildings != null} Fence:{layer3Fence != null}");
#else
        if (layer1Sky == null || layer2Buildings == null || layer3Fence == null)
            Debug.LogWarning("[Stage2ParallaxSetup] Background sprites must be assigned in the scene for builds.");
#endif
    }
}
