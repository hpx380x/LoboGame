using UnityEngine;
using UnityEditor;

/// <summary>
/// Genera una aldea de test hecha con cubos sobre el TerrenoVerde.
/// Menu: Tools > Generate Test Village
/// </summary>
public class TestVillageGenerator : Editor
{
    static readonly Vector3 VILLAGE_CENTER = new Vector3(179f, 0f, 454f);
    static Material matWall, matRoof, matPath, matTower, matDoor, matFence;

    [MenuItem("Tools/Generate Test Village")]
    public static void Generate()
    {
        // Limpiar aldea anterior si existe
        GameObject old = GameObject.Find("TestVillage");
        if (old != null) DestroyImmediate(old);

        CreateMaterials();

        // Raiz de la aldea
        GameObject village = new GameObject("TestVillage");
        village.transform.position = VILLAGE_CENTER;
        Undo.RegisterCreatedObjectUndo(village, "Create Test Village");

        // 1. Plaza central
        MakeCube(village, "Plaza_Central", new Vector3(0, 0.05f, 0), new Vector3(18, 0.1f, 18), matPath);

        // 2. Caminos cardinales
        MakeCube(village, "Path_North", new Vector3(0, 0.05f,  22), new Vector3(4, 0.1f, 28), matPath);
        MakeCube(village, "Path_South", new Vector3(0, 0.05f, -22), new Vector3(4, 0.1f, 28), matPath);
        MakeCube(village, "Path_East",  new Vector3( 22, 0.05f, 0), new Vector3(28, 0.1f, 4), matPath);
        MakeCube(village, "Path_West",  new Vector3(-22, 0.05f, 0), new Vector3(28, 0.1f, 4), matPath);

        // 3. Hoguera central
        MakeCube(village, "Bonfire_Base", new Vector3(0, 0.35f, 0), new Vector3(1.5f, 0.5f, 1.5f), matTower);
        MakeCube(village, "Bonfire_Logs", new Vector3(0, 0.9f,  0), new Vector3(0.8f, 0.9f, 0.8f), matDoor);

        // 4. Torre / Iglesia al norte de la plaza
        BuildTower(village, new Vector3(0, 0, 14));

        // 5. Ocho casas alrededor
        Vector3[] hpos = new Vector3[] {
            new Vector3(-14, 0,  14),
            new Vector3( 14, 0,  14),
            new Vector3(-18, 0,   0),
            new Vector3( 18, 0,   0),
            new Vector3(-14, 0, -14),
            new Vector3( 14, 0, -14),
            new Vector3( -6, 0, -18),
            new Vector3(  6, 0, -18),
        };
        float[] hrot = new float[] { 135, 45, 90, -90, -135, -45, 180, 180 };
        for (int i = 0; i < hpos.Length; i++)
            BuildHouse(village, "Casa_" + (i + 1), hpos[i], hrot[i]);

        // 6. Pozo
        BuildWell(village, new Vector3(5, 0, 5));

        // 7. Barda perimetral
        BuildFence(village);

        // 8. Faroles decorativos
        Vector3[] lfpos = new Vector3[] {
            new Vector3( 8, 0, 0), new Vector3(-8, 0, 0),
            new Vector3(0, 0,  8), new Vector3(0, 0, -8),
        };
        for (int i = 0; i < lfpos.Length; i++)
            BuildLamp(village, lfpos[i]);

        Selection.activeGameObject = village;
        SceneView.FrameLastActiveSceneView();
        Debug.Log("[TestVillageGenerator] Aldea de test generada.");
        EditorUtility.DisplayDialog("Hecho", "Aldea creada sobre TerrenoVerde.", "OK");
    }

    // ─── Constructores ────────────────────────────────────────────────────────

    static void BuildHouse(GameObject parent, string id, Vector3 lpos, float rotY)
    {
        GameObject h = new GameObject(id);
        h.transform.SetParent(parent.transform);
        h.transform.localPosition = lpos;
        h.transform.localEulerAngles = new Vector3(0, rotY, 0);

        MakeCube(h, "Walls",    new Vector3(0,    1.5f,     0), new Vector3(5,    3,    4),    matWall);
        MakeCube(h, "Door",     new Vector3(0,    0.9f, 2.05f), new Vector3(1,    1.8f, 0.2f), matDoor);
        MakeCube(h, "Win_L",    new Vector3(-1.5f, 1.8f, 2.05f), new Vector3(0.8f, 0.8f, 0.2f), matPath);
        MakeCube(h, "Win_R",    new Vector3( 1.5f, 1.8f, 2.05f), new Vector3(0.8f, 0.8f, 0.2f), matPath);
        MakeCube(h, "Roof",     new Vector3(0,    3.55f,    0), new Vector3(5.6f, 1.1f, 4.6f), matRoof);
        MakeCube(h, "Chimney",  new Vector3(1.5f, 4.3f, -0.8f), new Vector3(0.5f, 1.2f, 0.5f), matTower);
    }

    static void BuildTower(GameObject parent, Vector3 lpos)
    {
        GameObject t = new GameObject("Iglesia");
        t.transform.SetParent(parent.transform);
        t.transform.localPosition = lpos;

        MakeCube(t, "Body",   new Vector3(0,  2.5f, 0),    new Vector3(7,   5,   7),    matTower);
        MakeCube(t, "Tower",  new Vector3(0,  8f,   1f),   new Vector3(3,   6,   3),    matTower);
        MakeCube(t, "Spire",  new Vector3(0, 11.5f, 1f),   new Vector3(2, 1.5f,  2),    matRoof);
        MakeCube(t, "Bell",   new Vector3(0,  9.5f, 1f),   new Vector3(0.5f, 0.5f, 0.5f), matDoor);
        MakeCube(t, "Roof",   new Vector3(0,  5.4f, 0),    new Vector3(7.6f, 0.8f, 7.6f), matRoof);
        MakeCube(t, "Door",   new Vector3(0,  1.5f, 3.6f), new Vector3(1.5f, 3f,  0.2f), matDoor);
        MakeCube(t, "Cruz_V", new Vector3(0, 13.2f, 1f),   new Vector3(0.2f, 1.5f, 0.2f), matWall);
        MakeCube(t, "Cruz_H", new Vector3(0, 13.5f, 1f),   new Vector3(0.9f, 0.2f, 0.2f), matWall);
    }

