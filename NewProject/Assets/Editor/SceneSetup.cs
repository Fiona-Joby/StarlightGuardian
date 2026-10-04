using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Editor tool that builds the entire MainScene hierarchy from scratch.
/// Run via menu: Tools > Starlight Guardian > Build Scene.
///
/// This guarantees all references are wired correctly — no broken YAML.
/// Safe to re-run: it clears the scene first.
/// </summary>
public class SceneSetup : EditorWindow
{
    [MenuItem("Tools/Starlight Guardian/Build Scene")]
    public static void BuildScene()
    {
        if (!EditorUtility.DisplayDialog(
            "Build Starlight Guardian Scene",
            "This will CLEAR the current scene and rebuild it.\n\nContinue?",
            "Build", "Cancel"))
            return;

        ClearScene();
        CreateScene();
        Debug.Log("[StarlightGuardian] Scene built successfully. Save the scene (Ctrl+S).");
    }

    private static void ClearScene()
    {
        // Destroy all root objects in the scene.
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in roots)
            DestroyImmediate(go);
    }

    private static void CreateScene()
    {
        // ============================================================
        // 1. MAIN CAMERA
        // ============================================================
        var cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        cameraGO.transform.position = new Vector3(0f, 0f, -10f);

        var cam = cameraGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.02f, 0.15f); // dark night sky

        cameraGO.AddComponent<AudioListener>();

        // URP camera data — Unity adds this automatically when URP is active,
        // but we add the CameraFollow script manually.
        var camFollow = cameraGO.AddComponent<CameraFollow>();

        // ============================================================
        // 2. GLOBAL LIGHT 2D
        // ============================================================
        var lightGO = new GameObject("Global Light 2D");
        var light2D = lightGO.AddComponent<Light2D>();
        light2D.lightType = Light2D.LightType.Global;
        light2D.intensity = 1f;
        light2D.color = Color.white;

        // ============================================================
        // 3. PLAYER
        // ============================================================
        var playerGO = new GameObject("Player");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(-6f, -3f, 0f);

        var playerSR = playerGO.AddComponent<SpriteRenderer>();
        playerSR.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        playerSR.color = Color.cyan;
        playerSR.sortingOrder = 10;

        var playerCol = playerGO.AddComponent<BoxCollider2D>();
        playerCol.isTrigger = false;
        playerCol.size = new Vector2(1f, 1f);

        var playerRB = playerGO.AddComponent<Rigidbody2D>();
        playerRB.bodyType = RigidbodyType2D.Kinematic;
        playerRB.gravityScale = 0f;

        playerGO.AddComponent<PlayerInput>();
        playerGO.AddComponent<PlayerMovement>();
        playerGO.AddComponent<WebcamPoseInput>();

        // Animator controller for Run / Idle / Reach states.
        var playerAnimator = playerGO.AddComponent<Animator>();
        var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animations/PlayerAnimator.controller");
        if (animController != null)
            playerAnimator.runtimeAnimatorController = animController;

        // Wire camera to follow player.
        camFollow.player = playerGO.transform;

        // --- CatchZone (child of Player) ---
        var catchZoneGO = new GameObject("CatchZone");
        catchZoneGO.transform.SetParent(playerGO.transform, false);
        catchZoneGO.transform.localPosition = new Vector3(1f, 1f, 0f);

        var catchCol = catchZoneGO.AddComponent<CircleCollider2D>();
        catchCol.isTrigger = true;
        catchCol.radius = 1f;

        catchZoneGO.AddComponent<CatchZone>();

        // ============================================================
        // 4. GROUND (solid, non-trigger)
        // ============================================================
        var groundGO = new GameObject("Ground");
        groundGO.transform.position = new Vector3(22f, -4f, 0f);
        groundGO.transform.localScale = new Vector3(60f, 1f, 1f);

        var groundSR = groundGO.AddComponent<SpriteRenderer>();
        groundSR.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        groundSR.color = new Color(0.15f, 0.1f, 0.25f); // dark ground
        groundSR.sortingOrder = 5;

        var groundCol = groundGO.AddComponent<BoxCollider2D>();
        groundCol.isTrigger = false;

        // ============================================================
        // 5. GROUND DETECTOR (thin trigger, SEPARATE from ground)
        // ============================================================
        var detectorGO = new GameObject("GroundDetectorZone");
        detectorGO.transform.position = new Vector3(0f, -3.45f, 0f);

        var detectorCol = detectorGO.AddComponent<BoxCollider2D>();
        detectorCol.isTrigger = true;
        detectorCol.size = new Vector2(120f, 0.2f);
        detectorCol.offset = new Vector2(20f, 0f);

        var detectorRB = detectorGO.AddComponent<Rigidbody2D>();
        detectorRB.bodyType = RigidbodyType2D.Kinematic;
        detectorRB.gravityScale = 0f;

        detectorGO.AddComponent<GroundDetector>();

        // ============================================================
        // 6. BACKGROUND
        // ============================================================
        var bgGO = new GameObject("Background");
        bgGO.transform.position = new Vector3(22f, 1f, 5f);
        bgGO.transform.localScale = new Vector3(60f, 10f, 1f);

        var bgSR = bgGO.AddComponent<SpriteRenderer>();
        bgSR.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        bgSR.color = new Color(0.08f, 0.04f, 0.2f); // dark sky background
        bgSR.sortingOrder = -10;

        var parallax = bgGO.AddComponent<ParallaxBackground>();
        parallax.cameraTransform = cameraGO.transform;
        parallax.parallaxSpeed = 0.3f;

        // ============================================================
        // 7. STAR PREFAB (create or find)
        // ============================================================
        var starPrefab = CreateStarPrefab();

        // ============================================================
        // 8. STAR SPAWN POINTS
        // ============================================================
        var spawnParent = new GameObject("StarSpawnPoints");
        spawnParent.transform.position = Vector3.zero;

        var sp1GO = new GameObject("StarSpawnPoint_01");
        sp1GO.transform.SetParent(spawnParent.transform, false);
        sp1GO.transform.localPosition = new Vector3(12f, 3f, 0f);

        var sp2GO = new GameObject("StarSpawnPoint_02");
        sp2GO.transform.SetParent(spawnParent.transform, false);
        sp2GO.transform.localPosition = new Vector3(22f, 4f, 0f);

        var sp3GO = new GameObject("StarSpawnPoint_03");
        sp3GO.transform.SetParent(spawnParent.transform, false);
        sp3GO.transform.localPosition = new Vector3(32f, 3f, 0f);

        // ============================================================
        // 9. STAR SPAWNER
        // ============================================================
        var spawnerGO = new GameObject("StarSpawner");
        var spawner = spawnerGO.AddComponent<StarSpawner>();

        // Use SerializedObject to set private serialized fields.
        var so = new SerializedObject(spawner);
        so.FindProperty("starPrefab").objectReferenceValue = starPrefab;

        var spArray = so.FindProperty("spawnPoints");
        spArray.arraySize = 3;

        // Point 1: Normal
        SetSpawnPoint(spArray.GetArrayElementAtIndex(0), sp1GO.transform, 0, 3f, 1.5f);
        // Point 2: Rainbow
        SetSpawnPoint(spArray.GetArrayElementAtIndex(1), sp2GO.transform, 1, 3f, 1.5f);
        // Point 3: Normal
        SetSpawnPoint(spArray.GetArrayElementAtIndex(2), sp3GO.transform, 0, 3f, 1.5f);

        so.FindProperty("spawnInterval").floatValue = 2f;
        so.FindProperty("repeatSequence").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ============================================================
        // Select camera so user can see the scene is built.
        // ============================================================
        Selection.activeGameObject = playerGO;
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

    private static void SetSpawnPoint(SerializedProperty element, Transform point, int type, float hSpeed, float dSpeed)
    {
        element.FindPropertyRelative("point").objectReferenceValue = point;
        element.FindPropertyRelative("starType").enumValueIndex = type;
        element.FindPropertyRelative("horizontalSpeed").floatValue = hSpeed;
        element.FindPropertyRelative("downwardSpeed").floatValue = dSpeed;
    }

    private static StarMovement CreateStarPrefab()
    {
        const string prefabPath = "Assets/Prefabs/Star.prefab";

        // Check if prefab already exists.
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
        {
            var existingSM = existing.GetComponent<StarMovement>();
            if (existingSM != null) return existingSM;
        }

        // Create a new prefab.
        var starGO = new GameObject("Star");

        var sr = starGO.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        sr.color = new Color(1f, 0.85f, 0.2f); // golden-yellow star
        sr.sortingOrder = 8;
        starGO.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        var col = starGO.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        var rb = starGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        var sm = starGO.AddComponent<StarMovement>();

        // Save as prefab.
        var prefab = PrefabUtility.SaveAsPrefabAsset(starGO, prefabPath);
        DestroyImmediate(starGO);

        return prefab.GetComponent<StarMovement>();
    }
}
