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
            Debug.LogWarning("[ParallaxLayerSetup] No main camera found.");
            return;
        }

        camStartX = cam.position.x;

        // Disable the old Background SpriteRenderer so it doesn't block us.
        DisableOldBackground();

        // Auto-load sprites in the Editor if not already assigned.
        LoadSpritesInEditor();

        // Sorting orders: use -9 to -6 so they render in front of the
        // old Background (which was at -10) but behind gameplay sprites (0+).
        layers = new LoopingLayer[4];
        layers[0] = MakeLayer("BG_L1_Sky",      layer1Sky,      speed1, 1f, -9);
        layers[1] = MakeLayer("BG_L2_Hills",    layer2Hills,    speed2, 2f, -8);
        layers[2] = MakeLayer("BG_L3_Wreckage", layer3Wreckage, speed3, 3f, -7);
        layers[3] = MakeLayer("BG_L4_Glow",     layer4Glow,     speed4, 4f, -6);

        Debug.Log("[ParallaxLayerSetup] Background layers created (looping).");
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
            //    We compute the camera's X relative to the root, then
            //    re-center the three tiles around the nearest "slot".
            float camRelX = cam.position.x - rootX;
            float w = l.tileWorldWidth;

            // Which tile slot the camera is closest to.
            // Mathf.Round gives us the nearest integer tile index.
            float centerSlot = Mathf.Round(camRelX / w);

            for (int t = 0; t < l.tiles.Length; t++)
            {
                // tiles[0] = centerSlot - 1, tiles[1] = centerSlot, tiles[2] = centerSlot + 1
                float slotOffset = (t - 1) + centerSlot;
                l.tiles[t].localPosition = new Vector3(slotOffset * w, 0f, 0f);
            }
        }
    }

    // ------------------------------------------------------------------ //

    /// <summary>
    /// Finds the old "Background" GameObject in the scene and disables its
    /// SpriteRenderer so it doesn't render on top of the new parallax layers.
    /// </summary>
    private void DisableOldBackground()
    {
        // Find by name.
        var old = GameObject.Find("Background");
        if (old != null)
        {
            var sr = old.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = false;
                Debug.Log("[ParallaxLayerSetup] Disabled old Background SpriteRenderer.");
            }
            // Also disable the old ParallaxBackground script if present.
            var oldScript = old.GetComponent<ParallaxBackground>();
            if (oldScript != null)
            {
                oldScript.enabled = false;
            }
        }
    }

    /// <summary>
    /// Creates one parallax layer with 3 tiled copies so it loops infinitely.
    /// </summary>
    private LoopingLayer MakeLayer(string name, Sprite sprite, float parallax, float z, int order)
    {
        LoopingLayer ll = new LoopingLayer { parallax = parallax };

        if (sprite == null)
        {
            Debug.LogWarning($"[ParallaxLayerSetup] {name} sprite is null — skipping.");
            return ll;
        }

        // Root object: receives the parallax offset.
        var root = new GameObject(name);
        root.transform.SetParent(transform);
        root.transform.position = new Vector3(cam.position.x, cam.position.y, z);

        // Scale factor: fill the camera viewport height.
        float camH = Camera.main.orthographicSize * 2f;
        float spriteH = sprite.bounds.size.y;
        float scale = camH / spriteH;

        // Tile width in world units (sprite width * uniform scale).
        float tileWorldWidth = sprite.bounds.size.x * scale;

        // Create 3 tiles: left (-1), center (0), right (+1).
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

        Debug.Log($"[ParallaxLayerSetup] {name}: pos=({root.transform.position.x:F1},{root.transform.position.y:F1},{z}), scale={scale:F2}, tileW={tileWorldWidth:F2}, sortOrder={order}");

        return ll;
    }

    // ------------------------------------------------------------------ //
    //  Editor-only sprite loading
    // ------------------------------------------------------------------ //

    private void LoadSpritesInEditor()
    {
#if UNITY_EDITOR
        if (layer1Sky == null)
            layer1Sky = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage1/Layer1_Sky.png");
        if (layer2Hills == null)
            layer2Hills = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage1/Layer2_Hills.png");
        if (layer3Wreckage == null)
            layer3Wreckage = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage1/Layer3_Wreckage.png");
        if (layer4Glow == null)
            layer4Glow = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/Stage1/Layer4_Glow.png");

        Debug.Log($"[ParallaxLayerSetup] Sprites loaded — Sky:{layer1Sky != null} Hills:{layer2Hills != null} Wreckage:{layer3Wreckage != null} Glow:{layer4Glow != null}");
#else
        Debug.LogWarning("[ParallaxLayerSetup] Not in Editor — sprites must be assigned in Inspector.");
#endif
    }
}
