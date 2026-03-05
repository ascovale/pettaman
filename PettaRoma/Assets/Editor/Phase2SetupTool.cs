/*  ================================================================
 *  Phase2SetupTool.cs  —  Editor-only
 *  Menu: Petta ▸ Setup Phase 2 (Interactions)
 *
 *  One-click setup that adds to the existing scene:
 *      • AudioManager singleton
 *      • Shop Door triggers (inside/outside teleport)
 *      • NPC "Romano" with dialogue near the piazza
 *      • Enemy "Angry Pizza" patrols near the park
 *      • HUD Phase 2 elements (health bar, interact prompt,
 *        dialogue panel, notification text)
 *  ================================================================ */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public static class Phase2SetupTool
{
    [MenuItem("Petta/Setup Phase 2 (Interactions)")]
    public static void Setup()
    {
        // ── 1. AudioManager ──
        SetupAudioManager();

        // ── 2. Shop Door ──
        SetupShopDoor();

        // ── 3. NPC ──
        SetupNPC();

        // ── 4. Enemy ──
        SetupEnemy();

        // ── 5. HUD Phase 2 elements ──
        SetupHUDPhase2();

        // ── Save ──
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("<color=#B11E23><b>🍕 Phase 2 setup complete!</b></color>\n" +
            "New features:\n" +
            "  • E/F near shop door → teleport inside/outside\n" +
            "  • E/F near NPC → dialogue\n" +
            "  • Angry Pizza patrols the park area\n" +
            "  • Health bar appears when hit\n" +
            "  • Sound effects on coin/checkpoint/damage");
    }

    // ═══════════════════════════════════════════════════════
    //  AUDIO MANAGER
    // ═══════════════════════════════════════════════════════
    static void SetupAudioManager()
    {
        if (Object.FindFirstObjectByType<AudioManager>() != null)
        {
            Debug.Log("[Phase2] AudioManager already exists, skipping.");
            return;
        }

        var go = new GameObject("AudioManager");
        go.AddComponent<AudioManager>();
        Undo.RegisterCreatedObjectUndo(go, "Create AudioManager");
    }

    // ═══════════════════════════════════════════════════════
    //  SHOP DOOR
    // ═══════════════════════════════════════════════════════
    static void SetupShopDoor()
    {
        if (Object.FindFirstObjectByType<ShopDoor>() != null)
        {
            Debug.Log("[Phase2] ShopDoor already exists, skipping.");
            return;
        }

        // Find the Petta Shop in the scene (created by RomeBlockoutBuilder)
        var pettaShop = GameObject.Find("PettaShop");
        Vector3 shopPos = pettaShop != null
            ? pettaShop.transform.position
            : new Vector3(28f, 0f, 2f); // default fallback

        // Door trigger near shop entrance
        var doorGo = new GameObject("ShopDoor");
        Undo.RegisterCreatedObjectUndo(doorGo, "Create ShopDoor");
        doorGo.transform.position = shopPos + new Vector3(0f, 1f, 4f);

        // Add trigger collider
        var col = doorGo.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(3f, 3f, 1.5f);

        // Visual indicator — semi-transparent plane
        var doorVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        doorVisual.name = "DoorVisual";
        doorVisual.transform.SetParent(doorGo.transform, false);
        doorVisual.transform.localPosition = Vector3.zero;
        doorVisual.transform.localScale = new Vector3(2f, 2.5f, 1f);
        Object.DestroyImmediate(doorVisual.GetComponent<MeshCollider>());
        var doorMat = MakeMat(new Color(0.69f, 0.12f, 0.14f, 0.5f), "_DoorMat");
        SetTransparent(doorMat);
        doorVisual.GetComponent<Renderer>().sharedMaterial = doorMat;

        // ShopDoor component
        var shopDoor = doorGo.AddComponent<ShopDoor>();

        // Inside teleport point (inside the shop)
        var insidePoint = new GameObject("InsidePoint");
        insidePoint.transform.SetParent(doorGo.transform, false);
        insidePoint.transform.position = shopPos + new Vector3(0f, 1f, -2f);

        // Outside teleport point (in front of the shop)
        var outsidePoint = new GameObject("OutsidePoint");
        outsidePoint.transform.SetParent(doorGo.transform, false);
        outsidePoint.transform.position = shopPos + new Vector3(0f, 1f, 7f);

        // Wire via SerializedObject
        var so = new SerializedObject(shopDoor);
        so.FindProperty("insidePoint").objectReferenceValue = insidePoint.transform;
        so.FindProperty("outsidePoint").objectReferenceValue = outsidePoint.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ═══════════════════════════════════════════════════════
    //  NPC
    // ═══════════════════════════════════════════════════════
    static void SetupNPC()
    {
        if (Object.FindFirstObjectByType<NPCDialogue>() != null)
        {
            Debug.Log("[Phase2] NPC already exists, skipping.");
            return;
        }

        var npcGo = new GameObject("NPC_Romano");
        Undo.RegisterCreatedObjectUndo(npcGo, "Create NPC");

        // Place near the Piazza (between shop and colonnade)
        npcGo.transform.position = new Vector3(20f, 1f, 10f);

        // Trigger collider for interaction range
        var col = npcGo.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 3f;

        // NPCDialogue component
        npcGo.AddComponent<NPCDialogue>();

        // ── Visual: simple humanoid from primitives ──
        var visualRoot = new GameObject("Visual");
        visualRoot.transform.SetParent(npcGo.transform, false);

        // Body — capsule
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(visualRoot.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
        body.GetComponent<Renderer>().sharedMaterial =
            MakeMat(new Color(0.2f, 0.4f, 0.7f), "_NPCBody"); // blue shirt

        // Head — sphere
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(visualRoot.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.95f, 0f);
        head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        Object.DestroyImmediate(head.GetComponent<SphereCollider>());
        head.GetComponent<Renderer>().sharedMaterial =
            MakeMat(new Color(0.85f, 0.7f, 0.55f), "_NPCHead"); // skin

        // Wire visualRoot
        var dialogue = npcGo.GetComponent<NPCDialogue>();
        var so = new SerializedObject(dialogue);
        so.FindProperty("visualRoot").objectReferenceValue = visualRoot.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ═══════════════════════════════════════════════════════
    //  ENEMY
    // ═══════════════════════════════════════════════════════
    static void SetupEnemy()
    {
        if (Object.FindFirstObjectByType<EnemyAI>() != null)
        {
            Debug.Log("[Phase2] Enemy already exists, skipping.");
            return;
        }

        var enemyGo = new GameObject("Enemy_AngryPizza");
        Undo.RegisterCreatedObjectUndo(enemyGo, "Create Enemy");

        // Place in park area
        var park = GameObject.Find("Park");
        Vector3 parkPos = park != null ? park.transform.position : new Vector3(-20f, 0f, 30f);
        enemyGo.transform.position = parkPos + new Vector3(5f, 1f, 0f);

        // CharacterController (required by EnemyAI)
        var cc = enemyGo.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.6f, 0f);
        cc.radius = 0.5f;
        cc.height = 1.2f;

        // ── Visual: pizza-like disc + angry face ──
        var visualRoot = new GameObject("Visual");
        visualRoot.transform.SetParent(enemyGo.transform, false);

        // Pizza body — flattened sphere
        var pizzaBody = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pizzaBody.name = "PizzaBody";
        pizzaBody.transform.SetParent(visualRoot.transform, false);
        pizzaBody.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        pizzaBody.transform.localScale = new Vector3(1.2f, 0.35f, 1.2f);
        Object.DestroyImmediate(pizzaBody.GetComponent<SphereCollider>());
        pizzaBody.GetComponent<Renderer>().sharedMaterial =
            MakeMat(new Color(0.9f, 0.7f, 0.2f), "_PizzaBody"); // golden dough

        // Topping — small red sphere (tomato sauce)
        var topping = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        topping.name = "TomatoTop";
        topping.transform.SetParent(visualRoot.transform, false);
        topping.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        topping.transform.localScale = new Vector3(0.9f, 0.15f, 0.9f);
        Object.DestroyImmediate(topping.GetComponent<SphereCollider>());
        topping.GetComponent<Renderer>().sharedMaterial =
            MakeMat(new Color(0.8f, 0.15f, 0.1f), "_PizzaTopping"); // red sauce

        // Eyes — two small white spheres
        for (int i = 0; i < 2; i++)
        {
            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = i == 0 ? "EyeL" : "EyeR";
            eye.transform.SetParent(visualRoot.transform, false);
            float xOff = i == 0 ? -0.2f : 0.2f;
            eye.transform.localPosition = new Vector3(xOff, 0.95f, 0.4f);
            eye.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            Object.DestroyImmediate(eye.GetComponent<SphereCollider>());
            eye.GetComponent<Renderer>().sharedMaterial =
                MakeMat(Color.white, $"_Eye{i}");

            // Pupil
            var pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pupil.name = "Pupil";
            pupil.transform.SetParent(eye.transform, false);
            pupil.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            pupil.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Object.DestroyImmediate(pupil.GetComponent<SphereCollider>());
            pupil.GetComponent<Renderer>().sharedMaterial =
                MakeMat(Color.black, "_Pupil");
        }

        // EnemyAI component
        var ai = enemyGo.AddComponent<EnemyAI>();

        // Create patrol points around park
        var patrolParent = new GameObject("PatrolPoints");
        patrolParent.transform.SetParent(enemyGo.transform.parent, true);
        patrolParent.transform.position = Vector3.zero;

        Transform[] patrolPoints = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            var wp = new GameObject($"Patrol_{i}");
            wp.transform.SetParent(patrolParent.transform, false);
            float angle = i * Mathf.PI * 0.5f;
            wp.transform.position = parkPos + new Vector3(
                Mathf.Cos(angle) * 8f, 0.5f, Mathf.Sin(angle) * 8f);
            patrolPoints[i] = wp.transform;
        }

        // Wire via SerializedObject
        var so = new SerializedObject(ai);
        so.FindProperty("visualRoot").objectReferenceValue = visualRoot.transform;
        var patrolProp = so.FindProperty("patrolPoints");
        patrolProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
            patrolProp.GetArrayElementAtIndex(i).objectReferenceValue = patrolPoints[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ═══════════════════════════════════════════════════════
    //  HUD PHASE 2 ELEMENTS
    // ═══════════════════════════════════════════════════════
    static void SetupHUDPhase2()
    {
        var hud = Object.FindFirstObjectByType<HUDManager>();
        if (hud == null)
        {
            Debug.LogWarning("[Phase2] No HUDManager found! Run 'Petta > Setup Player & Managers' first.");
            return;
        }

        var canvas = hud.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[Phase2] HUDManager has no Canvas.");
            return;
        }
        var canvasGo = canvas.gameObject;
        var soHud = new SerializedObject(hud);

        // ── Health Bar (top-right) ──
        if (soHud.FindProperty("healthBarRoot").objectReferenceValue == null)
        {
            var healthRoot = CreatePanel(canvasGo.transform, "HealthBar",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-20, -20), new Vector2(200, 30));

            // Background
            var bgImg = healthRoot.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Fill
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(healthRoot.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(4, 4);
            fillRect.offsetMax = new Vector2(-4, -4);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.8f, 0.2f, 0.15f); // red health
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;

            soHud.FindProperty("healthBarRoot").objectReferenceValue = healthRoot;
            soHud.FindProperty("healthFill").objectReferenceValue = fillImg;
        }

        // ── Interact Prompt (bottom-center) ──
        if (soHud.FindProperty("interactPromptRoot").objectReferenceValue == null)
        {
            var promptRoot = CreatePanel(canvasGo.transform, "InteractPrompt",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 100), new Vector2(300, 50));

            // Semi-transparent background
            var bgImg = promptRoot.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.6f);

            // Text
            var textGo = new GameObject("PromptText");
            textGo.transform.SetParent(promptRoot.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);
            var promptText = textGo.AddComponent<Text>();
            promptText.text = "Press E to interact";
            promptText.fontSize = 22;
            promptText.color = Color.white;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.font = GetFont();

            soHud.FindProperty("interactPromptRoot").objectReferenceValue = promptRoot;
            soHud.FindProperty("interactText").objectReferenceValue = promptText;
        }

        // ── Dialogue Panel (bottom third, center) ──
        if (soHud.FindProperty("dialoguePanelRoot").objectReferenceValue == null)
        {
            var dialogueRoot = CreatePanel(canvasGo.transform, "DialoguePanel",
                new Vector2(0.1f, 0), new Vector2(0.9f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 20), new Vector2(0, 120));

            // Background
            var bgImg = dialogueRoot.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);

            // Text
            var textGo = new GameObject("DialogueText");
            textGo.transform.SetParent(dialogueRoot.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 15);
            textRect.offsetMax = new Vector2(-20, -15);
            var dlgText = textGo.AddComponent<Text>();
            dlgText.text = "";
            dlgText.fontSize = 24;
            dlgText.color = Color.white;
            dlgText.alignment = TextAnchor.MiddleLeft;
            dlgText.font = GetFont();
            dlgText.supportRichText = true;

            soHud.FindProperty("dialoguePanelRoot").objectReferenceValue = dialogueRoot;
            soHud.FindProperty("dialogueText").objectReferenceValue = dlgText;
        }

        // ── Notification (top-center, brief messages) ──
        if (soHud.FindProperty("notificationRoot").objectReferenceValue == null)
        {
            var notifRoot = CreatePanel(canvasGo.transform, "Notification",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -80), new Vector2(350, 50));

            var bgImg = notifRoot.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.6f, 0.2f, 0.8f); // greenish

            var textGo = new GameObject("NotifText");
            textGo.transform.SetParent(notifRoot.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);
            var notifText = textGo.AddComponent<Text>();
            notifText.text = "";
            notifText.fontSize = 26;
            notifText.fontStyle = FontStyle.Bold;
            notifText.color = Color.white;
            notifText.alignment = TextAnchor.MiddleCenter;
            notifText.font = GetFont();

            soHud.FindProperty("notificationRoot").objectReferenceValue = notifRoot;
            soHud.FindProperty("notificationText").objectReferenceValue = notifText;
        }

        soHud.ApplyModifiedPropertiesWithoutUndo();
    }

    // ═══════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════

    static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return go;
    }

    static Material MakeMat(Color color, string suffix)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader);
        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        string folder = "Assets/_Core/Materials";
        EnsureFolder(folder);
        string path = $"{folder}/M{suffix}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            AssetDatabase.CreateAsset(mat, path);
        return AssetDatabase.LoadAssetAtPath<Material>(path) ?? mat;
    }

    static void SetTransparent(Material mat)
    {
        if (mat.HasProperty("_Surface"))
        {
            // URP Lit transparent mode
            mat.SetFloat("_Surface", 1); // 0=Opaque, 1=Transparent
            mat.SetFloat("_Blend", 0);   // Alpha
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        else
        {
            // Standard shader fallback
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
        }
    }

    static Font GetFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
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
