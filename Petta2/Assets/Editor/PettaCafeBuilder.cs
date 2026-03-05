using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor script that builds the Petta Café scene matching the reference image.
/// Menu: Petta > Build Café Scene
/// </summary>
public class PettaCafeBuilder : EditorWindow
{
    // ─── Asset paths (FBX files from MeshyImports — remeshed low-poly) ───
    const string WINDOW_FBX     = "Assets/MeshyImports/Meshy_Model_20260303_142112/Meshy_AI_Crimson_Arch_Window_0303132110_texture.fbx";
    const string WINDOW2_FBX    = "Assets/MeshyImports/Meshy_Model_20260303_142123/Meshy_AI_Crimson_Arch_Window_0303132121_texture.fbx";
    const string PENDANT_FBX    = "Assets/MeshyImports/Meshy_Model_20260303_142135/Meshy_AI_Crimson_Pendant_in_a__0303132133_texture.fbx";
    const string TABLE1_FBX     = "Assets/MeshyImports/Meshy_Model_20260303_142149/Meshy_AI_Square_top_pedestal_t_0303132147_texture.fbx";
    const string TABLE2_FBX     = TABLE1_FBX; // single table variant (remeshed), duplicated in scene
    const string STOOL1_FBX     = "Assets/MeshyImports/Meshy_Model_20260303_142220/Meshy_AI_Wooden_Stool_on_Purpl_0303132218_texture.fbx";
    const string STOOL2_FBX     = STOOL1_FBX; // single stool variant (remeshed), duplicated in scene
    const string COUNTER_FBX    = "Assets/MeshyImports/Meshy_Model_20260303_142207/Meshy_AI_Red_Tile_Counter_0303132205_texture.fbx";
    const string WALKING_FBX    = "Assets/MeshyImports/Walking_20260303_134945/Meshy_AI_Animation_Walking_withSkin.fbx";
    // Reference model — Petta Café single-piece (placed beside for comparison)
    const string CAFE_REF_FBX   = "Assets/MeshyImports/Meshy_Model_20260303_142238/Meshy_AI_Petta_Café_0303132235_texture.fbx";
    // Awning (striped canopy) — user-imported
    const string AWNING_FBX     = "Assets/MeshyImports/Meshy_Model_20260303_160731/Meshy_AI_Red_and_Cream_Striped_0303150727_texture.fbx";
    // Front door (arched glass double door) — user-imported
    const string FRONT_DOOR_FBX = "Assets/MeshyImports/Meshy_Model_20260303_161430/Meshy_AI_Arched_Glass_Double_D_0303151428_texture.fbx";
    // Sign ("Petta" sign) — user-imported
    const string SIGN_FBX       = "Assets/MeshyImports/Meshy_Model_20260303_165327/Meshy_AI_Petta_0303155325_texture.fbx";
    // Test1 — Blender-exported model
    const string TEST1_FBX      = "Assets/MeshyImports/Test1.fbx";
    // Test4 — Blender-exported vetrata (solo vetrata, senza mobili/muri)
    const string TEST4_FBX      = "Assets/MeshyImports/Test4.fbx";

    // ─── Café dimensions (in Unity units/meters) ───
    // The café interior is roughly a 4×3×3.5 box (width × depth × height)
    static readonly float cafeWidth  = 4.0f;
    static readonly float cafeDepth  = 3.0f;
    static readonly float cafeHeight = 3.5f;
    static readonly float tileHeight = 0.8f; // red tile accent at the bottom of walls
    static readonly float wallThickness = 0.08f;

    // Colors matching the image
    static readonly Color pinkWall      = new Color(0.95f, 0.82f, 0.82f, 1f);  // soft pink
    static readonly Color redTile       = new Color(0.82f, 0.35f, 0.30f, 1f);  // coral/red tile
    static readonly Color floorColor    = new Color(0.85f, 0.75f, 0.70f, 1f);  // warm floor
    static readonly Color ceilingColor  = new Color(0.96f, 0.88f, 0.88f, 1f);  // light pink
    static readonly Color bgColor       = new Color(0.58f, 0.47f, 0.52f, 1f);  // purple-ish bg

    [MenuItem("Petta/Build Café Scene")]
    static void BuildCafeScene()
    {
        if (!EditorUtility.DisplayDialog("Build Petta Café",
            "This will clear the current scene and rebuild the Petta Café.\nContinue?",
            "Build", "Cancel"))
            return;

        ClearScene();
        
        GameObject cafeRoot = new GameObject("PettaCafe_Root");
        Undo.RegisterCreatedObjectUndo(cafeRoot, "Build Petta Café");

        // ── 1. Build structural elements ──
        BuildFloor(cafeRoot);
        BuildWalls(cafeRoot);
        BuildCeiling(cafeRoot);

        // ── 2. Place reference model to the side for comparison ──
        PlaceCafeReference(cafeRoot);

        // ── 3. Place window/door at the front ──
        PlaceWindows(cafeRoot);

        // ── 4. Place pendant lamps (3 lamps) ──
        PlacePendantLamps(cafeRoot);

        // ── 5. Place tables (2) ──
        PlaceTables(cafeRoot);

        // ── 6. Place stools (4+) ──
        PlaceStools(cafeRoot);

        // ── 7. Place walking character ──
        PlaceCharacter(cafeRoot);

        // ── 8. Place awning (user-imported) ──
        PlaceAwning(cafeRoot);

        // ── 9. Build counter ──
        BuildCounter(cafeRoot);

        // ── 10. Place sign above awning ──
        PlaceSign(cafeRoot);

        // ── 11. Build street, sidewalks and neighboring buildings ──
        BuildStreetScene(cafeRoot);

        // ── 12. Setup camera ──
        SetupCamera();

        // ── 13. Setup lighting ──
        SetupLighting();

        // ── 14. Set background color ──
        SetupBackground();

        Selection.activeGameObject = cafeRoot;
        EditorUtility.SetDirty(cafeRoot);
        
        Debug.Log("✓ Petta Café scene built successfully!");
    }

