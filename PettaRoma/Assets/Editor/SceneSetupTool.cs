/*  ================================================================
 *  SceneSetupTool.cs  —  Editor-only
 *  Menu: Petta ▸ Setup Player & Managers
 *
 *  One-click setup that creates:
 *      • Player (CharacterController + PlayerController + Capsule visual)
 *      • Camera (CameraFollow on Main Camera, targeting Player)
 *      • GameManager, InputManager, LevelManager
 *      • HUD Canvas with coin counter
 *      • "Player" tag assignment
 *      • PlayerStats ScriptableObject asset (auto-wired)
 *  ================================================================ */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public static class SceneSetupTool
{
    [MenuItem("Petta/Setup Player & Managers")]
    public static void Setup()
    {
        // ── Ensure "Player" tag exists ──
        EnsurePlayerTag();

        // ── 1. Create or find PlayerStats asset ──
        PlayerStats stats = CreateOrFindPlayerStats();

        // ── 2. Create Player ──
        var player = SetupPlayer(stats);

        // ── 3. Setup Camera ──
        SetupCamera(player.transform);

        // ── 4. Create Managers ──
        SetupManagers();

        // ── 5. Create HUD Canvas ──
        SetupHUD();

        // ── 6. Save ──
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("<color=#B11E23><b>🍕 Player & Managers setup complete!</b></color>\n" +
            "Press Play to test:\n" +
            "  • WASD = move\n" +
            "  • Space = jump\n" +
            "  • Right-click + drag = orbit camera\n" +
            "  • Shift = run\n" +
            "  • Walk into gold coins to collect them");
    }

    // ═══════════════════════════════════════════════════════
    //  PLAYER
    // ═══════════════════════════════════════════════════════
    static GameObject SetupPlayer(PlayerStats stats)
    {
        // Check if Player already exists
        var existing = GameObject.FindGameObjectWithTag("Player");
        if (existing == null) existing = GameObject.Find("Player");
        if (existing != null)
        {
            Debug.Log("[Setup] Player already exists, updating it.");
            Undo.DestroyObjectImmediate(existing);
        }

        // Create Player root
        var player = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(player, "Create Player");

        player.transform.position = new Vector3(35f, 0f, -2f);
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Default");

        // CharacterController
        var cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.radius = 0.35f;
        cc.height = 1.8f;
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.4f;

        // PlayerController
        var pc = player.AddComponent<PlayerController>();
        // Wire PlayerStats via SerializedObject
        var so = new SerializedObject(pc);
        so.FindProperty("stats").objectReferenceValue = stats;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ── Visual: Capsule body ──
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(player.transform);
        body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
        // Remove capsule's own collider (CharacterController handles collision)
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

        // Give body a red Petta color
        var bodyMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ??
                                    Shader.Find("Standard"));
        bodyMat.color = new Color(0.69f, 0.12f, 0.14f); // Petta red
        if (bodyMat.HasProperty("_BaseColor"))
            bodyMat.SetColor("_BaseColor", new Color(0.69f, 0.12f, 0.14f));
        body.GetComponent<Renderer>().sharedMaterial = bodyMat;

        // Save material as asset
        string matFolder = "Assets/_Player/Materials";
        EnsureFolder(matFolder);
        string matPath = matFolder + "/M_PlayerBody.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(matPath) == null)
            AssetDatabase.CreateAsset(bodyMat, matPath);
        else
            body.GetComponent<Renderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(matPath);

        // ── Visual: Head sphere ──
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(player.transform);
        head.transform.localPosition = new Vector3(0f, 1.95f, 0f);
        head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        Object.DestroyImmediate(head.GetComponent<SphereCollider>());

        // Head gets a skin-tone material
        var headMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ??
                                    Shader.Find("Standard"));
        headMat.color = new Color(0.92f, 0.78f, 0.65f); // skin tone
        if (headMat.HasProperty("_BaseColor"))
            headMat.SetColor("_BaseColor", new Color(0.92f, 0.78f, 0.65f));
        head.GetComponent<Renderer>().sharedMaterial = headMat;

        string headMatPath = matFolder + "/M_PlayerHead.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(headMatPath) == null)
            AssetDatabase.CreateAsset(headMat, headMatPath);
        else
            head.GetComponent<Renderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(headMatPath);

        // ── Chef hat (small white cylinder on top) ──
        var hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hat.name = "ChefHat";
        hat.transform.SetParent(player.transform);
        hat.transform.localPosition = new Vector3(0f, 2.35f, 0f);
        hat.transform.localScale = new Vector3(0.4f, 0.2f, 0.4f);
        Object.DestroyImmediate(hat.GetComponent<CapsuleCollider>());

        var hatMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ??
                                   Shader.Find("Standard"));
        hatMat.color = Color.white;
        if (hatMat.HasProperty("_BaseColor"))
            hatMat.SetColor("_BaseColor", Color.white);
        hat.GetComponent<Renderer>().sharedMaterial = hatMat;

        string hatMatPath = matFolder + "/M_ChefHat.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(hatMatPath) == null)
            AssetDatabase.CreateAsset(hatMat, hatMatPath);
        else
            hat.GetComponent<Renderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(hatMatPath);

        // Wire modelRoot to Body so PlayerController rotates Body
        var so2 = new SerializedObject(pc);
        so2.FindProperty("modelRoot").objectReferenceValue = player.transform;
        so2.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    // ═══════════════════════════════════════════════════════
    //  CAMERA
    // ═══════════════════════════════════════════════════════
    static void SetupCamera(Transform playerTransform)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
        }

        // Add or get CameraFollow
        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null)
            follow = cam.gameObject.AddComponent<CameraFollow>();

        // Wire target via SerializedObject
        var so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = playerTransform;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Position camera behind player initially
        cam.transform.position = playerTransform.position + new Vector3(0, 5, -7);
        cam.transform.LookAt(playerTransform.position + Vector3.up * 1.5f);
    }

    // ═══════════════════════════════════════════════════════
    //  MANAGERS
    // ═══════════════════════════════════════════════════════
    static void SetupManagers()
    {
        CreateSingletonGO<GameManager>("GameManager");
        CreateSingletonGO<InputManager>("InputManager");
        CreateSingletonGO<LevelManager>("LevelManager");
    }

    static void CreateSingletonGO<T>(string name) where T : Component
    {
        if (Object.FindFirstObjectByType<T>() != null)
        {
            Debug.Log($"[Setup] {name} already exists, skipping.");
            return;
        }
        var go = new GameObject(name);
        go.AddComponent<T>();
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
    }

    // ═══════════════════════════════════════════════════════
    //  HUD CANVAS
    // ═══════════════════════════════════════════════════════
    static void SetupHUD()
    {
        // Skip if already exists
        if (Object.FindFirstObjectByType<HUDManager>() != null)
        {
            Debug.Log("[Setup] HUD already exists, skipping.");
            return;
        }

        // Canvas
        var canvasGo = new GameObject("HUD_Canvas");
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // Configure scaler for mobile
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // HUDManager component
        var hud = canvasGo.AddComponent<HUDManager>();

        // ── Coin icon + text (top-left) ──
        var coinPanel = new GameObject("CoinPanel");
        coinPanel.transform.SetParent(canvasGo.transform, false);
        var coinRect = coinPanel.AddComponent<RectTransform>();
        coinRect.anchorMin = new Vector2(0, 1);
        coinRect.anchorMax = new Vector2(0, 1);
        coinRect.pivot = new Vector2(0, 1);
        coinRect.anchoredPosition = new Vector2(20, -20);
        coinRect.sizeDelta = new Vector2(200, 50);

        // Coin emoji text (gold circle as placeholder)
        var coinIcon = new GameObject("CoinIcon");
        coinIcon.transform.SetParent(coinPanel.transform, false);
        var iconRect = coinIcon.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(0, 0);
        iconRect.sizeDelta = new Vector2(40, 40);
        var iconImg = coinIcon.AddComponent<Image>();
        iconImg.color = new Color(1f, 0.84f, 0f); // gold

        // Coin count text
        var coinTextGo = new GameObject("CoinText");
        coinTextGo.transform.SetParent(coinPanel.transform, false);
        var textRect = coinTextGo.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0.5f);
        textRect.anchorMax = new Vector2(0, 0.5f);
        textRect.pivot = new Vector2(0, 0.5f);
        textRect.anchoredPosition = new Vector2(50, 0);
        textRect.sizeDelta = new Vector2(150, 40);

        var coinText = coinTextGo.AddComponent<Text>();
        coinText.text = "× 0";
        coinText.fontSize = 32;
        coinText.fontStyle = FontStyle.Bold;
        coinText.color = Color.white;
        coinText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (coinText.font == null)
            coinText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        coinText.alignment = TextAnchor.MiddleLeft;

        // Add shadow for readability
        var shadow = coinTextGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.7f);
        shadow.effectDistance = new Vector2(2, -2);

        // Wire coinText to HUDManager
        var soHud = new SerializedObject(hud);
        soHud.FindProperty("coinText").objectReferenceValue = coinText;
        soHud.ApplyModifiedPropertiesWithoutUndo();
    }

    // ═══════════════════════════════════════════════════════
    //  PLAYER STATS ASSET
    // ═══════════════════════════════════════════════════════
    static PlayerStats CreateOrFindPlayerStats()
    {
        string path = "Assets/_Core/Data/PlayerStats_Default.asset";
        var existing = AssetDatabase.LoadAssetAtPath<PlayerStats>(path);
        if (existing != null) return existing;

        EnsureFolder("Assets/_Core/Data");
        var stats = ScriptableObject.CreateInstance<PlayerStats>();
        AssetDatabase.CreateAsset(stats, path);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<PlayerStats>(path);
    }

    // ═══════════════════════════════════════════════════════
    //  TAG HELPER
    // ═══════════════════════════════════════════════════════
    static void EnsurePlayerTag()
    {
        // "Player" tag exists by default in Unity, but just verify
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
        SerializedProperty tags = tagManager.FindProperty("tags");

        bool found = false;
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == "Player")
            {
                found = true;
                break;
            }
        }

        // "Player" is a built-in tag, so it should always be there.
        // If somehow missing from custom tags, Unity still has it built-in.
        if (!found)
        {
            Debug.Log("[Setup] 'Player' tag is built-in, no action needed.");
        }
    }

    // ═══════════════════════════════════════════════════════
    //  FOLDER HELPER
    // ═══════════════════════════════════════════════════════
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
