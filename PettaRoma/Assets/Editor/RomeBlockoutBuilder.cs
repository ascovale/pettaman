/*  ================================================================
 *  RomeBlockoutBuilder.cs  —  Editor-only
 *  Menu: Petta ▸ Build Rome Blockout
 *
 *  Generates a low-poly blockout of the Prati / Vatican district:
 *      • Ground plane (200×200)
 *      • Vatican walls + St Peter's dome
 *      • Castel Sant'Angelo
 *      • Via della Conciliazione
 *      • Via dei Gracchi with generic buildings
 *      • Petta shop with "PETTA" sign
 *      • Via Ottaviano + Metro entrance
 *      • Checkpoints, coins, player spawn
 *
 *  Everything is built from Unity primitives. Materials auto-detect
 *  URP vs Built-in pipeline.
 *  ================================================================ */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class RomeBlockoutBuilder
{
    // ─── Colours (warm Roman palette) ─────────────────────
    static readonly Color COL_GROUND     = new Color(0.76f, 0.70f, 0.60f); // stone beige
    static readonly Color COL_ROAD       = new Color(0.38f, 0.38f, 0.36f); // asphalt grey
    static readonly Color COL_BLDG       = new Color(0.85f, 0.72f, 0.55f); // terracotta
    static readonly Color COL_BLDG_ALT   = new Color(0.78f, 0.75f, 0.65f); // lighter stone
    static readonly Color COL_VATICAN    = new Color(0.92f, 0.90f, 0.82f); // marble white
    static readonly Color COL_DOME       = new Color(0.45f, 0.58f, 0.45f); // patinated copper
    static readonly Color COL_CASTEL     = new Color(0.72f, 0.65f, 0.55f); // old brick
    static readonly Color COL_PETTA      = new Color(0.69f, 0.12f, 0.14f); // #B11E23 Petta red
    static readonly Color COL_METRO      = new Color(0.20f, 0.25f, 0.55f); // metro blue
    static readonly Color COL_CHECKPOINT = new Color(1f, 0.85f, 0f, 0.6f); // gold translucent
    static readonly Color COL_COIN       = new Color(1f, 0.84f, 0f);       // gold
    static readonly Color COL_COLONNADE  = new Color(0.88f, 0.85f, 0.78f); // warm stone
    static readonly Color COL_GRASS      = new Color(0.40f, 0.56f, 0.30f); // olive green

    static readonly string MAT_FOLDER = "Assets/_Levels/Materials";

    // ──────────────────────────────────────────────────────
    [MenuItem("Petta/Build Rome Blockout")]
    public static void Build()
    {
        // Remove previous blockout if present
        var old = GameObject.Find("RomeBlockout");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("RomeBlockout");
        Undo.RegisterCreatedObjectUndo(root, "Build Rome Blockout");

        BuildGround(root.transform);
        BuildRoads(root.transform);
        BuildVatican(root.transform);
        BuildCastel(root.transform);
        BuildViaGracchiBuildings(root.transform);
        BuildPettaShop(root.transform);
        BuildMetro(root.transform);
        BuildPark(root.transform);
        BuildCheckpoints(root.transform);
        BuildCoins(root.transform);
        BuildPlayerSpawn(root.transform);
        BuildLighting();

        Selection.activeGameObject = root;
        Debug.Log("<color=#B11E23><b>🍕 Rome Blockout built!</b></color> Select RomeBlockout in Hierarchy.");
    }

    // ═══════════════════════════════════════════════════════
    //  GROUND
    // ═══════════════════════════════════════════════════════
    static void BuildGround(Transform parent)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = "Ground";
        g.transform.SetParent(parent);
        g.transform.localPosition = new Vector3(0, -0.5f, 0);
        g.transform.localScale = new Vector3(200, 1, 200);
        g.isStatic = true;
        SetMat(g, "M_Ground", COL_GROUND);
    }

    // ═══════════════════════════════════════════════════════
    //  ROADS
    // ═══════════════════════════════════════════════════════
    static void BuildRoads(Transform parent)
    {
        var roadParent = MakeEmpty("Roads", parent);
        Material mat = GetOrCreateMat("M_Road", COL_ROAD);

        // Via della Conciliazione — wide boulevard Vatican → Castel
        MakeRoad(roadParent.transform, "Via della Conciliazione",
            new Vector3(-15, 0.02f, 32), new Vector3(60, 0.04f, 12), mat);

        // Via dei Gracchi — N-S
        MakeRoad(roadParent.transform, "Via dei Gracchi",
            new Vector3(35, 0.02f, 10), new Vector3(6, 0.04f, 70), mat);

        // Via Ottaviano — E-W
        MakeRoad(roadParent.transform, "Via Ottaviano",
            new Vector3(30, 0.02f, -10), new Vector3(80, 0.04f, 6), mat);

        // Small connecting road (Vicolo)
        MakeRoad(roadParent.transform, "Vicolo",
            new Vector3(15, 0.02f, 15), new Vector3(6, 0.04f, 30), mat);
    }

    static void MakeRoad(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
        r.name = name;
        r.transform.SetParent(parent);
        r.transform.localPosition = pos;
        r.transform.localScale = scale;
        r.isStatic = true;
        r.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // ═══════════════════════════════════════════════════════
    //  VATICAN
    // ═══════════════════════════════════════════════════════
    static void BuildVatican(Transform parent)
    {
        var vat = MakeEmpty("Vatican", parent);
        Material mWall = GetOrCreateMat("M_Vatican", COL_VATICAN);
        Material mDome = GetOrCreateMat("M_Dome", COL_DOME);
        Material mCol  = GetOrCreateMat("M_Colonnade", COL_COLONNADE);

        // Perimeter walls (simplified rectangle)
        float cx = -40f, cz = 40f;
        float hw = 30f, hd = 25f, wallH = 8f, wallT = 1.5f;

        // North wall
        MakeBox(vat.transform, "Wall_N", new Vector3(cx, wallH / 2, cz + hd), new Vector3(hw * 2, wallH, wallT), mWall);
        // South wall
        MakeBox(vat.transform, "Wall_S", new Vector3(cx, wallH / 2, cz - hd), new Vector3(hw * 2, wallH, wallT), mWall);
        // West wall
        MakeBox(vat.transform, "Wall_W", new Vector3(cx - hw, wallH / 2, cz), new Vector3(wallT, wallH, hd * 2), mWall);
        // East wall (with gap for entrance)
        MakeBox(vat.transform, "Wall_E_Top", new Vector3(cx + hw, wallH / 2, cz + 15), new Vector3(wallT, wallH, 20), mWall);
        MakeBox(vat.transform, "Wall_E_Bot", new Vector3(cx + hw, wallH / 2, cz - 15), new Vector3(wallT, wallH, 20), mWall);

        // St Peter's Basilica body (large box)
        MakeBox(vat.transform, "Basilica_Nave", new Vector3(cx, 5f, cz + 5), new Vector3(20, 10, 30), mWall);

        // Drum (cylinder under dome)
        var drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        drum.name = "Basilica_Drum";
        drum.transform.SetParent(vat.transform);
        drum.transform.localPosition = new Vector3(cx, 12f, cz + 5);
        drum.transform.localScale = new Vector3(14, 3, 14);
        drum.isStatic = true;
        SetMatDirect(drum, mWall);

        // Dome (sphere)
        var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dome.name = "Basilica_Dome";
        dome.transform.SetParent(vat.transform);
        dome.transform.localPosition = new Vector3(cx, 18f, cz + 5);
        dome.transform.localScale = new Vector3(13, 10, 13);
        dome.isStatic = true;
        SetMatDirect(dome, mDome);

        // Colonnade: simplified as two curved walls (series of pillars)
        float colRadius = 22f;
        int pillars = 12;
        for (int i = 0; i < pillars; i++)
        {
            float angle = Mathf.Lerp(-70f, 70f, i / (float)(pillars - 1));
            float rad = angle * Mathf.Deg2Rad;
            float px = cx + Mathf.Sin(rad) * colRadius;
            float pz = (cz - hd - 3) + Mathf.Cos(rad) * 6f; // slight outward curve

            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = $"Colonnade_{i}";
            pillar.transform.SetParent(vat.transform);
            pillar.transform.localPosition = new Vector3(px, 3.5f, pz);
            pillar.transform.localScale = new Vector3(1.2f, 3.5f, 1.2f);
            pillar.isStatic = true;
            SetMatDirect(pillar, mCol);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  CASTEL SANT'ANGELO
    // ═══════════════════════════════════════════════════════
    static void BuildCastel(Transform parent)
    {
        var ca = MakeEmpty("CastelSantAngelo", parent);
        Material mat = GetOrCreateMat("M_Castel", COL_CASTEL);
        Material mWall = GetOrCreateMat("M_Vatican", COL_VATICAN);

        float cx = 10f, cz = 38f;

        // Main drum
        var drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        drum.name = "MainDrum";
        drum.transform.SetParent(ca.transform);
        drum.transform.localPosition = new Vector3(cx, 6f, cz);
        drum.transform.localScale = new Vector3(18, 6, 18);
        drum.isStatic = true;
        SetMatDirect(drum, mat);

        // Upper tier
        var upper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        upper.name = "UpperTier";
        upper.transform.SetParent(ca.transform);
        upper.transform.localPosition = new Vector3(cx, 14f, cz);
        upper.transform.localScale = new Vector3(10, 3, 10);
        upper.isStatic = true;
        SetMatDirect(upper, mWall);

        // Angel statue (thin tall box)
        MakeBox(ca.transform, "AngelStatue", new Vector3(cx, 19f, cz), new Vector3(1, 3, 0.5f), mWall);

        // Bridge (Ponte Sant'Angelo — a long flat box)
        MakeBox(ca.transform, "Bridge", new Vector3(cx, 1.5f, cz - 18), new Vector3(5, 1, 20), mat);
    }

    // ═══════════════════════════════════════════════════════
    //  VIA DEI GRACCHI — BUILDINGS
    // ═══════════════════════════════════════════════════════
    static void BuildViaGracchiBuildings(Transform parent)
    {
        var bg = MakeEmpty("Buildings_ViaGracchi", parent);
        Material m1 = GetOrCreateMat("M_Building", COL_BLDG);
        Material m2 = GetOrCreateMat("M_BuildingAlt", COL_BLDG_ALT);

        // East side of Via dei Gracchi (x ≈ 42-50)
        MakeBox(bg.transform, "Bldg_E1", new Vector3(46, 7, 35), new Vector3(10, 14, 8), m1);
        MakeBox(bg.transform, "Bldg_E2", new Vector3(48, 5, 22), new Vector3(8, 10, 6), m2);
        MakeBox(bg.transform, "Bldg_E3", new Vector3(44, 6, 10), new Vector3(7, 12, 7), m1);
        MakeBox(bg.transform, "Bldg_E4", new Vector3(47, 4, -2), new Vector3(9, 8, 6), m2);
        MakeBox(bg.transform, "Bldg_E5", new Vector3(45, 5.5f, -18), new Vector3(8, 11, 7), m1);

        // West side of Via dei Gracchi (x ≈ 22-30)
        MakeBox(bg.transform, "Bldg_W1", new Vector3(26, 6, 38), new Vector3(9, 12, 7), m2);
        MakeBox(bg.transform, "Bldg_W2", new Vector3(24, 5, 25), new Vector3(8, 10, 8), m1);
        MakeBox(bg.transform, "Bldg_W3", new Vector3(27, 7, 0), new Vector3(10, 14, 6), m2);
        MakeBox(bg.transform, "Bldg_W4", new Vector3(25, 4.5f, -15), new Vector3(7, 9, 8), m1);

        // Buildings along Via Ottaviano
        MakeBox(bg.transform, "Bldg_O1", new Vector3(5, 6, -16), new Vector3(8, 12, 6), m2);
        MakeBox(bg.transform, "Bldg_O2", new Vector3(55, 5, -16), new Vector3(7, 10, 7), m1);
        MakeBox(bg.transform, "Bldg_O3", new Vector3(65, 4, -4), new Vector3(6, 8, 8), m2);

        // Fill buildings near Vatican (south side)
        MakeBox(bg.transform, "Bldg_V1", new Vector3(-20, 5.5f, 10), new Vector3(10, 11, 8), m1);
        MakeBox(bg.transform, "Bldg_V2", new Vector3(-5, 6, 8), new Vector3(8, 12, 7), m2);
    }

    // ═══════════════════════════════════════════════════════
    //  PETTA SHOP  (the player's home base)
    // ═══════════════════════════════════════════════════════
    static void BuildPettaShop(Transform parent)
    {
        var shop = MakeEmpty("PettaShop", parent);
        Material mPetta = GetOrCreateMat("M_Petta", COL_PETTA);
        Material mBldg  = GetOrCreateMat("M_Building", COL_BLDG);

        float sx = 35f, sz = 5f;

        // Building body
        MakeBox(shop.transform, "ShopBody", new Vector3(sx, 3.5f, sz), new Vector3(8, 7, 6), mBldg);

        // Awning (flat red box above entrance)
        MakeBox(shop.transform, "Awning", new Vector3(sx, 6.5f, sz - 3.5f), new Vector3(9, 0.3f, 2), mPetta);

        // PETTA sign (3D text)
        var signObj = new GameObject("PETTA_Sign");
        signObj.transform.SetParent(shop.transform);
        signObj.transform.localPosition = new Vector3(sx - 2.5f, 7.5f, sz - 3.1f);
        signObj.transform.localScale = Vector3.one * 0.8f;

        var tm = signObj.AddComponent<TextMesh>();
        tm.text = "PETTA";
        tm.fontSize = 48;
        tm.characterSize = 0.25f;
        tm.anchor = TextAnchor.MiddleLeft;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        tm.fontStyle = FontStyle.Bold;

        // Door placeholder (dark box)
        var doorMat = GetOrCreateMat("M_Door", new Color(0.25f, 0.15f, 0.1f));
        MakeBox(shop.transform, "Door", new Vector3(sx, 1.5f, sz - 3.05f), new Vector3(1.8f, 3, 0.15f), doorMat);

        // Pizza emoji decoration — small sphere on awning
        var pizzaDeco = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pizzaDeco.name = "PizzaDeco";
        pizzaDeco.transform.SetParent(shop.transform);
        pizzaDeco.transform.localPosition = new Vector3(sx + 3, 7f, sz - 4f);
        pizzaDeco.transform.localScale = new Vector3(1.2f, 0.3f, 1.2f);
        pizzaDeco.isStatic = true;
        SetMat(pizzaDeco, "M_Coin", COL_COIN); // gold / cheese color
    }

    // ═══════════════════════════════════════════════════════
    //  METRO OTTAVIANO
    // ═══════════════════════════════════════════════════════
    static void BuildMetro(Transform parent)
    {
        var metro = MakeEmpty("MetroOttaviano", parent);
        Material mMetro = GetOrCreateMat("M_Metro", COL_METRO);
        Material mBldg  = GetOrCreateMat("M_BuildingAlt", COL_BLDG_ALT);

        float mx = 50f, mz = -10f;

        // Entrance structure
        MakeBox(metro.transform, "Entrance", new Vector3(mx, 2f, mz), new Vector3(6, 4, 4), mBldg);

        // Metro sign (M)
        var signObj = new GameObject("Metro_M_Sign");
        signObj.transform.SetParent(metro.transform);
        signObj.transform.localPosition = new Vector3(mx - 1.5f, 4.5f, mz - 2.1f);
        signObj.transform.localScale = Vector3.one;

        var tm = signObj.AddComponent<TextMesh>();
        tm.text = "M";
        tm.fontSize = 64;
        tm.characterSize = 0.2f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.white;
        tm.fontStyle = FontStyle.Bold;

        // Red circle behind M
        var circle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        circle.name = "Metro_Circle";
        circle.transform.SetParent(metro.transform);
        circle.transform.localPosition = new Vector3(mx, 4.5f, mz - 2.05f);
        circle.transform.localScale = new Vector3(2.5f, 0.1f, 2.5f);
        circle.transform.localRotation = Quaternion.Euler(90, 0, 0);
        circle.isStatic = true;
        SetMatDirect(circle, mMetro);

        // Steps going down (series of smaller boxes)
        for (int i = 0; i < 5; i++)
        {
            MakeBox(metro.transform, $"Step_{i}",
                new Vector3(mx, -0.3f * i, mz + 1 + i * 0.8f),
                new Vector3(4, 0.3f, 0.8f), mBldg);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  SMALL PARK / GREEN AREA
    // ═══════════════════════════════════════════════════════
    static void BuildPark(Transform parent)
    {
        var park = MakeEmpty("Park", parent);
        Material mGrass = GetOrCreateMat("M_Grass", COL_GRASS);
        Material mBldg  = GetOrCreateMat("M_BuildingAlt", COL_BLDG_ALT);

        // Grass patch near Castel
        MakeBox(park.transform, "Grass_1", new Vector3(-5, 0.03f, 15), new Vector3(16, 0.06f, 12), mGrass);

        // Simple tree trunk + canopy (a few)
        MakeTree(park.transform, new Vector3(-8, 0, 18), mBldg, mGrass);
        MakeTree(park.transform, new Vector3(-2, 0, 12), mBldg, mGrass);
        MakeTree(park.transform, new Vector3(3, 0, 17), mBldg, mGrass);

        // Bench (flat box)
        MakeBox(park.transform, "Bench", new Vector3(-4, 0.5f, 14), new Vector3(2.5f, 0.4f, 0.8f), mBldg);
    }

    static void MakeTree(Transform parent, Vector3 pos, Material trunk, Material canopy)
    {
        var tree = MakeEmpty("Tree", parent);
        tree.transform.localPosition = pos;

        var t = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        t.name = "Trunk";
        t.transform.SetParent(tree.transform);
        t.transform.localPosition = new Vector3(0, 1.5f, 0);
        t.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
        t.isStatic = true;
        SetMatDirect(t, trunk);

        var c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        c.name = "Canopy";
        c.transform.SetParent(tree.transform);
        c.transform.localPosition = new Vector3(0, 4f, 0);
        c.transform.localScale = new Vector3(3.5f, 3, 3.5f);
        c.isStatic = true;
        SetMatDirect(c, canopy);
    }

    // ═══════════════════════════════════════════════════════
    //  CHECKPOINTS
    // ═══════════════════════════════════════════════════════
    static void BuildCheckpoints(Transform parent)
    {
        var cp = MakeEmpty("Checkpoints", parent);

        MakeCheckpoint(cp.transform, 0, new Vector3(30, 0.5f, 0));     // Start — near Petta shop
        MakeCheckpoint(cp.transform, 1, new Vector3(10, 0.5f, 20));    // Near bridge / Castel
        MakeCheckpoint(cp.transform, 2, new Vector3(-40, 0.5f, 15));   // Vatican entrance
    }

    static void MakeCheckpoint(Transform parent, int id, Vector3 pos)
    {
        var go = new GameObject($"Checkpoint_{id}");
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.tag = "Untagged"; // no special tag needed

        // Trigger collider
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(3, 4, 3);

        // Checkpoint script
        var chk = go.AddComponent<Checkpoint>();
        // Set checkpoint id via serialized field
        var so = new SerializedObject(chk);
        so.FindProperty("checkpointId").intValue = id;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Visual flag (small cylinder)
        var flag = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        flag.name = "Flag";
        flag.transform.SetParent(go.transform);
        flag.transform.localPosition = new Vector3(0, 1.5f, 0);
        flag.transform.localScale = new Vector3(0.3f, 1.5f, 0.3f);
        flag.isStatic = false;
        SetMat(flag, "M_Checkpoint", COL_CHECKPOINT);

        // Remove collider on visual (avoid double collision)
        Object.DestroyImmediate(flag.GetComponent<Collider>());

        // Link visual for pulse animation
        var soVis = new SerializedObject(chk);
        soVis.FindProperty("visual").objectReferenceValue = flag.transform;
        soVis.ApplyModifiedPropertiesWithoutUndo();
    }

    // ═══════════════════════════════════════════════════════
    //  COINS
    // ═══════════════════════════════════════════════════════
    static void BuildCoins(Transform parent)
    {
        var coins = MakeEmpty("Coins", parent);
        Material mat = GetOrCreateMat("M_Coin", COL_COIN);

        Vector3[] positions = new Vector3[]
        {
            new Vector3(32, 1.2f, 5),   // near Petta
            new Vector3(30, 1.2f, 10),
            new Vector3(28, 1.2f, 15),
            new Vector3(25, 1.2f, 20),
            new Vector3(20, 1.2f, 25),  // toward Castel
            new Vector3(15, 1.2f, 28),
            new Vector3(10, 1.2f, 25),  // near Castel
            new Vector3(5, 1.2f, 20),
            new Vector3(0, 1.2f, 18),   // in park
            new Vector3(-10, 1.2f, 15),
            new Vector3(-20, 1.2f, 15), // toward Vatican
            new Vector3(-30, 1.2f, 15),
            new Vector3(-40, 1.2f, 15), // Vatican entrance
            new Vector3(35, 1.2f, -5),  // along Via Ottaviano
            new Vector3(40, 1.2f, -10),
            new Vector3(45, 1.2f, -10), // near Metro
        };

        for (int i = 0; i < positions.Length; i++)
        {
            var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = $"Coin_{i}";
            coin.transform.SetParent(coins.transform);
            coin.transform.localPosition = positions[i];
            coin.transform.localScale = new Vector3(0.6f, 0.08f, 0.6f);
            coin.isStatic = false;
            SetMatDirect(coin, mat);

            // Make trigger
            var col = coin.GetComponent<CapsuleCollider>();
            if (col != null) Object.DestroyImmediate(col);
            var sphere = coin.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.8f;

            // Add CoinPickup script
            coin.AddComponent<CoinPickup>();
        }
    }

    // ═══════════════════════════════════════════════════════
    //  PLAYER SPAWN
    // ═══════════════════════════════════════════════════════
    static void BuildPlayerSpawn(Transform parent)
    {
        // Just a marker — the actual player prefab is placed here
        var spawn = new GameObject("PlayerSpawn");
        spawn.transform.SetParent(parent);
        spawn.transform.localPosition = new Vector3(35, 0f, -2);

        // Add a small gizmo sphere for visibility in editor
        var vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vis.name = "SpawnMarker";
        vis.transform.SetParent(spawn.transform);
        vis.transform.localPosition = Vector3.zero;
        vis.transform.localScale = Vector3.one * 0.5f;
        Object.DestroyImmediate(vis.GetComponent<Collider>());
        SetMat(vis, "M_Petta", COL_PETTA);
    }

    // ═══════════════════════════════════════════════════════
    //  LIGHTING
    // ═══════════════════════════════════════════════════════
    static void BuildLighting()
    {
        // Find or create directional light
        var sun = GameObject.Find("Directional Light");
        if (sun == null)
        {
            sun = new GameObject("Directional Light");
            sun.AddComponent<Light>();
        }

        var light = sun.GetComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.88f); // warm Roman sun
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(45, -30, 0);

        // Ambient
        RenderSettings.ambientLight = new Color(0.65f, 0.62f, 0.72f); // slight blue ambient
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
    }

    // ═══════════════════════════════════════════════════════
    //  H E L P E R S
    // ═══════════════════════════════════════════════════════
    static Transform MakeEmpty(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }

    static GameObject MakeBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.isStatic = true;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // ─── Materials ────────────────────────────────────────

    static void SetMat(GameObject go, string matName, Color color)
    {
        go.GetComponent<Renderer>().sharedMaterial = GetOrCreateMat(matName, color);
    }

    static void SetMatDirect(GameObject go, Material mat)
    {
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static Material GetOrCreateMat(string name, Color color)
    {
        string path = $"{MAT_FOLDER}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        // Ensure directory exists
        EnsureFolder(MAT_FOLDER);

        // Auto-detect render pipeline
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        mat = new Material(shader);

        // Set color for both pipelines
        mat.color = color;                             // Standard
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);         // URP

        // Handle transparency for checkpoint material
        if (color.a < 1f)
        {
            mat.SetFloat("_Mode", 3);                  // Standard transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            if (mat.HasProperty("_Surface"))           // URP transparent
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
            }
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void EnsureFolder(string folderPath)
    {
        // e.g. "Assets/_Levels/Materials"
        string[] parts = folderPath.Split('/');
        string current = parts[0]; // "Assets"
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
