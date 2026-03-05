/*  ================================================================
 *  PlaceShopModel.cs  —  Editor-only
 *  Menu: Petta ▸ Place Shop Model
 *
 *  Takes a Meshy Bridge-imported model from MeshyImports/ and
 *  positions it at the PettaShop location.
 *
 *  ⚠  Does NOT modify FBX import settings — Bridge handles those.
 *  ================================================================ */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class PlaceShopModel
{
    [MenuItem("Petta/Place Shop Model")]
    public static void Place()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Errore",
                "Esci dal Play Mode prima!", "OK");
            return;
        }

        Debug.Log("[PlaceShop] ── START ──");

        // ── 1. Find newest FBX in MeshyImports ──
        string meshyDir = Path.Combine(Application.dataPath, "MeshyImports");
        if (!Directory.Exists(meshyDir))
        {
            EditorUtility.DisplayDialog("Errore",
                "Cartella Assets/MeshyImports/ non trovata!\n\n" +
                "Usa il Meshy Bridge (Window → Meshy) per importare un modello.", "OK");
            return;
        }

        string bestFbx = null;
        System.DateTime newest = System.DateTime.MinValue;
        foreach (var f in Directory.GetFiles(meshyDir, "*.fbx", SearchOption.AllDirectories))
        {
            var info = new FileInfo(f);
            if (info.LastWriteTime > newest)
            {
                newest = info.LastWriteTime;
                bestFbx = f;
            }
        }

        if (bestFbx == null)
        {
            EditorUtility.DisplayDialog("Errore",
                "Nessun FBX trovato in MeshyImports/!\n\n" +
                "Importa un modello con il Meshy Bridge.", "OK");
            return;
        }

        string assetPath = "Assets" + bestFbx.Substring(Application.dataPath.Length).Replace('\\', '/');
        Debug.Log($"[PlaceShop] Using: {assetPath}");

        // ── 2. Load prefab (NO import settings changes) ──
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Errore",
                $"Impossibile caricare:\n{assetPath}", "OK");
            return;
        }

        // ── 3. Remove old shop objects ──
        var rome = GameObject.Find("Rome_Prati");
        Vector3 shopPos = new Vector3(35f, 0f, 5f);

        // Remove blockout
        RemoveObject("PettaShop", rome);
        // Remove previous model placement
        RemoveObject("PettaShop_Model", rome);

        // ── 4. Instantiate ──
        var shop = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        shop.name = "PettaShop_Model";
        shop.transform.position = Vector3.zero;
        shop.transform.rotation = Quaternion.identity;
        shop.transform.localScale = Vector3.one;

        // ── 5. Auto-scale to ~7 units tall ──
        Bounds raw = CalcBounds(shop);
        float targetH = 7f;
        if (raw.size.y > 0.001f)
        {
            float s = targetH / raw.size.y;
            shop.transform.localScale = Vector3.one * s;
        }

        // ── 6. Position — bottom at Y=0, at shop location ──
        Bounds scaled = CalcBounds(shop);
        float yOff = -scaled.min.y;
        shop.transform.position = shopPos + Vector3.up * yOff;

        // Parent under Rome_Prati
        if (rome != null)
            shop.transform.SetParent(rome.transform, true);

        // ── 7. Enable all renderers, log material status ──
        var renderers = shop.GetComponentsInChildren<Renderer>(true);
        int textured = 0;
        foreach (var r in renderers)
        {
            r.enabled = true;
            foreach (var m in r.sharedMaterials)
            {
                if (m != null)
                {
                    var tex = m.GetTexture("_BaseMap") ?? m.GetTexture("_MainTex");
                    Debug.Log($"[PlaceShop]   {r.name}: shader={m.shader.name}, " +
                              $"baseMap={tex?.name ?? "NONE"}");
                    if (tex != null) textured++;
                }
            }
        }

        // ── 8. Add collider ──
        foreach (var c in shop.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);
        Bounds fb = CalcBounds(shop);
        var box = shop.AddComponent<BoxCollider>();
        box.center = shop.transform.InverseTransformPoint(fb.center);
        box.size = new Vector3(
            fb.size.x / shop.transform.lossyScale.x,
            fb.size.y / shop.transform.lossyScale.y,
            fb.size.z / shop.transform.lossyScale.z);

        // ── 9. Static ──
        foreach (var t in shop.GetComponentsInChildren<Transform>())
            t.gameObject.isStatic = true;

        // ── 10. Select & frame ──
        Selection.activeGameObject = shop;
        SceneView.lastActiveSceneView?.FrameSelected();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        string msg = textured > 0
            ? $"Modello piazzato con texture!\nPos: {shop.transform.position}"
            : $"Modello piazzato ma SENZA texture.\nPos: {shop.transform.position}\n\n" +
              "Controlla il Meshy Bridge import.";

        Debug.Log($"<color=green><b>[PlaceShop] ✅ DONE — {shop.name} at {shop.transform.position}</b></color>");
        EditorUtility.DisplayDialog("Negozio Piazzato!", msg, "OK");
    }

    static void RemoveObject(string name, GameObject parent)
    {
        GameObject obj = null;
        if (parent != null)
        {
            var t = parent.transform.Find(name);
            if (t != null) obj = t.gameObject;
        }
        if (obj == null) obj = GameObject.Find(name);
        if (obj != null)
        {
            Object.DestroyImmediate(obj);
            Debug.Log($"[PlaceShop] Removed: {name}");
        }
    }

    static Bounds CalcBounds(GameObject go)
    {
        var rr = go.GetComponentsInChildren<Renderer>(true);
        if (rr.Length > 0)
        {
            Bounds b = rr[0].bounds;
            for (int i = 1; i < rr.Length; i++) b.Encapsulate(rr[i].bounds);
            if (b.size.magnitude > 0.001f) return b;
        }
        var mfs = go.GetComponentsInChildren<MeshFilter>(true);
        if (mfs.Length > 0)
        {
            Bounds b = new Bounds(go.transform.position, Vector3.zero);
            foreach (var mf in mfs)
            {
                if (mf.sharedMesh != null)
                {
                    var c = mf.transform.TransformPoint(mf.sharedMesh.bounds.center);
                    var s = Vector3.Scale(mf.sharedMesh.bounds.size, mf.transform.lossyScale);
                    b.Encapsulate(new Bounds(c, s));
                }
            }
            return b;
        }
        return new Bounds(go.transform.position, Vector3.zero);
    }
}
#endif
