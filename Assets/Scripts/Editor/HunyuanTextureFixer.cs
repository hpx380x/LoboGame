// =============================================================
//  HERRAMIENTA: Hunyuan3D Texture Fixer
//  Asigna automáticamente texturas a modelos importados desde Hunyuan3D
//  Menú: Tools > Hunyuan3D Texture Fixer
// =============================================================
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class HunyuanTextureFixer : EditorWindow
{
    // ─── Variables de UI ───────────────────────────────────────────────────────
    private GameObject _targetObject;
    private string _searchFolder = "Assets";
    private bool _createNewMaterial = true;
    private bool _useEmbeddedMaterials = true;   // ← NUEVO: usar materiales del modelo
    private string _materialSavePath = "Assets/Materials/Hunyuan";
    private bool _autoDetectTextures = true;
    private Vector2 _scrollPos;

    // Log de resultados
    private List<string> _log = new List<string>();

    // Buscamos texturas por sus sufijos comunes exportados con Hunyuan3D
    private static readonly string[] ALBEDO_SUFFIXES   = { "_albedo", "_diffuse", "_color", "_basecolor", "_col", "_alb", "" };
    private static readonly string[] NORMAL_SUFFIXES   = { "_normal", "_nrm", "_nor", "_normalmap" };
    private static readonly string[] ROUGHNESS_SUFFIXES = { "_roughness", "_rough", "_rg", "_metalrough" };
    private static readonly string[] METAL_SUFFIXES    = { "_metallic", "_metal", "_met" };
    private static readonly string[] EMISSION_SUFFIXES = { "_emission", "_emissive", "_emit" };
    private static readonly string[] TEX_EXTENSIONS    = { ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".psd", ".exr" };

    // ─── Abrir ventana ─────────────────────────────────────────────────────────
    [MenuItem("Tools/Hunyuan3D Texture Fixer")]
    public static void ShowWindow()
    {
        var win = GetWindow<HunyuanTextureFixer>("Hunyuan Texture Fixer");
        win.minSize = new Vector2(420, 560);
    }

    // ─── INTERFAZ ─────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        DrawHeader();

        EditorGUILayout.Space(8);

        DrawObjectPicker();

        EditorGUILayout.Space(6);

        DrawOptions();

        EditorGUILayout.Space(10);

        DrawActionButtons();

        EditorGUILayout.Space(10);

        DrawLog();
    }

    private void DrawHeader()
    {
        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("⚙  Hunyuan3D Texture Fixer", headerStyle);

        var subStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        EditorGUILayout.LabelField("Asigna texturas automáticamente a modelos importados desde Hunyuan3D (URP).", subStyle);
        EditorGUILayout.LabelField("───────────────────────────────────────────────────", EditorStyles.centeredGreyMiniLabel);
    }

    private void DrawObjectPicker()
    {
        EditorGUILayout.LabelField("OBJETIVO", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _targetObject = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Modelo Hunyuan3D", "Arrastra aquí el modelo importado desde Hunyuan3D"),
                _targetObject, typeof(GameObject), true);

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Usar Selección de Escena", GUILayout.Height(22)))
                {
                    _targetObject = Selection.activeGameObject;
                    if (_targetObject == null)
                        AddLog("⚠  No hay ningún objeto seleccionado en la escena.");
                }
                if (GUILayout.Button("Limpiar", GUILayout.Width(70), GUILayout.Height(22)))
                    _targetObject = null;
            }
        }
    }

    private void DrawOptions()
    {
        EditorGUILayout.LabelField("OPCIONES", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            // ── NUEVO: opción para usar materiales embebidos ──
            _useEmbeddedMaterials = EditorGUILayout.Toggle(
                new GUIContent("Usar Materiales del Modelo", "[RECOMENDADO] Extrae y aplica los materiales que Hunyuan3D ya generó dentro del archivo OBJ/FBX importado"),
                _useEmbeddedMaterials);

            EditorGUILayout.Space(4);

            _autoDetectTextures = EditorGUILayout.Toggle(
                new GUIContent("Auto-detectar Texturas", "Busca texturas con el mismo nombre base que el modelo en todas las carpetas del proyecto"),
                _autoDetectTextures);

            EditorGUILayout.Space(4);

            _createNewMaterial = EditorGUILayout.Toggle(
                new GUIContent("Crear Material Nuevo", "Crea un material URP nuevo. Si está desactivado, intenta reutilizar el material existente del objeto"),
                _createNewMaterial);

            if (_createNewMaterial)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _materialSavePath = EditorGUILayout.TextField(
                        new GUIContent("Carpeta del Material", "Ruta donde se guardará el material creado"),
                        _materialSavePath);
                    if (GUILayout.Button("…", GUILayout.Width(26)))
                    {
                        string folder = EditorUtility.OpenFolderPanel("Elegir carpeta de materiales", Application.dataPath, "");
                        if (!string.IsNullOrEmpty(folder))
                        {
                            // Convertir a ruta relativa de Assets
                            if (folder.StartsWith(Application.dataPath))
                                _materialSavePath = "Assets" + folder.Substring(Application.dataPath.Length);
                        }
                    }
                }
            }

            EditorGUILayout.Space(4);

            _searchFolder = EditorGUILayout.TextField(
                new GUIContent("Carpeta de Búsqueda", "Carpeta donde buscar las texturas (normalmente Assets o la carpeta donde importaste el modelo)"),
                _searchFolder);
        }
    }

    private void DrawActionButtons()
    {
        var greenStyle = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 13
        };

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("▶  IMPORTAR Y ASIGNAR TEXTURAS", greenStyle, GUILayout.Height(38)))
            {
                _log.Clear();
                RunFixer();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Limpiar Log", GUILayout.Width(90), GUILayout.Height(38)))
                _log.Clear();
        }

        EditorGUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = new Color(0.8f, 0.9f, 1f);
            if (GUILayout.Button("🔎  Buscar Texturas Huérfanas en Proyecto", GUILayout.Height(28)))
            {
                _log.Clear();
                ScanProjectForHunyuanModels();
            }
            GUI.backgroundColor = Color.white;
        }
    }

    private void DrawLog()
    {
        EditorGUILayout.LabelField("REGISTRO", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinHeight(120)))
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
            if (_log.Count == 0)
            {
                EditorGUILayout.LabelField("Pulsa el botón para empezar...", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                foreach (var line in _log)
                    EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndScrollView();
        }
    }

    // ─── LÓGICA PRINCIPAL ─────────────────────────────────────────────────────

    private void RunFixer()
    {
        if (_targetObject == null)
        {
            AddLog("❌ No hay ningún objeto asignado. Arrastra un modelo al campo 'Modelo Hunyuan3D'.");
            return;
        }

        AddLog($"▶ Procesando: {_targetObject.name}");

        // Acumulamos todos los renderers del objeto y sus hijos
        var renderers = _targetObject.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            AddLog("❌ El objeto no tiene ningún Renderer. ¿Es un modelo 3D correcto?");
            return;
        }

        AddLog($"  Renderers encontrados: {renderers.Length}");

        // Paso 1: si está activado, extraer materiales embebidos del modelo fuente
        if (_useEmbeddedMaterials)
        {
            bool applied = TryApplyEmbeddedMaterials(_targetObject);
            if (applied)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                AddLog("✅ Materiales embebidos aplicados. Proceso completado.");
                return;
            }
            AddLog("ℹ  No se encontraron materiales embebidos. Continuando con búsqueda de texturas...");
        }

        // Paso 2: fallback → asignar por nombre de textura
        foreach (var rend in renderers)
        {
            ProcessRenderer(rend);
        }

        // Garantizar que Unity actualiza el proyecto
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AddLog("✅ ¡Proceso completado! Revisa el Inspector para ver los cambios.");
    }

    private void ProcessRenderer(Renderer rend)
    {
        AddLog($"\n  · Renderer: {rend.gameObject.name}");

        // Intentar obtener las texturas por el nombre del objeto o del mesh
        string baseName = rend.gameObject.name;
        if (rend is MeshRenderer mr && mr.GetComponent<MeshFilter>() is MeshFilter mf && mf.sharedMesh != null)
            baseName = mf.sharedMesh.name;

        // Limpiar sufijos numéricos que Unity añade (ej: "Mesh_0_0")
        baseName = SanitizeName(baseName);
        AddLog($"    Nombre base para búsqueda: '{baseName}'");

        // Buscar texturas
        Texture2D texAlbedo   = FindTexture(baseName, ALBEDO_SUFFIXES);
        Texture2D texNormal   = FindTexture(baseName, NORMAL_SUFFIXES);
        Texture2D texRoughness = FindTexture(baseName, ROUGHNESS_SUFFIXES);
        Texture2D texMetal    = FindTexture(baseName, METAL_SUFFIXES);
        Texture2D texEmission = FindTexture(baseName, EMISSION_SUFFIXES);

        LogTextureResult("Albedo",    texAlbedo);
        LogTextureResult("Normal",    texNormal);
        LogTextureResult("Roughness", texRoughness);
        LogTextureResult("Metal",     texMetal);
        LogTextureResult("Emission",  texEmission);

        if (texAlbedo == null && texNormal == null && texRoughness == null)
        {
            AddLog("    ⚠  No se encontraron texturas. Revisa la carpeta de búsqueda.");
            return;
        }

        // Crear o reutilizar material
        Material mat = _createNewMaterial ? CreateUrpMaterial(rend.gameObject.name) : GetOrCreateMaterial(rend);

        // Asignar texturas al material URP
        AssignTexturesToMaterial(mat, texAlbedo, texNormal, texRoughness, texMetal, texEmission);

        // Aplicar el material al renderer
        rend.sharedMaterial = mat;
        EditorUtility.SetDirty(rend);
        AddLog($"    ✅ Material asignado: {mat.name}");
    }

    // ─── MATERIALES EMBEBIDOS (Hunyuan3D OBJ/FBX) ────────────────────────────

    /// <summary>
    /// Busca el archivo fuente del modelo (OBJ, FBX) en el proyecto y extrae
    /// los materiales que Unity ya generó automáticamente al importarlo.
    /// Los aplica directamente a los Renderers del objeto en escena.
    /// </summary>
    private bool TryApplyEmbeddedMaterials(GameObject target)
    {
        // Buscamos el prefab/model fuente original del objeto en escena
        string sourcePath = FindModelAssetPath(target);

        if (string.IsNullOrEmpty(sourcePath))
        {
            AddLog("  ⚠  No se encontró el asset fuente (OBJ/FBX) del objeto en el proyecto.");
            return false;
        }

        AddLog($"  📂 Asset fuente: {sourcePath}");

        // Cargar TODOS los sub-assets del model (incluye materiales y meshes)
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(sourcePath);
        var embeddedMaterials = new Dictionary<string, Material>();

        foreach (var asset in allAssets)
        {
            if (asset is Material mat)
            {
                // Clave normalizada para comparar con los renderers
                embeddedMaterials[mat.name.ToLower()] = mat;
                AddLog($"    📦 Material embebido encontrado: '{mat.name}'");
            }
        }

        if (embeddedMaterials.Count == 0)
        {
            // Hunyuan a veces guarda los materiales en una carpeta hermana
            embeddedMaterials = SearchMaterialsNearModel(sourcePath);
        }

        if (embeddedMaterials.Count == 0)
            return false;

        // Aplicar los materiales a los Renderers
        var renderers = target.GetComponentsInChildren<Renderer>(true);
        int applied = 0;

        foreach (var rend in renderers)
        {
            // Intentamos hacer coincidir por nombre del GameObject o del material actual
            string rendName = rend.gameObject.name.ToLower();
            string matName  = rend.sharedMaterial != null ? rend.sharedMaterial.name.ToLower() : "";

            Material match = null;

            // Coincidencia exacta con el nombre del renderer
            if (embeddedMaterials.TryGetValue(rendName, out match) ||
                embeddedMaterials.TryGetValue(matName, out match))
            {
                rend.sharedMaterial = match;
                EditorUtility.SetDirty(rend);
                AddLog($"    ✅ '{rend.gameObject.name}' ← material '{match.name}'");
                applied++;
                continue;
            }

            // Coincidencia parcial: el nombre del renderer contiene el nombre del material
            foreach (var kv in embeddedMaterials)
            {
                if (rendName.Contains(kv.Key) || kv.Key.Contains(rendName))
                {
                    rend.sharedMaterial = kv.Value;
                    EditorUtility.SetDirty(rend);
                    AddLog($"    ✅ '{rend.gameObject.name}' ← material '{kv.Value.name}' (coincidencia parcial)");
                    applied++;
                    break;
                }
            }

            // Si solo hay UN material embebido, lo aplicamos a todos los renderers
            if (match == null && embeddedMaterials.Count == 1)
            {
                var singleMat = embeddedMaterials.Values.First();
                rend.sharedMaterial = singleMat;
                EditorUtility.SetDirty(rend);
                AddLog($"    ✅ '{rend.gameObject.name}' ← material único '{singleMat.name}'");
                applied++;
            }
        }

        AddLog($"  Total aplicados: {applied}/{renderers.Length}");
        return applied > 0;
    }

    /// <summary>
    /// Busca el asset fuente OBJ/FBX del que proviene el GameObject.
    /// Primero mira el prefab raíz; si no, hace una búsqueda por nombre en el proyecto.
    /// </summary>
    private string FindModelAssetPath(GameObject target)
    {
        // Obtener el prefab original (si el objeto viene de un prefab)
        GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(target);
        if (prefabSource != null)
        {
            string path = AssetDatabase.GetAssetPath(prefabSource);
            if (!string.IsNullOrEmpty(path)) return path;
        }

        // Fallback: buscar en el proyecto un modelo con el mismo nombre
        string[] modelExtensions = { ".obj", ".fbx", ".gltf", ".glb" };
        foreach (var ext in modelExtensions)
        {
            string[] guids = AssetDatabase.FindAssets($"{target.name} t:Model", new[] { _searchFolder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.ToLower().EndsWith(ext))
                    return path;
            }
        }

        // Búsqueda más amplia por nombre sin extensión
        string[] allGuids = AssetDatabase.FindAssets($"{target.name}", new[] { _searchFolder });
        foreach (var guid in allGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string lower = path.ToLower();
            if (lower.EndsWith(".obj") || lower.EndsWith(".fbx") ||
                lower.EndsWith(".gltf") || lower.EndsWith(".glb"))
                return path;
        }

        return null;
    }

    /// <summary>
    /// Hunyuan3D a veces exporta los materiales en una carpeta hermana al OBJ,
    /// por ejemplo: MiModelo.obj → MiModelo/Materials/Mat_X.mat
    /// Este método los busca en esa ubicación.
    /// </summary>
    private Dictionary<string, Material> SearchMaterialsNearModel(string modelPath)
    {
        var result = new Dictionary<string, Material>();
        string modelDir  = Path.GetDirectoryName(modelPath);
        string modelName = Path.GetFileNameWithoutExtension(modelPath);

        // Buscar en la misma carpeta y en subcarpetas inmediatas
        string[] searchDirs =
        {
            modelDir,
            Path.Combine(modelDir, modelName),
            Path.Combine(modelDir, "Materials"),
            Path.Combine(modelDir, modelName, "Materials")
        };

        foreach (var dir in searchDirs)
        {
            if (!Directory.Exists(dir)) continue;

            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { dir.Replace("\\", "/") });
            foreach (var guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    result[mat.name.ToLower()] = mat;
                    AddLog($"    📦 Material encontrado cerca del modelo: '{mat.name}'");
                }
            }
        }

        return result;
    }

    // ─── CREACIÓN DE MATERIAL URP ─────────────────────────────────────────────

    private Material CreateUrpMaterial(string objectName)
    {
        string sanitized = SanitizeName(objectName);
        string folderPath = _materialSavePath;

        // Crear la carpeta si no existe
        if (!Directory.Exists(Path.Combine(Application.dataPath, folderPath.Substring("Assets/".Length))))
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, folderPath.Substring("Assets/".Length)));
            AssetDatabase.Refresh();
        }

        string matPath = $"{folderPath}/Mat_Hunyuan_{sanitized}.mat";

        // Usamos el shader Lit de URP
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            AddLog("    ⚠  No se encontró el shader 'Universal Render Pipeline/Lit'. Usando Standard.");
            urpLit = Shader.Find("Standard");
        }

        Material mat = new Material(urpLit) { name = $"Mat_Hunyuan_{sanitized}" };
        AssetDatabase.CreateAsset(mat, matPath);
        AddLog($"    📦 Material creado en: {matPath}");
        return mat;
    }

    private Material GetOrCreateMaterial(Renderer rend)
    {
        if (rend.sharedMaterial != null && rend.sharedMaterial.name != "Default-Material")
            return rend.sharedMaterial;
        return CreateUrpMaterial(rend.gameObject.name);
    }

    private void AssignTexturesToMaterial(Material mat, Texture2D albedo, Texture2D normal, Texture2D roughness, Texture2D metal, Texture2D emission)
    {
        // Propiedades del shader URP Lit
        if (albedo != null)
        {
            mat.SetTexture("_BaseMap", albedo);
            mat.SetColor("_BaseColor", Color.white);
        }

        if (normal != null)
        {
            // Configurar la textura normal correctamente
            ConfigureNormalMap(normal);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (roughness != null)
        {
            // En URP Lit, la roughness/smoothness va en el canal A del MetallicGloss
            mat.SetTexture("_MetallicGlossMap", roughness);
            // El smoothness es inverso a la roughness (0=rugoso, 1=brillante)
            mat.SetFloat("_Smoothness", 0.5f);
        }

        if (metal != null)
        {
            mat.SetTexture("_MetallicGlossMap", metal);
        }

        if (emission != null)
        {
            mat.SetTexture("_EmissionMap", emission);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.white);
        }

        EditorUtility.SetDirty(mat);
    }

    // ─── BÚSQUEDA DE TEXTURAS ─────────────────────────────────────────────────

    private Texture2D FindTexture(string baseName, string[] suffixes)
    {
        foreach (string suffix in suffixes)
        {
            foreach (string ext in TEX_EXTENSIONS)
            {
                string searchName = $"{baseName}{suffix}";

                // Búsqueda por GUID en el AssetDatabase (más robusta)
                string[] guids = AssetDatabase.FindAssets($"t:Texture2D {searchName}", new[] { _searchFolder });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = Path.GetFileNameWithoutExtension(path).ToLower();
                    string targetName = searchName.ToLower();

                    if (fileName == targetName)
                    {
                        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    }
                }
            }
        }
        return null;
    }

    // ─── ESCANEO DE PROYECTO ──────────────────────────────────────────────────

    private void ScanProjectForHunyuanModels()
    {
        AddLog("🔎 Escaneando proyecto en busca de modelos sin material o textura...");
        
        // Buscar todos los objetos en la escena con renderers
        var allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        int count = 0;

        foreach (var rend in allRenderers)
        {
            bool hasValidMaterial = rend.sharedMaterial != null && 
                                    rend.sharedMaterial.name != "Default-Material" && 
                                    rend.sharedMaterial.name != "Material";
            bool hasTexture = hasValidMaterial && (rend.sharedMaterial.mainTexture != null);

            if (!hasValidMaterial || !hasTexture)
            {
                AddLog($"  ⚠  '{rend.gameObject.name}' — Sin {(!hasValidMaterial ? "material" : "textura")} asignada.");
                count++;
            }
        }

        if (count == 0)
            AddLog("✅ Todos los modelos en escena tienen material y textura asignados.");
        else
            AddLog($"\n  Total de objetos sin textura/material: {count}");
    }

    // ─── UTILIDADES ───────────────────────────────────────────────────────────

    private string SanitizeName(string name)
    {
        // Eliminar sufijos numéricos (Mesh_0, _001, etc.)
        name = name.Replace("_mesh", "").Replace("_Mesh", "");
        // Quitar caracteres inválidos para nombre de archivo
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");
        return name.Trim();
    }

    private void ConfigureNormalMap(Texture2D tex)
    {
        string path = AssetDatabase.GetAssetPath(tex);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            AssetDatabase.ImportAsset(path);
        }
    }

    private void AddLog(string msg)
    {
        _log.Add(msg);
        Repaint(); // Refrescar la ventana para ver el log en tiempo real
    }

    private void LogTextureResult(string label, Texture2D tex)
    {
        if (tex != null)
            AddLog($"    ✓ {label}: {tex.name}");
    }
}