    // ═══════════════════════════════════════════════════════════
    //  CLEAR SCENE
    // ═══════════════════════════════════════════════════════════
    static void ClearScene()
    {
        // Destroy all root objects except essential ones we'll recreate
        var allRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in allRoots)
        {
            Undo.DestroyObjectImmediate(go);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  FLOOR
    // ═══════════════════════════════════════════════════════════
    static void BuildFloor(GameObject parent)
    {
        GameObject floor = CreatePrimitive("Floor", PrimitiveType.Cube, parent);
        floor.transform.localPosition = new Vector3(0, -0.025f, 0);
        floor.transform.localScale = new Vector3(cafeWidth + 0.2f, 0.05f, cafeDepth + 0.2f);
        SetColor(floor, floorColor);
    }

    // ═══════════════════════════════════════════════════════════
    //  WALLS
    // ═══════════════════════════════════════════════════════════
    static void BuildWalls(GameObject parent)
    {
        GameObject wallsParent = new GameObject("Walls");
        wallsParent.transform.SetParent(parent.transform);
        wallsParent.transform.localPosition = Vector3.zero;

        // ── Back Wall (partial - with opening for window) ──
        float backWindowWidth = 2.8f;  // wider opening for scale-140 window
        float backSideWidth = (cafeWidth - backWindowWidth) / 2f;

        BuildWallWithTile(wallsParent, "BackWall_Left",
            new Vector3(-cafeWidth / 2f + backSideWidth / 2f, cafeHeight / 2f, cafeDepth / 2f),
            new Vector3(backSideWidth, cafeHeight, wallThickness),
            Quaternion.identity);

        BuildWallWithTile(wallsParent, "BackWall_Right",
            new Vector3(cafeWidth / 2f - backSideWidth / 2f, cafeHeight / 2f, cafeDepth / 2f),
            new Vector3(backSideWidth, cafeHeight, wallThickness),
            Quaternion.identity);

        // Top part above back window
        float backWindowHeight = 3.0f;  // taller opening (nearly full height)
        BuildWallWithTile(wallsParent, "BackWall_Top",
            new Vector3(0, backWindowHeight + (cafeHeight - backWindowHeight) / 2f, cafeDepth / 2f),
            new Vector3(backWindowWidth, cafeHeight - backWindowHeight, wallThickness),
            Quaternion.identity, false);

        // ── Left Wall ──
        BuildWallWithTile(wallsParent, "LeftWall",
            new Vector3(-cafeWidth / 2f, cafeHeight / 2f, 0),
            new Vector3(wallThickness, cafeHeight, cafeDepth),
            Quaternion.identity);

        // ── Right Wall ──
        BuildWallWithTile(wallsParent, "RightWall",
            new Vector3(cafeWidth / 2f, cafeHeight / 2f, 0),
            new Vector3(wallThickness, cafeHeight, cafeDepth),
            Quaternion.identity);

        // ── Rounded Corners (4 vertical cylinders) ──
        float cornerRadius = 0.12f;
        Vector3[] cornerPositions = {
            new Vector3(-cafeWidth / 2f, cafeHeight / 2f, -cafeDepth / 2f), // front-left
            new Vector3( cafeWidth / 2f, cafeHeight / 2f, -cafeDepth / 2f), // front-right
            new Vector3(-cafeWidth / 2f, cafeHeight / 2f,  cafeDepth / 2f), // back-left
            new Vector3( cafeWidth / 2f, cafeHeight / 2f,  cafeDepth / 2f), // back-right
        };
        string[] cornerNames = { "Corner_FL", "Corner_FR", "Corner_BL", "Corner_BR" };
        for (int i = 0; i < 4; i++)
        {
            GameObject corner = CreatePrimitive(cornerNames[i], PrimitiveType.Cylinder, wallsParent);
            corner.transform.localPosition = cornerPositions[i];
            corner.transform.localScale = new Vector3(cornerRadius * 2f, cafeHeight / 2f, cornerRadius * 2f);
            SetColor(corner, pinkWall);

            // Red tile cylinder at the bottom of each corner
            GameObject cornerTile = CreatePrimitive(cornerNames[i] + "_Tile", PrimitiveType.Cylinder, wallsParent);
            cornerTile.transform.localPosition = new Vector3(cornerPositions[i].x, tileHeight / 2f, cornerPositions[i].z);
            cornerTile.transform.localScale = new Vector3((cornerRadius + 0.01f) * 2f, tileHeight / 2f, (cornerRadius + 0.01f) * 2f);
            SetColor(cornerTile, redTile);
        }

        // ── Front Wall (partial - with opening for door) ──
        // Left part of front wall
        float doorWidth = 2.8f;  // same opening width as back wall
        float sideWidth = (cafeWidth - doorWidth) / 2f;

        GameObject frontLeft = BuildWallWithTile(wallsParent, "FrontWall_Left",
            new Vector3(-cafeWidth / 2f + sideWidth / 2f, cafeHeight / 2f, -cafeDepth / 2f),
            new Vector3(sideWidth, cafeHeight, wallThickness),
            Quaternion.identity);

        GameObject frontRight = BuildWallWithTile(wallsParent, "FrontWall_Right",
            new Vector3(cafeWidth / 2f - sideWidth / 2f, cafeHeight / 2f, -cafeDepth / 2f),
            new Vector3(sideWidth, cafeHeight, wallThickness),
            Quaternion.identity);

        // Top part above door
        float doorHeight = 3.0f;  // same opening height as back wall
        GameObject frontTop = BuildWallWithTile(wallsParent, "FrontWall_Top",
            new Vector3(0, doorHeight + (cafeHeight - doorHeight) / 2f, -cafeDepth / 2f),
            new Vector3(doorWidth, cafeHeight - doorHeight, wallThickness),
            Quaternion.identity, false); // no tile at top
    }

    static GameObject BuildWallWithTile(GameObject parent, string name, Vector3 pos, Vector3 scale, Quaternion rot, bool addTile = true)
    {
        GameObject wallGroup = new GameObject(name);
        wallGroup.transform.SetParent(parent.transform);
        wallGroup.transform.localPosition = pos;
        wallGroup.transform.localRotation = rot;

        // Main pink wall (upper part)
        GameObject upper = CreatePrimitive(name + "_Upper", PrimitiveType.Cube, wallGroup);
        if (addTile)
        {
            float upperHeight = scale.y - tileHeight;
            upper.transform.localPosition = new Vector3(0, (scale.y / 2f) - (upperHeight / 2f) - scale.y / 2f + upperHeight / 2f + tileHeight / 2f, 0);
            upper.transform.localScale = new Vector3(scale.x, upperHeight, scale.z);
            SetColor(upper, pinkWall);

            // Red tile at bottom
            GameObject tile = CreatePrimitive(name + "_Tile", PrimitiveType.Cube, wallGroup);
            tile.transform.localPosition = new Vector3(0, -scale.y / 2f + tileHeight / 2f, 0);
            tile.transform.localScale = new Vector3(scale.x + 0.01f, tileHeight, scale.z + 0.01f);
            SetColor(tile, redTile);

            // Tile grid lines
            float tileGridSize = 0.25f;
            for (float y = -scale.y / 2f; y < -scale.y / 2f + tileHeight; y += tileGridSize)
            {
                GameObject hLine = CreatePrimitive("hLine", PrimitiveType.Cube, wallGroup);
                hLine.transform.localPosition = new Vector3(0, y + tileGridSize / 2f, scale.z > wallThickness ? 0 : -0.005f);
                // Determine the line's extent based on wall orientation
                if (scale.z <= wallThickness * 2f) // X-facing wall (front/back)
                {
                    hLine.transform.localScale = new Vector3(scale.x + 0.02f, 0.015f, scale.z + 0.03f);
                }
                else // Z-facing wall (left/right)
                {
                    hLine.transform.localScale = new Vector3(scale.x + 0.03f, 0.015f, scale.z + 0.02f);
                }
                SetColor(hLine, redTile * 0.7f);
            }
        }
        else
        {
            upper.transform.localPosition = Vector3.zero;
            upper.transform.localScale = scale;
            SetColor(upper, pinkWall);
        }

        return wallGroup;
    }

    // ═══════════════════════════════════════════════════════════
    //  CEILING
    // ═══════════════════════════════════════════════════════════
    static void BuildCeiling(GameObject parent)
    {
        GameObject ceiling = CreatePrimitive("Ceiling", PrimitiveType.Cube, parent);
        ceiling.transform.localPosition = new Vector3(0, cafeHeight + 0.025f, 0);
        ceiling.transform.localScale = new Vector3(cafeWidth + 0.2f, 0.05f, cafeDepth + 0.2f);
        SetColor(ceiling, ceilingColor);

        // Top ledge / cornice  
        GameObject cornice = CreatePrimitive("Cornice", PrimitiveType.Cube, parent);
        cornice.transform.localPosition = new Vector3(0, cafeHeight + 0.15f, -cafeDepth / 2f - 0.15f);
        cornice.transform.localScale = new Vector3(cafeWidth + 0.5f, 0.3f, 0.3f);
        SetColor(cornice, pinkWall);
    }

    // ═══════════════════════════════════════════════════════════
    //  REFERENCE MODEL (Petta Café single-piece, placed beside for comparison)
    // ═══════════════════════════════════════════════════════════
    static void PlaceCafeReference(GameObject parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CAFE_REF_FBX);
        if (prefab == null)
        {
            Debug.LogWarning("Petta Café reference FBX not found at: " + CAFE_REF_FBX);
            return;
        }

        GameObject reference = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        reference.name = "PettaCafe_REFERENCE (comparison)";
        reference.transform.SetParent(parent.transform);
        AssignFBXMaterial(reference, CAFE_REF_FBX);
        reference.transform.localScale = Vector3.one;
        reference.transform.localPosition = Vector3.zero;

        // Scale it to match our café height so proportions are comparable
        Bounds refBounds = GetCompositeBounds(reference);
        float currentHeight = refBounds.size.y;
        if (currentHeight < 0.001f)
            currentHeight = Mathf.Max(refBounds.size.x, refBounds.size.y, refBounds.size.z);
        float targetHeight = cafeHeight + 0.5f;
        float scaleFactor = targetHeight / Mathf.Max(currentHeight, 0.01f);
        reference.transform.localScale = reference.transform.localScale * scaleFactor;

        // Recompute bounds after scaling
        refBounds = GetCompositeBounds(reference);

        // Rotate 180° on Y so the front faces the same direction as our café
        reference.transform.localRotation = Quaternion.Euler(0, 180, 0) * reference.transform.localRotation;

        // Recompute bounds after rotation
        refBounds = GetCompositeBounds(reference);

        // Place it to the RIGHT of our café, with a gap, for side-by-side comparison
        float offsetX = cafeWidth / 2f + refBounds.extents.x + 1.5f;
        reference.transform.localPosition = new Vector3(
            offsetX,
            -refBounds.min.y, // bottom at floor level
            0
        );

        Debug.Log("✓ Reference model placed to the right, rotated to face same direction.");
    }