    static void BuildWell(GameObject parent, Vector3 lpos)
    {
        GameObject w = new GameObject("Pozo");
        w.transform.SetParent(parent.transform);
        w.transform.localPosition = lpos;

        MakeCube(w, "Wall_N",  new Vector3( 0,    0.6f,  0.9f), new Vector3(2,   1.2f, 0.2f), matTower);
        MakeCube(w, "Wall_S",  new Vector3( 0,    0.6f, -0.9f), new Vector3(2,   1.2f, 0.2f), matTower);
        MakeCube(w, "Wall_E",  new Vector3( 0.9f, 0.6f,  0),    new Vector3(0.2f, 1.2f,  2),  matTower);
        MakeCube(w, "Wall_W",  new Vector3(-0.9f, 0.6f,  0),    new Vector3(0.2f, 1.2f,  2),  matTower);
        MakeCube(w, "Post_L",  new Vector3(-0.8f, 1.7f,  0),    new Vector3(0.2f, 1f,   0.2f), matDoor);
        MakeCube(w, "Post_R",  new Vector3( 0.8f, 1.7f,  0),    new Vector3(0.2f, 1f,   0.2f), matDoor);
        MakeCube(w, "Beam",    new Vector3( 0,    2.2f,  0),    new Vector3(1.8f, 0.2f, 0.2f), matDoor);
    }

    static void BuildFence(GameObject parent)
    {
        GameObject fence = new GameObject("Cerca");
        fence.transform.SetParent(parent.transform);
        fence.transform.localPosition = Vector3.zero;

        float radius = 32f;
        int   posts  = 24;

        for (int i = 0; i < posts; i++)
        {
            float a1 = (360f / posts) * i  * Mathf.Deg2Rad;
            float a2 = (360f / posts) * (i + 1) * Mathf.Deg2Rad;
            Vector3 p1 = new Vector3(Mathf.Sin(a1) * radius, 0, Mathf.Cos(a1) * radius);
            Vector3 p2 = new Vector3(Mathf.Sin(a2) * radius, 0, Mathf.Cos(a2) * radius);

            // Poste
            GameObject post = MakeCube(fence, "Post_" + i,
                new Vector3(p1.x, 0.7f, p1.z),
                new Vector3(0.3f, 1.4f, 0.3f), matFence);

            // Barra horizontal
            Vector3 mid = (p1 + p2) * 0.5f;
            mid.y = 0.85f;
            float railLen  = Vector3.Distance(p1, p2);
            float railAngle = Mathf.Atan2(p2.x - p1.x, p2.z - p1.z) * Mathf.Rad2Deg;
            GameObject rail = MakeCube(fence, "Rail_" + i,
                mid,
                new Vector3(0.15f, 0.2f, railLen), matFence);
            rail.transform.localEulerAngles = new Vector3(0, railAngle, 0);
        }
    }

    static void BuildLamp(GameObject parent, Vector3 lpos)
    {
        GameObject lamp = new GameObject("Farol");
        lamp.transform.SetParent(parent.transform);
        lamp.transform.localPosition = lpos;

        MakeCube(lamp, "Pole",  new Vector3(0, 1.5f, 0),  new Vector3(0.15f, 3f,    0.15f), matTower);
        MakeCube(lamp, "Cap",   new Vector3(0, 3.1f, 0),  new Vector3(0.4f,  0.3f,  0.4f),  matDoor);
        MakeCube(lamp, "Light", new Vector3(0, 2.85f, 0), new Vector3(0.25f, 0.25f, 0.25f), matPath);
    }

    // ─── Primitiva base ───────────────────────────────────────────────────────

    static GameObject MakeCube(GameObject parent, string id, Vector3 lpos, Vector3 size, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = id;
        go.isStatic = true;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = lpos;
        go.transform.localScale    = size;
        go.transform.localRotation = Quaternion.identity;
        if (mat != null)
            go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // ─── Materiales URP ───────────────────────────────────────────────────────

    static void CreateMaterials()
    {
        matWall  = GetOrCreateMat("Mat_Village_Wall",  new Color(0.90f, 0.78f, 0.60f));
        matRoof  = GetOrCreateMat("Mat_Village_Roof",  new Color(0.48f, 0.20f, 0.10f));
        matPath  = GetOrCreateMat("Mat_Village_Path",  new Color(0.60f, 0.60f, 0.58f));
        matTower = GetOrCreateMat("Mat_Village_Tower", new Color(0.40f, 0.38f, 0.35f));
        matDoor  = GetOrCreateMat("Mat_Village_Door",  new Color(0.28f, 0.18f, 0.09f));
        matFence = GetOrCreateMat("Mat_Village_Fence", new Color(0.55f, 0.35f, 0.15f));
    }

    static Material GetOrCreateMat(string matName, Color color)
    {
        string folder = "Assets/Materials/Village";
        string path   = folder + "/" + matName + ".mat";

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Materials", "Village");

        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");

        Material mat = new Material(sh);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color",     color);
        mat.SetFloat("_Smoothness", 0.1f);
        mat.SetFloat("_Metallic",   0f);

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
