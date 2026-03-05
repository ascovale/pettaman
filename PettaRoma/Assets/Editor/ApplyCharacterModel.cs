/*  ================================================================
 *  ApplyCharacterModel.cs  —  Editor-only
 *  Menu: Petta ▸ Apply Character Model
 *
 *  Replaces the Player's primitive visuals (capsule/sphere/cylinder)
 *  with a Meshy FBX model from Assets/_Player/Models/.
 *  Shows a selection dialog if multiple FBX files are found.
 *  ================================================================ */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;

public static class ApplyCharacterModel
{
    [MenuItem("Petta/Apply Character Model")]
    public static void Apply()
    {
        // ── 1. Find Player ──
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Player");
        }
        if (player == null)
        {
            EditorUtility.DisplayDialog("Errore",
                "Player non trovato! Esegui prima 'Petta > Setup Player & Managers'.", "OK");
            return;
        }

        // ── 2. Find all FBX models in _Player/Models ──
        string modelsFolder = "Assets/_Player/Models";
        if (!AssetDatabase.IsValidFolder(modelsFolder))
        {
            EditorUtility.DisplayDialog("Errore",
                "Cartella Assets/_Player/Models/ non trovata!", "OK");
            return;
        }

        var fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { modelsFolder });
        
        List<string> fbxPaths = new List<string>();
        List<string> fbxNames = new List<string>();

        foreach (var guid in fbxGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".obj", System.StringComparison.OrdinalIgnoreCase))
            {
                fbxPaths.Add(path);
                fbxNames.Add(Path.GetFileNameWithoutExtension(path));
            }
        }

        // Also add the rigged animated model from Animations folder
        string animFolder = "Assets/_Player/Animations";
        if (AssetDatabase.IsValidFolder(animFolder))
        {
            var animGuids = AssetDatabase.FindAssets("t:Model", new[] { animFolder });
            foreach (var guid in animGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path).ToLower();
                // Only include Walking_withSkin (rigged model with walk animation)
                // Skip Character_output (static), Jump, Run (pure animation clips)
                if (fileName.Contains("walking") && path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    fbxPaths.Add(path);
                    fbxNames.Add(Path.GetFileNameWithoutExtension(path) + " ★ ANIMATO");
                }
            }
        }

        if (fbxPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Errore",
                "Nessun modello FBX trovato in Assets/_Player/Models/!\n" +
                "Trascina il file .fbx scaricato da Meshy in quella cartella.", "OK");
            return;
        }

        // ── 3. Let user pick which model ──
        int selected = 0;
        if (fbxPaths.Count > 1)
        {
            // Build description with numbered list
            string desc = "Quale modello vuoi applicare al Player?\n\n";
            for (int i = 0; i < fbxNames.Count; i++)
            {
                string marker = fbxPaths[i].ToLower().Contains("withskin") ? " ★ (con scheletro!)" : "";
                desc += $"{i + 1}. {fbxNames[i]}{marker}\n";
            }
            desc += "\nScegli il numero:";

            // Show input dialog — user types the number
            string input = EditorInputDialog.Show("Scegli Modello", desc, "1");
            if (string.IsNullOrEmpty(input)) return;
            
            if (int.TryParse(input.Trim(), out int choice) && choice >= 1 && choice <= fbxPaths.Count)
            {
                selected = choice - 1;
            }
            else
            {
                EditorUtility.DisplayDialog("Errore", "Numero non valido!", "OK");
                return;
            }
        }

        ApplyModel(player, fbxPaths[selected]);
    }

    static void ApplyModel(GameObject player, string fbxPath)
    {
        // ── 3b. Configure FBX import settings ──
        ConfigureFbxImport(fbxPath);

        // Also configure any FBX in the Animations folder (for looping)
        // AND set them all to use the same avatar source (the walking FBX)
        string animFolderCheck = "Assets/_Player/Animations";
        string walkingFbxPath = FindWalkingFbx(animFolderCheck);
        if (AssetDatabase.IsValidFolder(animFolderCheck))
        {
            var animGuids = AssetDatabase.FindAssets("t:Model", new[] { animFolderCheck });
            foreach (var guid in animGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ConfigureFbxImport(path);
                // Force all animation FBXs to copy avatar from the walking model
                if (walkingFbxPath != null && path != walkingFbxPath)
                    CopyAvatarSource(path, walkingFbxPath);
            }
        }
        AssetDatabase.Refresh();

        // ── 4. Load the FBX as prefab ──
        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (modelPrefab == null)
        {
            Debug.LogError($"[ApplyModel] Impossibile caricare: {fbxPath}");
            return;
        }

        // ── 5. Remove ALL old visuals (aggressive cleanup) ──
        // Remove named primitives
        string[] oldVisuals = { "Body", "Head", "ChefHat", "CharacterModel" };
        foreach (var name in oldVisuals)
        {
            var old = player.transform.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        // Remove previous ModelPivot (and everything inside it)
        var prevPivot = player.transform.Find("ModelPivot");
        if (prevPivot != null) Object.DestroyImmediate(prevPivot.gameObject);

        // Also kill any stray Renderers/SkinnedMeshRenderers directly on Player children
        // (but not the Player itself)
        for (int i = player.transform.childCount - 1; i >= 0; i--)
        {
            var child = player.transform.GetChild(i);
            if (child.GetComponent<Renderer>() != null || child.GetComponent<SkinnedMeshRenderer>() != null)
            {
                Debug.Log($"[ApplyModel] Removing stray visual child: {child.name}");
                Object.DestroyImmediate(child.gameObject);
            }
        }

        // ── 6. Create ModelPivot + instantiate model ──
        var pivot = new GameObject("ModelPivot");
        pivot.transform.SetParent(player.transform, false);
        pivot.transform.localPosition = Vector3.zero;
        pivot.transform.localRotation = Quaternion.identity;

        var modelInstance = (GameObject)Object.Instantiate(modelPrefab);
        modelInstance.name = "CharacterModel";
        modelInstance.transform.SetParent(pivot.transform, false);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localScale = Vector3.one;

        // Remove any colliders from the model (CharacterController handles collision)
        foreach (var col in modelInstance.GetComponentsInChildren<Collider>())
        {
            Object.DestroyImmediate(col);
        }

        // ── 6b. Fix rotation ──
        // With bakeAxisConversion, the model might already be upright
        // Check bounds to see if it needs rotation
        Bounds checkBounds = CalculateBounds(modelInstance);
        if (checkBounds.size.y < checkBounds.size.x * 0.5f || checkBounds.size.y < checkBounds.size.z * 0.5f)
        {
            // Still lying flat, apply -90 X
            modelInstance.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            Debug.Log("[ApplyModel] Model was flat after bake, applied -90 X rotation fix");
        }
        else
        {
            // Upright but facing wrong way — rotate 180 on Y
            modelInstance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Debug.Log("[ApplyModel] Model is upright, applied 180 Y to fix facing direction");
        }

        // ── 7. Auto-scale: measure bounds and fit to ~1.8 units tall ──
        float targetHeight = 1.8f;
        
        Bounds bounds = CalculateBounds(modelInstance);
        Debug.Log($"[ApplyModel] Raw bounds: center={bounds.center}, size={bounds.size}, min={bounds.min}, max={bounds.max}");

        if (bounds.size.y > 0.001f)
        {
            float currentHeight = bounds.size.y;
            float scaleFactor = targetHeight / currentHeight;
            scaleFactor = Mathf.Clamp(scaleFactor, 0.001f, 100f);
            
            modelInstance.transform.localScale = Vector3.one * scaleFactor;
            // Keep model at local Y=0 — with bakeAxisConversion the origin is at the feet
            modelInstance.transform.localPosition = Vector3.zero;

            Debug.Log($"[ApplyModel] Scaled {scaleFactor:F3}x → target height ~{targetHeight}m");
        }
        else
        {
            modelInstance.transform.localScale = Vector3.one * 0.01f;
            modelInstance.transform.localPosition = Vector3.zero;
            Debug.LogWarning("[ApplyModel] Could not measure bounds. Applied default scale 0.01.");
        }

        // ── 8. Wire ModelPivot as modelRoot on PlayerController ──
        var pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            var so = new SerializedObject(pc);
            so.FindProperty("modelRoot").objectReferenceValue = pivot.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 9. Disable shadow-casting on primitive collider remnants ──
        // (CharacterController handles collision, model is visual only)

        // ── 10. Setup URP materials with correct textures ──
        string modelFolder = Path.GetDirectoryName(fbxPath);
        SetupMaterials(modelInstance, modelFolder);

        // ── 11. Auto-setup Animator if model has animation clips ──
        bool animSetup = SetupAnimatorAuto(modelInstance, fbxPath);

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        string modelName = Path.GetFileNameWithoutExtension(fbxPath);
        Debug.Log($"<color=#B11E23><b>🍕 Modello '{modelName}' applicato al Player!</b></color>");

        string animMsg = animSetup
            ? "\n\n✓ Animator configurato automaticamente con animazione Walk!"
            : "\n\nNessuna animazione trovata nel modello.";

        EditorUtility.DisplayDialog("Modello Applicato!",
            $"Il modello '{modelName}' è stato assegnato al Player.{animMsg}\n\n" +
            "Premi Play per testare!", "OK");
    }

    // ═══════════════════════════════════════════════════════
    //  AUTO ANIMATOR SETUP
    // ═══════════════════════════════════════════════════════
    static bool SetupAnimatorAuto(GameObject modelInstance, string fbxPath)
    {
        // Find ALL animation clips: first from the model FBX itself, then from the Animations folder
        AnimationClip walkClip = null, idleClip = null, runClip = null, jumpClip = null;

        // Search inside the selected FBX for embedded clips
        var embeddedAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (var asset in embeddedAssets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                string clipLower = clip.name.ToLower();
                if (clipLower.Contains("walk") && walkClip == null) walkClip = clip;
                else if (clipLower.Contains("idle") && idleClip == null) idleClip = clip;
                else if (clipLower.Contains("run") && runClip == null) runClip = clip;
                else if (clipLower.Contains("jump") && jumpClip == null) jumpClip = clip;
                else if (walkClip == null) walkClip = clip; // first unknown clip → use as walk
            }
        }

        // Also search Animations folder for additional clips
        string animFolder = "Assets/_Player/Animations";
        if (AssetDatabase.IsValidFolder(animFolder))
        {
            var fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { animFolder });
            foreach (var guid in fbxGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string pathLower = path.ToLower();
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var asset in allAssets)
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        string clipLower = clip.name.ToLower();
                        // Match by clip name OR by FBX filename
                        bool isWalk = clipLower.Contains("walk") || (pathLower.Contains("walk") && !pathLower.Contains("run"));
                        bool isIdle = clipLower.Contains("idle") || pathLower.Contains("idle");
                        bool isRun  = clipLower.Contains("run")  || (pathLower.Contains("run") && !pathLower.Contains("regular"));
                        bool isJump = clipLower.Contains("jump") || (pathLower.Contains("jump") && !pathLower.Contains("run"));

                        if (isWalk && walkClip == null) walkClip = clip;
                        else if (isIdle && idleClip == null) idleClip = clip;
                        else if (isRun && runClip == null) runClip = clip;
                        else if (isJump && jumpClip == null) jumpClip = clip;
                    }
                }
            }
        }

        if (walkClip == null)
        {
            Debug.Log("[ApplyModel] Nessuna animazione trovata, Animator non configurato.");
            return false;
        }

        Debug.Log($"[ApplyModel] Animazioni trovate: walk={walkClip?.name}, idle={idleClip?.name}, run={runClip?.name}, jump={jumpClip?.name}");

        // ── Create or reuse AnimatorController ──
        string controllerFolder = "Assets/_Player/Animations";
        EnsureFolder(controllerFolder);
        string controllerPath = controllerFolder + "/PettaAnimator.controller";

        if (AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath) != null)
            AssetDatabase.DeleteAsset(controllerPath);

        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;

        // Idle state
        var idleState = sm.AddState("Idle");
        idleState.motion = idleClip; // null = T-pose fallback
        sm.defaultState = idleState;

        // Walk state
        var walkState = sm.AddState("Walk");
        walkState.motion = walkClip;

        // Idle → Walk
        var t1 = idleState.AddTransition(walkState);
        t1.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.1f, "Speed");
        t1.hasExitTime = false;
        t1.duration = 0.15f;

        // Walk → Idle
        var t2 = walkState.AddTransition(idleState);
        t2.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.1f, "Speed");
        t2.hasExitTime = false;
        t2.duration = 0.15f;

        // Run state
        if (runClip != null)
        {
            var runState = sm.AddState("Run");
            runState.motion = runClip;
            var tr1 = walkState.AddTransition(runState);
            tr1.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.8f, "Speed");
            tr1.hasExitTime = false; tr1.duration = 0.15f;
            var tr2 = runState.AddTransition(walkState);
            tr2.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.8f, "Speed");
            tr2.hasExitTime = false; tr2.duration = 0.15f;
        }

        // Jump state
        if (jumpClip != null)
        {
            var jumpState = sm.AddState("Jump");
            jumpState.motion = jumpClip;
            var tj = sm.AddAnyStateTransition(jumpState);
            tj.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsJumping");
            tj.hasExitTime = false; tj.duration = 0.1f;
            var tj2 = jumpState.AddTransition(idleState);
            tj2.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsGrounded");
            tj2.hasExitTime = false; tj2.duration = 0.2f;
        }

        AssetDatabase.SaveAssets();

        // ── Wire Animator to model ──
        Animator animator = modelInstance.GetComponent<Animator>();
        if (animator == null)
            animator = modelInstance.AddComponent<Animator>();

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        // Ensure avatar is set (load from the FBX if needed)
        if (animator.avatar == null)
        {
            foreach (var asset in embeddedAssets)
            {
                if (asset is Avatar av)
                {
                    animator.avatar = av;
                    Debug.Log($"[ApplyModel] Avatar assegnato: {av.name}");
                    break;
                }
            }
        }

        // If still no avatar, check animated FBXs in the Animation folder
        if (animator.avatar == null && AssetDatabase.IsValidFolder(animFolder))
        {
            var fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { animFolder });
            foreach (var guid in fbxGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.ToLower().Contains("withskin") || path.ToLower().Contains("rigged"))
                {
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        if (asset is Avatar av)
                        {
                            animator.avatar = av;
                            Debug.Log($"[ApplyModel] Avatar da FBX animato: {av.name}");
                            break;
                        }
                    }
                    if (animator.avatar != null) break;
                }
            }
        }

        EditorUtility.SetDirty(animator);
        Debug.Log($"[ApplyModel] Animator configurato: controller={controllerPath}, avatar={animator.avatar?.name ?? "NONE"}");
        return true;
    }

    // ═══════════════════════════════════════════════════════
    //  FBX IMPORT CONFIGURATION
    // ═══════════════════════════════════════════════════════
    static void ConfigureFbxImport(string fbxPath)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        bool needsReimport = false;

        // Set animation type to Generic
        if (importer.animationType != ModelImporterAnimationType.Generic)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            needsReimport = true;
        }

        // Bake axis conversion
        if (!importer.bakeAxisConversion)
        {
            importer.bakeAxisConversion = true;
            needsReimport = true;
        }

        // ── Set animation clips loop based on type ──
        // Walk/Run/Idle = loop, Jump = no loop
        var clipAnimations = importer.clipAnimations;
        if (clipAnimations == null || clipAnimations.Length == 0)
        {
            clipAnimations = importer.defaultClipAnimations;
        }

        string pathLower = fbxPath.ToLower();
        bool isJump = pathLower.Contains("jump") && !pathLower.Contains("run");
        bool shouldLoop = !isJump; // everything loops except pure jump

        if (clipAnimations != null && clipAnimations.Length > 0)
        {
            bool changed = false;
            for (int i = 0; i < clipAnimations.Length; i++)
            {
                if (clipAnimations[i].loopTime != shouldLoop)
                {
                    clipAnimations[i].loopTime = shouldLoop;
                    clipAnimations[i].loopPose = shouldLoop;
                    changed = true;
                }
            }
            if (changed)
            {
                importer.clipAnimations = clipAnimations;
                needsReimport = true;
                Debug.Log($"[ApplyModel] Set loop={shouldLoop} on clips in {fbxPath}");
            }
        }

        // ── Fix: ensure resampleCurves is ON (previous import may have broken it) ──
        if (!importer.resampleCurves)
        {
            importer.resampleCurves = true;
            needsReimport = true;
        }

        // ── Minimal compression to preserve quality ──
        if (importer.animationCompression != ModelImporterAnimationCompression.KeyframeReductionAndCompression)
        {
            importer.animationCompression = ModelImporterAnimationCompression.KeyframeReductionAndCompression;
            importer.animationRotationError = 0.1f;
            importer.animationPositionError = 0.1f;
            importer.animationScaleError = 0.1f;
            needsReimport = true;
        }

        if (needsReimport)
        {
            importer.SaveAndReimport();
            Debug.Log($"[ApplyModel] Reimported: {fbxPath}");
        }
    }

    /// <summary>Find the main walking FBX (avatar source for all animations)</summary>
    static string FindWalkingFbx(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return null;
        var guids = AssetDatabase.FindAssets("t:Model", new[] { folder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.ToLower().Contains("walking") && path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                return path;
        }
        return null;
    }

    /// <summary>Set an animation FBX to copy its avatar from a source FBX</summary>
    static void CopyAvatarSource(string fbxPath, string sourceFbxPath)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        // For Generic animation: set the avatar source to define the same skeleton mapping
        if (importer.sourceAvatar == null)
        {
            // Load avatar from source FBX
            var sourceAssets = AssetDatabase.LoadAllAssetsAtPath(sourceFbxPath);
            Avatar sourceAvatar = null;
            foreach (var asset in sourceAssets)
            {
                if (asset is Avatar av)
                {
                    sourceAvatar = av;
                    break;
                }
            }
            if (sourceAvatar != null)
            {
                importer.sourceAvatar = sourceAvatar;
                importer.SaveAndReimport();
                Debug.Log($"[ApplyModel] Set avatar source on {fbxPath} from {sourceFbxPath}");
            }
        }
    }

    static Bounds CalculateBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    static void SetupMaterials(GameObject modelInstance, string modelFolder)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null) return;

        // Find textures ONLY in the same folder as the FBX
        Texture2D albedo = null, normal = null, metallic = null;

        var texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { modelFolder });
        foreach (var guid in texGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string lower = Path.GetFileName(path).ToLower();

            if (lower.Contains("normal"))
                normal = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            else if (lower.Contains("metallic"))
                metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            else if (lower.Contains("roughness"))
                { /* skip roughness for now */ }
            else
                albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(path); // albedo = the plain one
        }

        Debug.Log($"[ApplyModel] Textures found in {modelFolder}: albedo={albedo?.name}, normal={normal?.name}, metallic={metallic?.name}");

        // Create one clean URP material with all textures
        var mat = new Material(shader);
        mat.name = "M_Character";

        if (albedo != null)
        {
            mat.SetTexture("_BaseMap", albedo);
            mat.SetColor("_BaseColor", Color.white);
        }
        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }
        if (metallic != null)
        {
            mat.SetTexture("_MetallicGlossMap", metallic);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        // Save material as asset
        string matFolder = "Assets/_Player/Materials";
        EnsureFolder(matFolder);
        string matPath = matFolder + "/M_Character.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null) AssetDatabase.DeleteAsset(matPath);
        AssetDatabase.CreateAsset(mat, matPath);
        mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        // Apply to ALL renderers on the model
        var renderers = modelInstance.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            var mats = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = mat;
            renderer.sharedMaterials = mats;
        }

        Debug.Log($"[ApplyModel] Applied material to {renderers.Length} renderers");
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
