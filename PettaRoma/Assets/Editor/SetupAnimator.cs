/*  ================================================================
 *  SetupAnimator.cs  —  Editor-only
 *  Menu: Petta ▸ Setup Animator
 *
 *  Creates an Animator Controller with Walk animation,
 *  adds Animator to the player model, and wires everything.
 *  ================================================================ */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public static class SetupAnimator
{
    [MenuItem("Petta/Setup Animator")]
    public static void Setup()
    {
        // ── 1. Find Player ──
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Errore", "Player non trovato!", "OK");
            return;
        }

        // ── 2. Find the model inside Player ──
        Transform modelPivot = player.transform.Find("ModelPivot");
        Transform characterModel = null;
        if (modelPivot != null)
            characterModel = modelPivot.Find("CharacterModel");
        
        if (characterModel == null)
        {
            EditorUtility.DisplayDialog("Errore",
                "CharacterModel non trovato! Esegui prima 'Petta > Apply Character Model'.", "OK");
            return;
        }

        // ── 3. Find animation clips ──
        string animFolder = "Assets/_Player/Animations";
        AnimationClip walkClip = FindClip(animFolder, "walk");
        AnimationClip idleClip = FindClip(animFolder, "idle");
        AnimationClip runClip = FindClip(animFolder, "run");
        AnimationClip jumpClip = FindClip(animFolder, "jump");

        if (walkClip == null)
        {
            EditorUtility.DisplayDialog("Errore",
                "Animazione 'walk' non trovata in Assets/_Player/Animations/!\n" +
                "Il file FBX deve contenere 'walk' nel nome.", "OK");
            return;
        }

        // ── 4. Create Animator Controller ──
        string controllerFolder = "Assets/_Player/Animations";
        EnsureFolder(controllerFolder);
        string controllerPath = controllerFolder + "/PettaAnimator.controller";

        // Delete old controller if it exists
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            AssetDatabase.DeleteAsset(controllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // ── 5. Add parameters ──
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);

        // ── 6. Setup states ──
        var rootStateMachine = controller.layers[0].stateMachine;

        // Idle state (default = standing still or walk at speed 0)
        AnimatorState idleState;
        if (idleClip != null)
        {
            idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;
        }
        else
        {
            // Use walk clip at speed 0 as idle placeholder
            idleState = rootStateMachine.AddState("Idle");
            idleState.motion = null; // no clip = T-pose, but better than nothing
            Debug.Log("[Animator] No idle clip found — using empty state. Add idle animation later.");
        }
        rootStateMachine.defaultState = idleState;

        // Walk state
        var walkState = rootStateMachine.AddState("Walk");
        walkState.motion = walkClip;

        // Run state (if available)
        AnimatorState runState = null;
        if (runClip != null)
        {
            runState = rootStateMachine.AddState("Run");
            runState.motion = runClip;
        }

        // Jump state (if available)
        AnimatorState jumpState = null;
        if (jumpClip != null)
        {
            jumpState = rootStateMachine.AddState("Jump");
            jumpState.motion = jumpClip;
        }

        // ── 7. Add transitions ──
        // Idle → Walk (Speed > 0.1)
        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.15f;

        // Walk → Idle (Speed < 0.1)
        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.15f;

        if (runState != null)
        {
            // Walk → Run (Speed > 0.8)
            var walkToRun = walkState.AddTransition(runState);
            walkToRun.AddCondition(AnimatorConditionMode.Greater, 0.8f, "Speed");
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.15f;

            // Run → Walk (Speed < 0.8)
            var runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.Less, 0.8f, "Speed");
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.15f;

            // Run → Idle
            var runToIdle = runState.AddTransition(idleState);
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.15f;
        }

        if (jumpState != null)
        {
            // Any → Jump (IsJumping = true)
            var anyToJump = rootStateMachine.AddAnyStateTransition(jumpState);
            anyToJump.AddCondition(AnimatorConditionMode.If, 0, "IsJumping");
            anyToJump.hasExitTime = false;
            anyToJump.duration = 0.1f;

            // Jump → Idle (IsGrounded = true)
            var jumpToIdle = jumpState.AddTransition(idleState);
            jumpToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            jumpToIdle.hasExitTime = false;
            jumpToIdle.duration = 0.2f;
        }

        AssetDatabase.SaveAssets();

        // ── 8. Add Animator component to model ──
        // First check if there's also an animated FBX (withSkin) that should replace the static model
        var animatedFbx = FindAnimatedModel(animFolder);
        
        Animator animator;
        if (animatedFbx != null)
        {
            Debug.Log($"[Animator] Found animated model: {animatedFbx}. Consider using this as the character model.");
        }

        animator = characterModel.gameObject.GetComponent<Animator>();
        if (animator == null)
            animator = characterModel.gameObject.AddComponent<Animator>();

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false; // we control movement via CharacterController
        
        // Try to set avatar from the animated FBX if the model doesn't have one
        if (animator.avatar == null && animatedFbx != null)
        {
            var animModelImporter = AssetImporter.GetAtPath(animatedFbx) as ModelImporter;
            if (animModelImporter != null)
            {
                // Load the avatar from the animated FBX
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(animatedFbx);
                foreach (var asset in allAssets)
                {
                    if (asset is Avatar avatar)
                    {
                        animator.avatar = avatar;
                        Debug.Log($"[Animator] Assigned avatar from animated FBX: {avatar.name}");
                        break;
                    }
                }
            }
        }

        // ── 9. Mark dirty ──
        EditorUtility.SetDirty(animator);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        string msg = $"Animator creato!\n\nStati:\n• Idle{(idleClip != null ? " ✓" : " (vuoto)")}\n• Walk ✓";
        if (runClip != null) msg += "\n• Run ✓";
        if (jumpClip != null) msg += "\n• Jump ✓";
        msg += "\n\nPremi Play per testare!";

        Debug.Log($"<color=#B11E23><b>🍕 {msg}</b></color>");
        EditorUtility.DisplayDialog("Animator Pronto!", msg, "OK");
    }

    static AnimationClip FindClip(string folder, string keyword)
    {
        var guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.ToLower().Contains(keyword))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                {
                    Debug.Log($"[Animator] Found {keyword} clip: {path}");
                    return clip;
                }
            }
        }

        // Also check inside FBX files for embedded clips
        var fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { folder });
        foreach (var guid in fbxGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.ToLower().Contains(keyword) || path.ToLower().Contains("anim"))
            {
                var objects = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var obj in objects)
                {
                    if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        Debug.Log($"[Animator] Found {keyword} clip inside FBX: {path} → {clip.name}");
                        return clip;
                    }
                }
            }
        }

        return null;
    }

    static string FindAnimatedModel(string folder)
    {
        var guids = AssetDatabase.FindAssets("t:Model", new[] { folder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string lower = path.ToLower();
            if (lower.Contains("withskin") || lower.Contains("animated") || lower.Contains("rigged"))
                return path;
        }
        return null;
    }

    static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
