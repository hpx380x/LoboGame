using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Herramienta Maestro para escanear y convertir los materiales no-URP de modelos 3D
/// de todo el proyecto Assets/ al Shader URP Lit.
/// Excluye efectos especiales (Vefects, FX), fuentes (TMP), agua e interfaz.
/// Menú: Tools > Fix ALL Purple Materials (Project Wide)
/// </summary>
public class FixAllProjectMaterials : Editor
{
    private const string URP_LIT = "Universal Render Pipeline/Lit";

    [MenuItem("Tools/Fix ALL Purple Materials (Project Wide)")]
    public static void FixAllMaterials()
    {
        Shader litShader = Shader.Find(URP_LIT);

        if (litShader == null)
        {
            Debug.LogError("[FixAllProjectMaterials] ERROR: URP Lit shader no encontrado. Asegúrate de tener URP instalado.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });

        bool confirm = EditorUtility.DisplayDialog(
            "Fix ALL Purple Materials",
            $"Se analizarán {guids.Length} materiales en todo el proyecto.\n\n" +
            "Se detectarán todos los materiales con Shaders antiguos (Built-in Standard, Legacy, etc.) y se convertirán a URP Lit.\n\n" +
            "Se excluirán automáticamente los efectos VFX (fuego, humo, partículas), fuentes TMP y shaders personalizados de agua.",
            "Sí, arreglar todo", "Cancelar");

        if (!confirm) return;

        int convertedLit = 0;
        int skipped = 0;

        AssetDatabase.StartAssetEditing();

        try
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string lowerPath = path.ToLower();

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                string matName = mat.name.ToLower();

                // 🛑 EXCLUSIONES ESTRICTAS: Fuentes, VFX, Fuego, Agua, Shaders especiales de paquetes
                if (lowerPath.Contains("textmesh pro") || lowerPath.Contains("tmpro") || lowerPath.Contains("fonts") || lowerPath.Contains("sdf") ||
                    lowerPath.Contains("vefects") || lowerPath.Contains("fx_materials") || lowerPath.Contains("ignitecoders") || lowerPath.Contains("n2studio") ||
                    matName.Contains("vfx") || matName.Contains("fire") || matName.Contains("smoke") || matName.Contains("water") || matName.Contains("sdf") || matName.Contains("font"))
                {
                    skipped++;
                    continue;
                }

                string shaderName = mat.shader != null ? mat.shader.name : "";

                // Si ya es un Shader URP, UI, Sprite o Shader Graph funcional, lo saltamos
                if (shaderName.StartsWith("Universal Render Pipeline") || 
                    shaderName.StartsWith("Shader Graphs") || 
                    shaderName.StartsWith("Vefects/") ||
                    shaderName.StartsWith("Nova/") ||
                    shaderName.StartsWith("GUI/") || 
                    shaderName.StartsWith("UI/") || 
                    shaderName.StartsWith("Sprites/") ||
                    shaderName.StartsWith("TextMeshPro/"))
                {
                    skipped++;
                    continue;
                }

                // Guardar texturas e información de color previa
                Texture mainTex = null;
                if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null) mainTex = mat.GetTexture("_BaseMap");
                else if (mat.HasProperty("_BaseTexture") && mat.GetTexture("_BaseTexture") != null) mainTex = mat.GetTexture("_BaseTexture");
                else if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null) mainTex = mat.GetTexture("_MainTex");

                Color baseColor = Color.white;
                if (mat.HasProperty("_BaseColor")) baseColor = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color")) baseColor = mat.GetColor("_Color");
                else if (mat.HasProperty("_TopColor")) baseColor = mat.GetColor("_TopColor");

                Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;

                mat.shader = litShader;
                if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                mat.SetColor("_BaseColor", baseColor);

                if (bumpMap != null)
                {
                    mat.SetTexture("_BumpMap", bumpMap);
                    mat.EnableKeyword("_NORMALMAP");
                }

                if (!mat.HasProperty("_Smoothness") || mat.GetFloat("_Smoothness") == 0)
                {
                    mat.SetFloat("_Smoothness", 0.1f);
                }

                convertedLit++;
                EditorUtility.SetDirty(mat);
                Debug.Log($"[FixAllProjectMaterials] ✅ Arreglado: {Path.GetFileName(path)} -> URP Lit");
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string summary = $"ARREGLO PROYECTO COMPLETO URP:\n" +
                         $"• Convertidos a URP Lit: {convertedLit}\n" +
                         $"• Omitidos/VFX/Fuentes/Especiales: {skipped}\n" +
                         $"• Total analizados: {guids.Length}";

        Debug.Log("[FixAllProjectMaterials] " + summary);
        EditorUtility.DisplayDialog("Arreglo URP Completo", summary, "OK");
    }
}
