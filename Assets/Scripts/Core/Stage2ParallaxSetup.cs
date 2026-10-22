using UnityEngine;

/// <summary>
/// Multi-layer parallax background for Stage 2.
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
    private LoopingLayer[] layers;

    /// <summary>
    /// Holds the three tiled transforms for a single parallax layer,
    /// plus the data needed to scroll and wrap them.
    /// </summary>
    private struct LoopingLayer
    {
        /// <summary>Parent transform that receives the parallax offset.</summary>
        public Transform root;
        /// <summary>The three tile transforms (children of root).</summary>
        public Transform[] tiles;     // length 3: left, center, right
        public float parallax;
        public float tileWorldWidth;   // width of one tile in world units
        public float rootStartX;
    }

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
        DisableOldBackground();

        // Auto-load sprites in the Editor if not already assigned.
        LoadSpritesInEditor();

        // Sorting orders: -9 to -7 so they render behind gameplay sprites (0+).
        layers = new LoopingLayer[3];
        layers[0] = MakeLayer("BG_S2_L1_Sky",       layer1Sky,       speed1, 1f, -9);
        layers[1] = MakeLayer("BG_S2_L2_Buildings",  layer2Buildings,  speed2, 2f, -8);
        layers[2] = MakeLayer("BG_S2_L3_Fence",     layer3Fence,     speed3, 3f, -7);

        Debug.Log("[Stage2ParallaxSetup] Background layers created (looping).");
    }

    private void LateUpdate()
    {
        if (cam == null || layers == null) return;

        for (int i = 0; i < layers.Length; i++)
        {
            ref LoopingLayer l = ref layers[i];
            if (l.root == null) continue;

            // 1) Apply parallax offset to the root.
            float dx = cam.position.x - camStartX;
            float rootX = l.rootStartX + dx * l.parallax;
            l.root.position = new Vector3(rootX, l.root.position.y, l.root.position.z);

            // 2) Wrap tiles so one always covers the camera.
            float camRelX = cam.position.x - rootX;
            float w = l.tileWorldWidth;

            float centerSlot = Mathf.Round(camRelX / w);

            for (int t = 0; t < l.tiles.Length; t++)
            {
                float slotOffset = (t - 1) + centerSlot;
                l.tiles[t].localPosition = new Vector3(slotOffset * w, 0f, 0f);
            }
        }
    }

    // ------------------------------------------------------------------ //

    private void DisableOldBackground()
    {
        var old = GameObject.Find("Background");
        if (old != null)
        {
            var sr = old.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = false;
                Debug.Log("[Stage2ParallaxSetup] Disabled old Background SpriteRenderer.");
            }
        }
    }

    private LoopingLayer MakeLayer(string name, Sprite sprite, float parallax, float z, int order)
    {
        LoopingLayer ll = new LoopingLayer { parallax = parallax };

        if (sprite == null)
        {
            Debug.LogWarning($"[Stage2ParallaxSetup] {name} sprite is null — skipping.");
            return ll;
        }

        var root = new GameObject(name);
        root.transform.SetParent(transform);
        root.transform.position = new Vector3(cam.position.x, cam.position.y, z);

        float camH = Camera.main.orthographicSize * 2f;
        float spriteH = sprite.bounds.size.y;
        float scale = camH / spriteH;

        float tileWorldWidth = sprite.bounds.size.x * scale;

        Transform[] tiles = new Transform[3];
        for (int t = 0; t < 3; t++)
        {
            float offset = (t - 1) * tileWorldWidth;
            var tile = new GameObject($"{name}_Tile{t}");
            tile.transform.SetParent(root.transform);
            tile.transform.localPosition = new Vector3(offset, 0f, 0f);
            tile.transform.localScale = new Vector3(scale, scale, 1f);

            var sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = Color.white;

            tiles[t] = tile.transform;
        }

        ll.root = root.transform;
        ll.tiles = tiles;
        ll.tileWorldWidth = tileWorldWidth;
        ll.rootStartX = root.transform.position.x;

        Debug.Log($"[Stage2ParallaxSetup] {name}: pos=({root.transform.position.x:F1},{root.transform.position.y:F1},{z}), scale={scale:F2}, tileW={tileWorldWidth:F2}, sortOrder={order}");

        return ll;
    }

    // ------------------------------------------------------------------ //
    //  Editor-only sprite loading
    // ------------------------------------------------------------------ //

    private void LoadSpritesInEditor()
    {
#if UNITY_EDITOR
        if (layer1Sky == null)
            layer1Sky = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage2/Layer1_Sky.png");
        if (layer2Buildings == null)
            layer2Buildings = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage2/Layer2_Buildings.png");
        if (layer3Fence == null)
            layer3Fence = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage2/Layer3_Fence.png");

        Debug.Log($"[Stage2ParallaxSetup] Sprites loaded — Sky:{layer1Sky != null} Buildings:{layer2Buildings != null} Fence:{layer3Fence != null}");
#else
        Debug.LogWarning("[Stage2ParallaxSetup] Not in Editor — sprites must be assigned in Inspector.");
#endif
    }
}