    // ═══════════════════════════════════════════════════════════
    //  WINDOWS / GLASS DOOR
    // ═══════════════════════════════════════════════════════════
    static void PlaceWindows(GameObject parent)
    {
        GameObject windowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WINDOW_FBX);
        if (windowPrefab == null)
        {
            Debug.LogWarning("Window FBX not found at: " + WINDOW_FBX);
            return;
        }

        // Back window (centered on back wall, scale 140, on floor)
        GameObject backWindow = InstantiateAndScale(windowPrefab, "BackWindow_Center", parent, 2.4f);
        AssignFBXMaterial(backWindow, WINDOW_FBX);
        // Reset position, apply user scale, then align to floor
        backWindow.transform.localPosition = Vector3.zero;
        backWindow.transform.localScale = new Vector3(140f, 140f, 140f);
        Bounds bwBounds = GetCompositeBounds(backWindow);
        backWindow.transform.localPosition = new Vector3(0f, -bwBounds.min.y, cafeDepth / 2f - 0.05f);

        // Front door (same scale and Y as back window, on front wall)
        GameObject doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FRONT_DOOR_FBX);
        if (doorPrefab != null)
        {
            GameObject frontDoor = InstantiateAndScale(doorPrefab, "FrontDoor_Center", parent, 2.4f);
            AssignFBXMaterials(frontDoor, FRONT_DOOR_FBX);
            frontDoor.transform.localPosition = Vector3.zero;
            frontDoor.transform.localScale = new Vector3(140f, 140f, 140f);
            Bounds fdBounds = GetCompositeBounds(frontDoor);
            frontDoor.transform.localPosition = new Vector3(0f, -fdBounds.min.y, -cafeDepth / 2f + 0.05f);
        }
        else
        {
            Debug.LogWarning("Front door FBX not found at: " + FRONT_DOOR_FBX);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  PENDANT LAMPS (x3)
    // ═══════════════════════════════════════════════════════════
    static void PlacePendantLamps(GameObject parent)
    {
        GameObject lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PENDANT_FBX);
        if (lampPrefab == null)
        {
            Debug.LogWarning("Pendant FBX not found at: " + PENDANT_FBX);
            return;
        }

        GameObject lampsParent = new GameObject("Lamps");
        lampsParent.transform.SetParent(parent.transform);
        lampsParent.transform.localPosition = Vector3.zero;

        // 3 lamps evenly spaced along the width, near the front
        float lampY = cafeHeight - 1.2f; // pulled down lower
        float[] lampXPositions = { -1.2f, 0f, 1.2f };
        float lampZ = -0.3f; // slightly toward the front

        for (int i = 0; i < 3; i++)
        {
            GameObject lamp = InstantiateAndScale(lampPrefab, $"PendantLamp_{i + 1}", lampsParent, 0.8f);
            AssignFBXMaterial(lamp, PENDANT_FBX);
            lamp.transform.localPosition = new Vector3(lampXPositions[i], lampY, lampZ);

            // Add a point light for atmosphere
            GameObject lightObj = new GameObject($"LampLight_{i + 1}");
            lightObj.transform.SetParent(lamp.transform);
            lightObj.transform.localPosition = new Vector3(0, -0.2f, 0);
            Light pointLight = lightObj.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(1f, 0.85f, 0.7f); // warm light
            pointLight.intensity = 1.5f;
            pointLight.range = 3f;
            pointLight.shadows = LightShadows.Soft;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  TABLES (x2)
    // ═══════════════════════════════════════════════════════════
    static void PlaceTables(GameObject parent)
    {
        GameObject table1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TABLE1_FBX);
        GameObject table2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TABLE2_FBX);

        if (table1Prefab == null && table2Prefab == null)
        {
            Debug.LogWarning("Table FBX not found!");
            return;
        }

        GameObject tablesParent = new GameObject("Tables");
        tablesParent.transform.SetParent(parent.transform);
        tablesParent.transform.localPosition = Vector3.zero;

        // Table 1: left side of café (symmetric to right)
        if (table1Prefab != null)
        {
            GameObject t1 = InstantiateAndScale(table1Prefab, "Table_1", tablesParent, 1.0f);
            AssignFBXMaterial(t1, TABLE1_FBX);
            // Reset position, apply user scale and rotation, then align to floor
            t1.transform.localPosition = Vector3.zero;
            t1.transform.localScale = new Vector3(50f, 50f, 50f);
            t1.transform.localRotation = Quaternion.Euler(-90f, 45f, 0f);
            Bounds t1Bounds = GetCompositeBounds(t1);
            t1.transform.localPosition = new Vector3(-1.0f, -t1Bounds.min.y, -0.6f);
        }

        // Table 2: right side of café
        GameObject t2Prefab = table2Prefab != null ? table2Prefab : table1Prefab;
        if (t2Prefab != null)
        {
            GameObject t2 = InstantiateAndScale(t2Prefab, "Table_2", tablesParent, 1.0f);
            AssignFBXMaterial(t2, TABLE2_FBX);
            // Reset position, apply user scale and rotation, then align to floor
            t2.transform.localPosition = Vector3.zero;
            t2.transform.localScale = new Vector3(50f, 50f, 50f);
            t2.transform.localRotation = Quaternion.Euler(-90f, 45f, 0f);
            Bounds t2Bounds = GetCompositeBounds(t2);
            t2.transform.localPosition = new Vector3(1.0f, -t2Bounds.min.y, -0.6f);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  STOOLS (x4+, arranged around tables)
    // ═══════════════════════════════════════════════════════════
    static void PlaceStools(GameObject parent)
    {
        GameObject stool1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(STOOL1_FBX);
        GameObject stool2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(STOOL2_FBX);

        if (stool1Prefab == null && stool2Prefab == null)
        {
            Debug.LogWarning("Stool FBX not found!");
            return;
        }

        GameObject stoolsParent = new GameObject("Stools");
        stoolsParent.transform.SetParent(parent.transform);
        stoolsParent.transform.localPosition = Vector3.zero;

        // Stool positions: 3 per table, arranged around each table
        // Table 1 is at (-1.0, 0, -0.6)
        // Table 2 is at (1.0, 0, -0.6)
        float stoolOffset = 0.5f;
        Vector3[] stoolPositions = {
            // Around Table 1 (left)
            new Vector3(-1.0f - stoolOffset, 0, -0.6f),          // left of table 1
            new Vector3(-1.0f + stoolOffset, 0, -0.6f),          // right of table 1
            new Vector3(-1.0f, 0, -0.6f - stoolOffset),          // front of table 1
            // Around Table 2 (right)
            new Vector3(1.0f - stoolOffset, 0, -0.6f),           // left of table 2
            new Vector3(1.0f + stoolOffset, 0, -0.6f),           // right of table 2
            new Vector3(1.0f, 0, -0.6f - stoolOffset),           // front of table 2
        };

        for (int i = 0; i < stoolPositions.Length; i++)
        {
            // Alternate between stool variants
            GameObject prefab = (i % 2 == 0) ? (stool1Prefab ?? stool2Prefab) : (stool2Prefab ?? stool1Prefab);
            GameObject stool = InstantiateAndScale(prefab, $"Stool_{i + 1}", stoolsParent, 0.7f);
            AssignFBXMaterial(stool, STOOL1_FBX);
            // Reset position, apply user scale, then align to floor
            stool.transform.localPosition = Vector3.zero;
            stool.transform.localScale = new Vector3(25f, 10f, 20f);
            Bounds stoolBounds = GetCompositeBounds(stool);
            Vector3 sp = stoolPositions[i];
            sp.y = -stoolBounds.min.y;
            stool.transform.localPosition = sp;

            // Slight Y rotation variation for natural look —
            // MULTIPLY with existing rotation to preserve the FBX -90° X fix
            float yRot = Random.Range(-15f, 15f);
            stool.transform.localRotation = Quaternion.Euler(0, yRot, 0) * stool.transform.localRotation;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  WALKING CHARACTER
    // ═══════════════════════════════════════════════════════════
    static void PlaceCharacter(GameObject parent)
    {
        GameObject charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WALKING_FBX);
        if (charPrefab == null)
        {
            Debug.LogWarning("Walking character FBX not found at: " + WALKING_FBX);
            return;
        }

        GameObject character = (GameObject)PrefabUtility.InstantiatePrefab(charPrefab);
        character.name = "Character_Walking";
        character.transform.SetParent(parent.transform);
        // Keep the prefab's original rotation
        character.transform.localScale = Vector3.one;
        character.transform.localPosition = Vector3.zero;

        // Scale character to fit the café proportionally
        Bounds charBounds = GetCompositeBounds(character);
        float currentCharHeight = charBounds.size.y;
        if (currentCharHeight < 0.001f)
            currentCharHeight = Mathf.Max(charBounds.size.x, charBounds.size.y, charBounds.size.z);
        // Target: character should be about 45% of café height for a cute diorama proportion
        float targetCharHeight = cafeHeight * 0.45f;
        float charScale = targetCharHeight / Mathf.Max(currentCharHeight, 0.01f);
        character.transform.localScale = character.transform.localScale * charScale;

        // Recompute bounds after scaling
        charBounds = GetCompositeBounds(character);

        // Align bottom to floor
        Vector3 charPos = character.transform.localPosition;
        charPos.y -= charBounds.min.y;
        // Place OUTSIDE the café (in front, to avoid clutter)
        charPos.x = 0f;
        charPos.z = -cafeDepth / 2f - 1.5f;
        character.transform.localPosition = charPos;

        // Face towards interior: multiply Y rotation with the existing rotation
        character.transform.localRotation = Quaternion.Euler(0, 160, 0) * character.transform.localRotation;

        // ── Assign the correct material with texture ──
        // Build material from scratch to guarantee texture shows in URP
        const string WALKING_TEX = "Assets/MeshyImports/Walking_20260303_134945/Meshy_AI_texture_0.png";
        Texture2D charTex = AssetDatabase.LoadAssetAtPath<Texture2D>(WALKING_TEX);
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        if (charTex != null && urpLit != null)
        {
            Material charMat = new Material(urpLit);
            charMat.name = "WalkingChar_Runtime";
            charMat.SetTexture("_BaseMap", charTex);
            charMat.SetTexture("_MainTex", charTex);
            charMat.SetColor("_BaseColor", Color.white);
            charMat.SetColor("_Color", Color.white);
            // Disable emission so it's not washed out
            charMat.DisableKeyword("_EMISSION");
            charMat.SetColor("_EmissionColor", Color.black);

            Renderer[] renderers = character.GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                Material[] mats = new Material[rend.sharedMaterials.Length];
                for (int m = 0; m < mats.Length; m++)
                    mats[m] = charMat;
                rend.materials = mats; // use .materials (instance) not .sharedMaterials
            }
            Debug.Log($"✓ Walking character material created from texture ({renderers.Length} renderers).");
        }
        else
        {
            Debug.LogWarning($"Could not create character material. Texture found: {charTex != null}, Shader found: {urpLit != null}");
        }

        // Setup Animator if there's animation
        Animator animator = character.GetComponent<Animator>();
        if (animator == null)
            animator = character.GetComponentInChildren<Animator>();
        
        if (animator != null)
        {
            // Try to find or create an AnimatorController
            SetupAnimator(character, animator);
        }

        // Add CharacterWalker script for runtime movement
        CharacterWalker walker = character.GetComponent<CharacterWalker>();
        if (walker == null)
            walker = character.AddComponent<CharacterWalker>();
        walker.speed = 1.2f;
        walker.walkDistance = 6f;
        walker.walkDirection = Vector3.right; // walk along sidewalk
    }

    static void SetupAnimator(GameObject character, Animator animator)
    {
        // Check if the FBX has animation clips
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(WALKING_FBX);
        AnimationClip walkClip = null;
        
        foreach (var asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                walkClip = clip;
                break;
            }
        }

        if (walkClip != null)
        {
            // Create an AnimatorController
            string controllerPath = "Assets/Editor/WalkingAnimator.controller";
            
            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootStateMachine = controller.layers[0].stateMachine;
            var walkState = rootStateMachine.AddState("Walking");
            walkState.motion = walkClip;
            
            animator.runtimeAnimatorController = controller;
            Debug.Log("✓ Walking animation controller created and assigned.");
        }
        else
        {
            Debug.LogWarning("No animation clips found in Walking FBX.");
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  CAMERA
    // ═══════════════════════════════════════════════════════════
    static void SetupCamera()
    {
        // Create new camera with isometric-like angle matching the reference image
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        
        Camera cam = camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();

        // Position for 3/4 view like the reference image
        camObj.transform.position = new Vector3(3.5f, 4.0f, -4.5f);
        camObj.transform.rotation = Quaternion.Euler(30, -35, 0);

        cam.fieldOfView = 35; // narrower FOV for more orthographic-like feel
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        cam.backgroundColor = bgColor;
        cam.clearFlags = CameraClearFlags.SolidColor;

        // Add URP camera data if available
        var urpCamType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpCamType != null)
        {
            camObj.AddComponent(urpCamType);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  LIGHTING
    // ═══════════════════════════════════════════════════════════
    static void SetupLighting()
    {
        // Main directional light (soft, warm)
        GameObject lightObj = new GameObject("Directional Light");
        Light dirLight = lightObj.AddComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.color = new Color(1f, 0.95f, 0.9f); // slightly warm
        dirLight.intensity = 1.5f;
        dirLight.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
        lightObj.transform.position = new Vector3(0, 5, 0);

        // Add URP light data if available
        var urpLightType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalLightData, Unity.RenderPipelines.Universal.Runtime");
        if (urpLightType != null)
        {
            lightObj.AddComponent(urpLightType);
        }

        // Fill light (softer, from the opposite side)
        GameObject fillLightObj = new GameObject("Fill Light");
        Light fillLight = fillLightObj.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.color = new Color(0.85f, 0.85f, 1f); // cool fill
        fillLight.intensity = 0.5f;
        fillLight.shadows = LightShadows.None;
        fillLightObj.transform.rotation = Quaternion.Euler(30, 150, 0);
    }

    // ═══════════════════════════════════════════════════════════
    //  BACKGROUND
    // ═══════════════════════════════════════════════════════════
    static void SetupBackground()
    {
        // Create a Global Volume for post-processing if URP is available
        var volumeType = System.Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
        if (volumeType != null)
        {
            GameObject volumeObj = new GameObject("Global Volume");
            var volume = volumeObj.AddComponent(volumeType) as UnityEngine.Rendering.Volume;
            if (volume != null)
            {
                volume.isGlobal = true;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  STREET SCENE — road, sidewalks, neighboring buildings
    // ═══════════════════════════════════════════════════════════
    static void BuildStreetScene(GameObject parent)
    {
        GameObject streetRoot = new GameObject("StreetScene");
        streetRoot.transform.SetParent(parent.transform);
        streetRoot.transform.localPosition = Vector3.zero;

        // --- Colors ---
        Color asphaltColor    = new Color(0.25f, 0.25f, 0.27f, 1f);  // dark grey road
        Color sidewalkColor   = new Color(0.75f, 0.72f, 0.68f, 1f);  // light grey/beige
        Color curbColor       = new Color(0.60f, 0.58f, 0.55f, 1f);  // slightly darker
        Color buildingColor1  = new Color(0.88f, 0.82f, 0.76f, 1f);  // cream/beige
        Color buildingColor2  = new Color(0.80f, 0.75f, 0.72f, 1f);  // warm grey
        Color roofColor       = new Color(0.55f, 0.35f, 0.30f, 1f);  // terracotta
        Color roadLineColor   = new Color(0.95f, 0.95f, 0.90f, 1f);  // white road markings
        Color windowColor     = new Color(0.65f, 0.78f, 0.88f, 1f);  // light blue
        Color doorColor       = new Color(0.45f, 0.30f, 0.20f, 1f);  // brown

        float streetLength = 20f;  // total length along X
        float roadWidth    = 6f;   // road width
        float sidewalkW    = 2.0f; // sidewalk width
        float sidewalkH    = 0.15f; // curb height
        float cafeZ        = -cafeDepth / 2f; // front of café
        float sidewalkZ    = cafeZ - sidewalkW; // front edge of sidewalk
        float roadZ        = sidewalkZ - roadWidth; // far edge of road

        // ── SIDEWALK (café side) ──
        GameObject sidewalk = CreatePrimitive("Sidewalk", PrimitiveType.Cube, streetRoot);
        sidewalk.transform.localScale = new Vector3(streetLength, sidewalkH, sidewalkW);
        sidewalk.transform.localPosition = new Vector3(0f, sidewalkH / 2f, cafeZ - sidewalkW / 2f);
        SetColor(sidewalk, sidewalkColor);

        // Curb
        GameObject curb = CreatePrimitive("Curb", PrimitiveType.Cube, streetRoot);
        curb.transform.localScale = new Vector3(streetLength, sidewalkH + 0.05f, 0.12f);
        curb.transform.localPosition = new Vector3(0f, (sidewalkH + 0.05f) / 2f, sidewalkZ - 0.06f);
        SetColor(curb, curbColor);

        // ── ROAD ──
        GameObject road = CreatePrimitive("Road", PrimitiveType.Cube, streetRoot);
        road.transform.localScale = new Vector3(streetLength, 0.05f, roadWidth);
        road.transform.localPosition = new Vector3(0f, 0.025f, sidewalkZ - roadWidth / 2f);
        SetColor(road, asphaltColor);

        // Road center line (dashed)
        float dashLen = 1.0f;
        float gapLen  = 0.6f;
        float lineZ   = sidewalkZ - roadWidth / 2f;
        for (float x = -streetLength / 2f; x < streetLength / 2f; x += dashLen + gapLen)
        {
            GameObject dash = CreatePrimitive("RoadDash", PrimitiveType.Cube, streetRoot);
            dash.transform.localScale = new Vector3(dashLen, 0.06f, 0.12f);
            dash.transform.localPosition = new Vector3(x + dashLen / 2f, 0.055f, lineZ);
            SetColor(dash, roadLineColor);
        }

        // ── OPPOSITE SIDEWALK ──
        GameObject sidewalk2 = CreatePrimitive("Sidewalk_Opposite", PrimitiveType.Cube, streetRoot);
        sidewalk2.transform.localScale = new Vector3(streetLength, sidewalkH, sidewalkW);
        sidewalk2.transform.localPosition = new Vector3(0f, sidewalkH / 2f, roadZ - sidewalkW / 2f);
        SetColor(sidewalk2, sidewalkColor);

        // Curb opposite
        GameObject curb2 = CreatePrimitive("Curb_Opposite", PrimitiveType.Cube, streetRoot);
        curb2.transform.localScale = new Vector3(streetLength, sidewalkH + 0.05f, 0.12f);
        curb2.transform.localPosition = new Vector3(0f, (sidewalkH + 0.05f) / 2f, roadZ + 0.06f);
        SetColor(curb2, curbColor);

        // ── NEIGHBORING BUILDINGS (same side as café, left and right) ──
        BuildNeighborBuilding(streetRoot, "Building_Left",
            new Vector3(-cafeWidth / 2f - 2.5f, 0f, 0f),
            new Vector3(5f, 5.0f, cafeDepth),
            buildingColor1, roofColor, windowColor, doorColor, true);

        BuildNeighborBuilding(streetRoot, "Building_Right",
            new Vector3(cafeWidth / 2f + 2.5f, 0f, 0f),
            new Vector3(5f, 4.0f, cafeDepth),
            buildingColor2, roofColor, windowColor, doorColor, false);

        // Taller building further left
        BuildNeighborBuilding(streetRoot, "Building_FarLeft",
            new Vector3(-cafeWidth / 2f - 7.5f, 0f, 0f),
            new Vector3(5f, 6.5f, cafeDepth),
            buildingColor2, roofColor, windowColor, doorColor, true);

        Debug.Log("✓ Street scene built (road, sidewalks, 3 buildings).");
    }

    static void BuildNeighborBuilding(GameObject parent, string name,
        Vector3 offset, Vector3 size, Color wallCol, Color roofCol,
        Color winCol, Color doorCol, bool hasDoor)
    {
        GameObject bldg = new GameObject(name);
        bldg.transform.SetParent(parent.transform);
        bldg.transform.localPosition = offset;

        float w = size.x, h = size.y, d = size.z;

        // Main body
        GameObject body = CreatePrimitive(name + "_Body", PrimitiveType.Cube, bldg);
        body.transform.localScale = new Vector3(w, h, d);
        body.transform.localPosition = new Vector3(0f, h / 2f, 0f);
        SetColor(body, wallCol);

        // Roof slab
        GameObject roof = CreatePrimitive(name + "_Roof", PrimitiveType.Cube, bldg);
        roof.transform.localScale = new Vector3(w + 0.2f, 0.15f, d + 0.2f);
        roof.transform.localPosition = new Vector3(0f, h + 0.075f, 0f);
        SetColor(roof, roofCol);

        // Front face windows (2 rows)
        float frontZ = -d / 2f - 0.02f;
        int floors = Mathf.FloorToInt(h / 2.5f);
        for (int floor = 0; floor < floors; floor++)
        {
            float winY = 1.5f + floor * 2.5f;
            // 2 windows per floor
            for (int wx = -1; wx <= 1; wx += 2)
            {
                GameObject win = CreatePrimitive(name + "_Win", PrimitiveType.Cube, bldg);
                win.transform.localScale = new Vector3(0.7f, 0.9f, 0.05f);
                win.transform.localPosition = new Vector3(wx * 0.9f, winY, frontZ);
                SetColor(win, winCol);

                // Window frame
                GameObject frame = CreatePrimitive(name + "_Frame", PrimitiveType.Cube, bldg);
                frame.transform.localScale = new Vector3(0.8f, 1.0f, 0.03f);
                frame.transform.localPosition = new Vector3(wx * 0.9f, winY, frontZ - 0.01f);
                SetColor(frame, new Color(0.9f, 0.9f, 0.88f));
            }
        }

        // Door on ground floor
        if (hasDoor)
        {
            GameObject door = CreatePrimitive(name + "_Door", PrimitiveType.Cube, bldg);
            door.transform.localScale = new Vector3(0.9f, 1.8f, 0.06f);
            door.transform.localPosition = new Vector3(0f, 0.9f, frontZ);
            SetColor(door, doorCol);
        }
    }

    /// <summary>
    /// Combine multiple meshes into a single mesh.
    /// </summary>
    static Mesh CombineMeshes(List<Mesh> meshes)
    {
        CombineInstance[] combine = new CombineInstance[meshes.Count];
        for (int i = 0; i < meshes.Count; i++)
        {
            combine[i].mesh = meshes[i];
            combine[i].transform = Matrix4x4.identity;
        }
        Mesh combined = new Mesh();
        combined.name = "Combined_Mesh";
        combined.CombineMeshes(combine, true, true);
        combined.RecalculateBounds();
        combined.RecalculateNormals();
        return combined;
    }

    // ═══════════════════════════════════════════════════════════
    //  SIGN ("Petta" sign above awning on front wall)
    // ═══════════════════════════════════════════════════════════
    static void PlaceSign(GameObject parent)
    {
        GameObject signPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SIGN_FBX);
        if (signPrefab == null)
        {
            Debug.LogWarning("Sign FBX not found at: " + SIGN_FBX);
            return;
        }

        GameObject sign = InstantiateAndScale(signPrefab, "Sign_Petta", parent, 0.5f);
        AssignFBXMaterial(sign, SIGN_FBX);

        // Reset and scale
        sign.transform.localPosition = Vector3.zero;
        sign.transform.localScale = new Vector3(100f, 100f, 100f);
        sign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * sign.transform.localRotation;

        // User-specified position
        sign.transform.localPosition = new Vector3(0f, 3.6f, -1.9f);

        Debug.Log("✓ Sign placed above awning.");
    }

    // ═══════════════════════════════════════════════════════════
    //  AWNING (user-imported striped canopy)
    // ═══════════════════════════════════════════════════════════
    static void PlaceAwning(GameObject parent)
    {
        GameObject awningPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AWNING_FBX);
        if (awningPrefab == null)
        {
            Debug.LogWarning("Awning FBX not found at: " + AWNING_FBX);
            return;
        }

        // Instantiate and scale to match café width
        GameObject awning = InstantiateAndScale(awningPrefab, "Awning", parent, 1.0f);
        AssignFBXMaterial(awning, AWNING_FBX);

        // Apply user-specified scale, rotation and position
        awning.transform.localPosition = Vector3.zero;
        awning.transform.localScale = new Vector3(160f, 200f, 200f);
        awning.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * awning.transform.localRotation;

        // Position on front wall, Y = 2.64
        Bounds awBounds = GetCompositeBounds(awning);
        awning.transform.localPosition = new Vector3(
            0f,
            2.64f,
            -2.16f
        );

        Debug.Log("✓ Awning placed above front entrance.");
    }

    // ═══════════════════════════════════════════════════════════
    //  COUNTER / BAR (red tile counter at the back)
    // ═══════════════════════════════════════════════════════════
    [MenuItem("Petta/Add Counter")]
    static void AddCounter()
    {
        GameObject cafeRoot = GameObject.Find("PettaCafe_Root");
        if (cafeRoot == null)
        {
            Debug.LogError("Build the café scene first!");
            return;
        }
        BuildCounter(cafeRoot);
    }

    static void BuildCounter(GameObject parent)
    {
        // Try to use the real Red Tile Counter model
        GameObject counterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(COUNTER_FBX);
        if (counterPrefab != null)
        {
            // Use the Meshy counter model
            GameObject counter = InstantiateAndScale(counterPrefab, "Counter_RedTile", parent, 1.0f);
            AssignFBXMaterial(counter, COUNTER_FBX);
            // Override scale and position to user-specified values
            counter.transform.localScale = new Vector3(100f, 150f, 100f);
            counter.transform.localPosition = new Vector3(0.482f, 0.5f, 0.482f);
            Debug.Log("✓ Red Tile Counter model placed.");
        }
        else
        {
            // Fallback: procedural counter
            Debug.LogWarning("Counter FBX not found, building procedural counter.");
            GameObject counter = new GameObject("Counter");
            counter.transform.SetParent(parent.transform);
            counter.transform.localPosition = Vector3.zero;

            GameObject body = CreatePrimitive("CounterBody", PrimitiveType.Cube, counter);
            body.transform.localPosition = new Vector3(0, 0.5f, cafeDepth / 2f - 0.4f);
            body.transform.localScale = new Vector3(cafeWidth - 0.4f, 1.0f, 0.6f);
            SetColor(body, redTile);

            GameObject top = CreatePrimitive("CounterTop", PrimitiveType.Cube, counter);
            top.transform.localPosition = new Vector3(0, 1.02f, cafeDepth / 2f - 0.4f);
            top.transform.localScale = new Vector3(cafeWidth - 0.3f, 0.06f, 0.7f);
            SetColor(top, new Color(0.7f, 0.5f, 0.35f));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  BUILD COMPLETE SCENE (with all extras)
    // ═══════════════════════════════════════════════════════════
    [MenuItem("Petta/Build Complete Café (with Awning + Counter)")]
    static void BuildCompleteCafe()
    {
        // This now duplicates — BuildCafeScene already includes awning + counter.
        // Keeping for backward compat, just calls main build.
        BuildCafeScene();
        
        Debug.Log("✓ Complete Petta Café with awning and counter built!");
    }

    // ═══════════════════════════════════════════════════════════
    //  PREVIEW TEST1 — place Test1.fbx in scene for inspection
    // ═══════════════════════════════════════════════════════════
    [MenuItem("Petta/Preview Test1")]
    static void PreviewTest1()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TEST1_FBX);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Preview Test1", "Test1.fbx not found at:\n" + TEST1_FBX, "OK");
            return;
        }

        // Remove previous preview if any
        GameObject old = GameObject.Find("Test1_Preview");
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Test1_Preview";
        Undo.RegisterCreatedObjectUndo(instance, "Preview Test1");

        // Keep original rotation, scale to ~3m tall
        instance.transform.localScale = Vector3.one;
        instance.transform.localPosition = Vector3.zero;

        Bounds bounds = GetCompositeBounds(instance);
        float currentHeight = bounds.size.y;
        if (currentHeight < 0.001f)
            currentHeight = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float targetHeight = 3.0f;
        float scaleFactor = targetHeight / Mathf.Max(currentHeight, 0.01f);
        instance.transform.localScale = instance.transform.localScale * scaleFactor;

        // Align bottom to floor
        bounds = GetCompositeBounds(instance);
        instance.transform.localPosition = new Vector3(0f, -bounds.min.y, 0f);

        // Try to assign material from original café ref (since Test1 is exported from Blender without textures)
        AssignFBXMaterial(instance, CAFE_REF_FBX);

        Selection.activeGameObject = instance;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log($"✓ Test1 preview placed. Bounds: {bounds.size}, Vertices: {CountVertices(instance)}");
    }

    // ═══════════════════════════════════════════════════════════
    //  PREVIEW TEST4 — place Test4.fbx (vetrata) in scene
    // ═══════════════════════════════════════════════════════════
    [MenuItem("Petta/Preview Test4 (Vetrata)")]
    static void PreviewTest4()
    {
        PreviewBlenderExport(TEST4_FBX, "Test4_Preview");
    }

    /// <summary>
    /// Generic preview for Blender-exported FBX models.
    /// Scales to 3m, aligns to floor, assigns café ref material, logs vertex count.
    /// </summary>
    static void PreviewBlenderExport(string fbxPath, string previewName)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Preview", "FBX not found at:\n" + fbxPath, "OK");
            return;
        }

        // Remove previous preview if any
        GameObject old = GameObject.Find(previewName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = previewName;
        Undo.RegisterCreatedObjectUndo(instance, "Preview " + previewName);

        instance.transform.localScale = Vector3.one;
        instance.transform.localPosition = Vector3.zero;

        Bounds bounds = GetCompositeBounds(instance);
        float currentHeight = bounds.size.y;
        if (currentHeight < 0.001f)
            currentHeight = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float targetHeight = 3.0f;
        float scaleFactor = targetHeight / Mathf.Max(currentHeight, 0.01f);
        instance.transform.localScale = instance.transform.localScale * scaleFactor;

        bounds = GetCompositeBounds(instance);
        instance.transform.localPosition = new Vector3(0f, -bounds.min.y, 0f);

        // Assign transparent glass material (azzurro, see-through)
        AssignGlassMaterial(instance);

        Selection.activeGameObject = instance;
        SceneView.lastActiveSceneView?.FrameSelected();

        int verts = CountVertices(instance);
        int tris = CountTriangles(instance);
        Debug.Log($"✓ {previewName} placed. Bounds: {bounds.size}, Vertices: {verts}, Triangles: {tris}");
    }

    static int CountTriangles(GameObject go)
    {
        int count = 0;
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh != null) count += mf.sharedMesh.triangles.Length / 3;
        }
        return count;
    }

    static int CountVertices(GameObject go)
    {
        int count = 0;
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh != null) count += mf.sharedMesh.vertexCount;
        }
        return count;
    }

    /// <summary>
    /// Assign a transparent blue-tinted glass material (URP) to all renderers.
    /// Slightly blue from inside, see-through, glossy surface.
    /// </summary>
    static void AssignGlassMaterial(GameObject instance)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");
        if (urpLit == null) return;

        Material glassMat = new Material(urpLit);
        glassMat.name = "Vetrata_Glass";

        // Transparent light blue tint — subtle, no weird reflections
        Color glassColor = new Color(0.75f, 0.88f, 1.0f, 0.18f); // azzurro molto leggero
        glassMat.SetColor("_BaseColor", glassColor);
        glassMat.SetColor("_Color", glassColor);

        // URP transparency setup
        glassMat.SetFloat("_Surface", 1);        // 0=Opaque, 1=Transparent
        glassMat.SetFloat("_Blend", 0);           // 0=Alpha (cleanest blend mode)
        glassMat.SetFloat("_AlphaClip", 0);       // no alpha clip
        glassMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glassMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        glassMat.SetFloat("_ZWrite", 0);          // disable z-write for transparency

        // Low smoothness + no metallic = NO specular reflections/artifacts
        glassMat.SetFloat("_Smoothness", 0.4f);   // moderate, avoids mirror-like reflections
        glassMat.SetFloat("_Metallic", 0f);        // non-metallic = no environment reflections

        // Disable specular highlights to prevent "flashing" artifacts
        glassMat.SetFloat("_SpecularHighlights", 0f);
        glassMat.SetFloat("_EnvironmentReflections", 0f);
        glassMat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        glassMat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");

        // Render both sides (double-sided) so glass looks correct from inside AND outside
        glassMat.SetFloat("_Cull", 0); // 0=Off (double-sided), 1=Front, 2=Back

        // Enable transparency keywords
        glassMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        glassMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");  // plain alpha blend, no premultiply artifacts
        glassMat.DisableKeyword("_ALPHATEST_ON");
        glassMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        // Disable emission
        glassMat.DisableKeyword("_EMISSION");
        glassMat.SetColor("_EmissionColor", Color.black);

        // No shadow casting from glass (avoids dark shadow rectangles)
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Material[] mats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = glassMat;
            rend.sharedMaterials = mats;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // no shadow from glass
        }
        Debug.Log($"✓ Glass material assigned ({renderers.Length} renderers, alpha={glassColor.a}, no reflections)");
    }

    // ═══════════════════════════════════════════════════════════
    //  UTILITY METHODS
    // ═══════════════════════════════════════════════════════════

    static GameObject CreatePrimitive(string name, PrimitiveType type, GameObject parent)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent.transform);
        
        // Remove collider for visual-only objects
        var collider = obj.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);
        
        return obj;
    }

    static void SetColor(GameObject obj, Color color)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create a URP-compatible material
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
            {
                mat = new Material(Shader.Find("Standard"));
            }
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.3f);
            renderer.material = mat;
        }
    }

    static GameObject InstantiateAndScale(GameObject prefab, string name, GameObject parent, float targetHeight)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent.transform);
        // KEEP the prefab's original rotation (Meshy FBX models are Z-up,
        // Unity importer adds -90° on X to stand them upright).
        // Do NOT reset to Quaternion.identity.
        instance.transform.localScale = Vector3.one;
        instance.transform.localPosition = Vector3.zero;

        // Measure current bounds (world-space, already accounts for rotation)
        Bounds bounds = GetCompositeBounds(instance);
        float currentHeight = bounds.size.y;

        // Safety: if height is negligible, use the largest dimension
        if (currentHeight < 0.001f)
        {
            currentHeight = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        }

        float scale = targetHeight / Mathf.Max(currentHeight, 0.01f);
        instance.transform.localScale = instance.transform.localScale * scale;

        // Recompute bounds after scaling and align bottom to floor (y=0)
        bounds = GetCompositeBounds(instance);
        float bottomY = bounds.min.y;
        Vector3 pos = instance.transform.localPosition;
        pos.y -= bottomY;
        instance.transform.localPosition = pos;

        return instance;
    }

    /// <summary>
    /// Assign the Material.001.mat from the same folder as the FBX to all renderers.
    /// Fixes white models after crash/recovery or broken material links.
    /// </summary>
    static void AssignFBXMaterial(GameObject instance, string fbxPath)
    {
        // Derive material path from the FBX path's directory
        string dir = System.IO.Path.GetDirectoryName(fbxPath);
        string matPath = dir + "/Material.001.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            // Try loading embedded materials from the FBX itself
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var sub in subAssets)
            {
                if (sub is Material m)
                {
                    mat = m;
                    break;
                }
            }
        }
        if (mat == null) return;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Material[] mats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = mat;
            rend.sharedMaterials = mats;
        }
    }

    /// <summary>
    /// Assign ALL .mat files from the FBX folder, matching by material name.
    /// Uses embedded FBX materials as source of truth for slot names,
    /// so it works even when current material references are null/broken.
    /// Preserves multi-material setups (e.g. opaque + transparent glass).
    /// </summary>
    static void AssignFBXMaterials(GameObject instance, string fbxPath)
    {
        string dir = System.IO.Path.GetDirectoryName(fbxPath);
        // Load all extracted .mat files from the directory, keyed by m_Name
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { dir });
        var matsByName = new System.Collections.Generic.Dictionary<string, Material>();
        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null && !matsByName.ContainsKey(m.name))
                matsByName[m.name] = m;
        }

        if (matsByName.Count == 0) return;

        // Load embedded materials from FBX to know the correct slot names/order
        // This is the source of truth even when current slot refs are null
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        var embeddedMats = new System.Collections.Generic.List<Material>();
        foreach (var sub in subAssets)
        {
            if (sub is Material m)
                embeddedMats.Add(m);
        }

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Material[] currentMats = rend.sharedMaterials;
            Material[] newMats = new Material[currentMats.Length];
            for (int i = 0; i < currentMats.Length; i++)
            {
                // Determine slot name: prefer current material name, fall back to embedded FBX material
                string slotName = "";
                if (currentMats[i] != null)
                    slotName = currentMats[i].name;
                else if (i < embeddedMats.Count)
                    slotName = embeddedMats[i].name;

                if (matsByName.TryGetValue(slotName, out Material found))
                    newMats[i] = found;
                else if (matsByName.ContainsKey("Material.001"))
                    newMats[i] = matsByName["Material.001"]; // fallback
                else
                    newMats[i] = currentMats[i]; // keep original
            }
            rend.sharedMaterials = newMats;
        }
    }


    static Bounds GetCompositeBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(go.transform.position, Vector3.one * 0.1f);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    // ═══════════════════════════════════════════════════════════════
    //  CLEAN CAFFÈ — Remove furniture from Petta Café → new "Caffe_2"
    // ═══════════════════════════════════════════════════════════════
    [MenuItem("Petta/Clean Caffè")]
    static void CleanCaffe()
    {
        GameObject cafePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CAFE_REF_FBX);
        if (cafePrefab == null)
        {
            EditorUtility.DisplayDialog("Clean Caffè", "Petta Café reference FBX not found!\n" + CAFE_REF_FBX, "OK");
            return;
        }

        // Instantiate temporarily to analyze
        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(cafePrefab);
        temp.transform.position = Vector3.zero;
        temp.transform.rotation = Quaternion.identity;
        temp.transform.localScale = Vector3.one;

        MeshFilter[] meshFilters = temp.GetComponentsInChildren<MeshFilter>();
        Renderer[] renderers = temp.GetComponentsInChildren<Renderer>();
        Material sourceMaterial = null;
        if (renderers.Length > 0 && renderers[0].sharedMaterial != null)
            sourceMaterial = renderers[0].sharedMaterial;

        // Get full model bounds
        Bounds fullBounds = GetCompositeBounds(temp);
        float modelHeight = fullBounds.size.y;
        float modelWidth  = fullBounds.size.x;
        float modelDepth  = fullBounds.size.z;

        Debug.Log($"Clean Caffè — model bounds: center={fullBounds.center}, size={fullBounds.size}");
        Debug.Log($"  Model dimensions: W={modelWidth:F3} H={modelHeight:F3} D={modelDepth:F3}");
        Debug.Log($"  Bounds min={fullBounds.min} max={fullBounds.max}");
        Debug.Log($"  MeshFilters found: {meshFilters.Length}");

        List<Mesh> cleanedMeshes = new List<Mesh>();
        int totalRemoved = 0;
        int totalKept = 0;

        foreach (var mf in meshFilters)
        {
            Mesh sourceMesh = mf.sharedMesh;
            if (sourceMesh == null) continue;
            Transform t = mf.transform;

            CleanResult result = RemoveFurnitureFromMesh(sourceMesh, t, fullBounds);
            if (result.cleanedMesh != null && result.cleanedMesh.vertexCount > 0)
            {
                cleanedMeshes.Add(result.cleanedMesh);
            }
            totalRemoved += result.removedTriangles;
            totalKept += result.keptTriangles;
        }

        Object.DestroyImmediate(temp);

        if (cleanedMeshes.Count == 0)
        {
            EditorUtility.DisplayDialog("Clean Caffè", "No mesh data could be processed.", "OK");
            return;
        }

        // Combine into final mesh
        Mesh caffe2Mesh = CombineMeshes(cleanedMeshes);
        caffe2Mesh.name = "Caffe_2_Cleaned";

        // Save as asset
        string meshAssetPath = "Assets/Editor/Caffe_2_Mesh.asset";
        AssetDatabase.CreateAsset(caffe2Mesh, meshAssetPath);
        AssetDatabase.SaveAssets();

        // Create a prefab-like GameObject in the scene
        GameObject caffe2 = new GameObject("Caffe_2");
        Undo.RegisterCreatedObjectUndo(caffe2, "Clean Caffè");
        MeshFilter caffe2MF = caffe2.AddComponent<MeshFilter>();
        caffe2MF.sharedMesh = caffe2Mesh;
        MeshRenderer caffe2MR = caffe2.AddComponent<MeshRenderer>();
        if (sourceMaterial != null)
            caffe2MR.sharedMaterial = sourceMaterial;

        // Place it next to origin
        caffe2.transform.position = Vector3.zero;

        Selection.activeGameObject = caffe2;

        string msg = $"Caffe_2 created!\n\n" +
                     $"Triangles kept: {totalKept}\n" +
                     $"Triangles removed (furniture): {totalRemoved}\n" +
                     $"Mesh saved: {meshAssetPath}";
        Debug.Log("✓ " + msg);
        EditorUtility.DisplayDialog("Clean Caffè", msg, "OK");
    }

    struct CleanResult
    {
        public Mesh cleanedMesh;
        public int removedTriangles;
        public int keptTriangles;
    }

    /// <summary>
    /// Spatial approach: remove triangles whose centroid falls in the interior
    /// "furniture zone" — between floor and ~45% height, not touching walls.
    /// The mesh is continuous so island detection doesn't work.
    /// </summary>
    static CleanResult RemoveFurnitureFromMesh(Mesh source, Transform transform, Bounds fullBounds)
    {
        Vector3[] verts = source.vertices;
        int[] tris = source.triangles;
        Vector2[] uvs = source.uv;
        Vector3[] normals = source.normals;
        int triCount = tris.Length / 3;

        if (triCount == 0)
            return new CleanResult { cleanedMesh = null, removedTriangles = 0, keptTriangles = 0 };

        float modelHeight = fullBounds.size.y;
        float modelWidth  = fullBounds.size.x;
        float modelDepth  = fullBounds.size.z;

        // Define the furniture zone in normalized coordinates (0..1 within bounds)
        // Furniture sits on the floor, in the interior, not touching walls
        float furnitureMinY = 0.02f;  // just above ground (skip floor itself)
        float furnitureMaxY = 0.45f;  // tables/stools don't go higher than ~45% of model
        float wallMarginX  = 0.18f;   // 18% inset from left/right walls
        float wallMarginZ  = 0.18f;   // 18% inset from front/back walls

        // Also check for normals: furniture surfaces tend to be horizontal (facing up)
        // while walls are vertical. We'll use this as a secondary filter.

        HashSet<int> trianglesToRemove = new HashSet<int>();

        for (int t = 0; t < triCount; t++)
        {
            // Get triangle centroid in world space
            Vector3 v0 = transform.TransformPoint(verts[tris[t * 3 + 0]]);
            Vector3 v1 = transform.TransformPoint(verts[tris[t * 3 + 1]]);
            Vector3 v2 = transform.TransformPoint(verts[tris[t * 3 + 2]]);
            Vector3 centroid = (v0 + v1 + v2) / 3f;

            // Normalize position within model bounds (0..1)
            float normY = (centroid.y - fullBounds.min.y) / modelHeight;
            float normX = (centroid.x - fullBounds.min.x) / modelWidth;
            float normZ = (centroid.z - fullBounds.min.z) / modelDepth;

            // Check if ALL three vertices are in the furniture height zone
            float normY0 = (v0.y - fullBounds.min.y) / modelHeight;
            float normY1 = (v1.y - fullBounds.min.y) / modelHeight;
            float normY2 = (v2.y - fullBounds.min.y) / modelHeight;

            bool allVertsInHeightRange = 
                normY0 >= furnitureMinY && normY0 <= furnitureMaxY &&
                normY1 >= furnitureMinY && normY1 <= furnitureMaxY &&
                normY2 >= furnitureMinY && normY2 <= furnitureMaxY;

            // Check if centroid is in interior (not near walls)
            bool isInteriorX = normX > wallMarginX && normX < (1f - wallMarginX);
            bool isInteriorZ = normZ > wallMarginZ && normZ < (1f - wallMarginZ);

            if (allVertsInHeightRange && isInteriorX && isInteriorZ)
            {
                trianglesToRemove.Add(t);
            }
        }

        Debug.Log($"  Spatial filter: {trianglesToRemove.Count} triangles marked as furniture out of {triCount} total");

        // ── Rebuild mesh without removed triangles ──
        List<int> keptTris = new List<int>();
        for (int t = 0; t < triCount; t++)
        {
            if (!trianglesToRemove.Contains(t))
            {
                keptTris.Add(tris[t * 3]);
                keptTris.Add(tris[t * 3 + 1]);
                keptTris.Add(tris[t * 3 + 2]);
            }
        }

        if (keptTris.Count == 0)
            return new CleanResult { cleanedMesh = null, removedTriangles = trianglesToRemove.Count, keptTriangles = 0 };

        // Compact vertex list
        Dictionary<int, int> remap = new Dictionary<int, int>();
        List<Vector3> newVerts = new List<Vector3>();
        List<Vector2> newUVs = new List<Vector2>();
        List<Vector3> newNormals = new List<Vector3>();

        for (int i = 0; i < keptTris.Count; i++)
        {
            int oldIdx = keptTris[i];
            if (!remap.ContainsKey(oldIdx))
            {
                remap[oldIdx] = newVerts.Count;
                newVerts.Add(verts[oldIdx]);
                newUVs.Add(uvs != null && oldIdx < uvs.Length ? uvs[oldIdx] : Vector2.zero);
                newNormals.Add(normals != null && oldIdx < normals.Length ? normals[oldIdx] : Vector3.up);
            }
            keptTris[i] = remap[oldIdx];
        }

        Mesh cleaned = new Mesh();
        cleaned.name = "Caffe_2_Part";
        cleaned.vertices = newVerts.ToArray();
        cleaned.uv = newUVs.ToArray();
        cleaned.normals = newNormals.ToArray();
        cleaned.triangles = keptTris.ToArray();
        cleaned.RecalculateBounds();

        return new CleanResult
        {
            cleanedMesh = cleaned,
            removedTriangles = trianglesToRemove.Count,
            keptTriangles = keptTris.Count / 3
        };
    }
}
