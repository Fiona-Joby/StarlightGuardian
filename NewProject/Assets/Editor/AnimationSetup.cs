using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Editor tool that sets up ALL player animations using Unity's API:
///   - Idle animation (single-frame from Idle_Right sprite)
///   - Reach_Front animation (7 frames)
///   - Animator controller states + transitions
///   - Fixes SpriteRenderer colour to white (not cyan)
///   - Adds WebcamPoseInput if missing
///
/// Run via menu: Tools > Starlight Guardian > Setup Reach Animation
/// </summary>
public class AnimationSetup : EditorWindow
{
    [MenuItem("Tools/Starlight Guardian/Setup Reach Animation")]
    public static void SetupReachAnimation()
    {
        CreateIdleAnimationClip();
        CreateReachAnimationClip();
        UpdateAnimatorController();
        WirePlayerComponents();
        Debug.Log("[StarlightGuardian] Animation setup complete. Save the scene (Ctrl+S).");
    }

    // ------------------------------------------------------------------ //
    //  1a. Create Idle animation clip (single frame)
    // ------------------------------------------------------------------ //

    private static void CreateIdleAnimationClip()
    {
        const string clipPath = "Assets/Animations/Idle.anim";
        const string spritePath = "Assets/Art/Character/Idle/Idle_Right.png";

        Sprite idleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (idleSprite == null)
        {
            Debug.LogError($"[AnimationSetup] Idle sprite not found: {spritePath}");
            return;
        }

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = 8;

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        // Single frame that holds indefinitely.
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[]
        {
            new ObjectReferenceKeyframe { time = 0f, value = idleSprite },
        };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        Debug.Log("[AnimationSetup] Idle.anim created.");
    }

    // ------------------------------------------------------------------ //
    //  1b. Create Reach_Front animation clip (7 frames)
    // ------------------------------------------------------------------ //

    private static void CreateReachAnimationClip()
    {
        const string clipPath = "Assets/Animations/Reach_Front.anim";
        const string spriteFolderPath = "Assets/Art/Character/ReachFront/";
        const float frameRate = 8f;

        Sprite[] sprites = new Sprite[7];
        for (int i = 0; i < 7; i++)
        {
            string spritePath = spriteFolderPath + "ReachFront_" + (i + 1) + ".png";
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprites[i] == null)
            {
                Debug.LogError($"[AnimationSetup] Sprite not found: {spritePath}");
                return;
            }
        }

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = frameRate;

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length + 1];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[i]
            };
        }
        keyframes[sprites.Length] = new ObjectReferenceKeyframe
        {
            time = sprites.Length / frameRate,
            value = sprites[0]
        };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false; // Play once then return to Idle
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        Debug.Log("[AnimationSetup] Reach_Front.anim created/updated.");
    }

    // ------------------------------------------------------------------ //
    //  2. Update the PlayerAnimator controller
    // ------------------------------------------------------------------ //

    private static void UpdateAnimatorController()
    {
        const string controllerPath = "Assets/Animations/PlayerAnimator.controller";

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError("[AnimationSetup] PlayerAnimator.controller not found!");
            return;
        }

        var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Idle.anim");
        var reachClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Reach_Front.anim");

        // --- Add isReaching parameter if missing ---
        bool hasReachingParam = false;
        foreach (var param in controller.parameters)
        {
            if (param.name == "isReaching") { hasReachingParam = true; break; }
        }
        if (!hasReachingParam)
            controller.AddParameter("isReaching", AnimatorControllerParameterType.Bool);

        // --- Find or create states ---
        var rootSM = controller.layers[0].stateMachine;
        AnimatorState idleState = null, runState = null, reachState = null;

        foreach (var cs in rootSM.states)
        {
            if (cs.state.name == "Idle") idleState = cs.state;
            else if (cs.state.name == "Run") runState = cs.state;
            else if (cs.state.name == "Reach") reachState = cs.state;
        }

        // Assign Idle clip to Idle state.
        if (idleState != null && idleClip != null)
        {
            idleState.motion = idleClip;
            Debug.Log("[AnimationSetup] Assigned Idle.anim to Idle state.");
        }

        // Create Reach state if missing.
        if (reachState == null && reachClip != null)
        {
            reachState = rootSM.AddState("Reach", new Vector3(500, 100, 0));
            reachState.motion = reachClip;
            Debug.Log("[AnimationSetup] Created Reach state.");
        }
        else if (reachState != null && reachClip != null)
        {
            reachState.motion = reachClip;
        }

        // --- Transitions ---
        if (idleState != null && reachState != null)
            AddTransitionIfMissing(idleState, reachState, "isReaching", true);
        if (runState != null && reachState != null)
            AddTransitionIfMissing(runState, reachState, "isReaching", true);
        if (reachState != null && idleState != null)
        {
            // Return to Idle after reach animation finishes (HasExitTime = true).
            AddExitTimeTransition(reachState, idleState);
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[AnimationSetup] Animator controller updated.");
    }

    private static void AddTransitionIfMissing(
        AnimatorState from, AnimatorState to, string param, bool value)
    {
        if (from == null || to == null) return;
        foreach (var t in from.transitions)
            if (t.destinationState == to) return; // Already exists

        var tr = from.AddTransition(to);
        tr.hasExitTime = false;
        tr.duration = 0f;
        tr.AddCondition(
            value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, param);
    }

    /// <summary>
    /// Reach → Idle: transition back when the clip finishes (exit time = 1).
    /// Also requires isReaching == false so it doesn't loop.
    /// </summary>
    private static void AddExitTimeTransition(AnimatorState from, AnimatorState to)
    {
        if (from == null || to == null) return;
        foreach (var t in from.transitions)
            if (t.destinationState == to) return;

        var tr = from.AddTransition(to);
        tr.hasExitTime = true;
        tr.exitTime = 1f;
        tr.duration = 0f;
        tr.AddCondition(AnimatorConditionMode.IfNot, 0f, "isReaching");
    }

    // ------------------------------------------------------------------ //
    //  3. Wire Player components in the current scene
    // ------------------------------------------------------------------ //

    private static void WirePlayerComponents()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null)
        {
            Debug.LogWarning("[AnimationSetup] No 'Player' tag found in scene.");
            return;
        }

        // Fix SpriteRenderer — white colour so sprites render with original art.
        var sr = playerGO.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.white;

            // Set the default sprite to Idle_Right so the character is visible.
            var idleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Character/Idle/Idle_Right.png");
            if (idleSprite != null)
                sr.sprite = idleSprite;
        }

        // Ensure Animator.
        var animator = playerGO.GetComponent<Animator>();
        if (animator == null)
            animator = playerGO.AddComponent<Animator>();

        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animations/PlayerAnimator.controller");
        if (controller != null)
            animator.runtimeAnimatorController = controller;

        // Ensure WebcamPoseInput.
        if (playerGO.GetComponent<WebcamPoseInput>() == null)
            playerGO.AddComponent<WebcamPoseInput>();

        EditorUtility.SetDirty(playerGO);
        Debug.Log("[AnimationSetup] Player fixed: white colour, idle sprite, animator, pose input.");
    }
}
