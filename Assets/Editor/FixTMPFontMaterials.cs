using UnityEngine;
using UnityEditor;

/// <summary>
/// Restaura los materiales de TextMeshPro (SDF / Fuentes) que fueron cambiados por error al shader URP Lit.
/// Menú: Tools > Restore TMP Font Materials
/// </summary>
public class FixTMPFontMaterials : Editor
{
    [MenuItem("Tools/Restore TMP Font Materials")]
    [InitializeOnLoadMethod]
    public static void RestoreFontMaterials()
    {
        Shader tmpDistanceField = Shader.Find("TextMeshPro/Distance Field");
        if (tmpDistanceField == null)
        {
            tmpDistanceField = Shader.Find("TextMeshPro/Mobile/Distance Field");
        }

        if (tmpDistanceField == null)
        {
            Debug.LogWarning("[FixTMPFontMaterials] No se encontró el shader TextMeshPro/Distance Field.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int restoredCount = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                string matName = mat.name.ToLower();
                string lowerPath = path.ToLower();

                // Detectar si es un material de fuente o TextMeshPro (SDF, Font, TMPro, etc.)
                bool isTMPMaterial = matName.Contains("sdf") || 
                                     matName.Contains("font") || 
                                     matName.Contains("tmp") || 
                                     lowerPath.Contains("sdf") || 
                                     lowerPath.Contains("font") || 
                                     lowerPath.Contains("tmpro") || 
                                     lowerPath.Contains("textmesh pro");

                if (isTMPMaterial)
                {
                    // Si el shader actual es de URP Lit o Standard (incompatible con TMP), lo restauramos a TMP Distance Field
                    if (mat.shader != null && (mat.shader.name.StartsWith("Universal Render Pipeline/") || mat.shader.name == "Standard"))
                    {
                        mat.shader = tmpDistanceField;
                        EditorUtility.SetDirty(mat);
                        restoredCount++;
                        Debug.Log($"[FixTMPFontMaterials] 🛠️ Restaurado shader de TextMeshPro en: {mat.name} ({path}) -> {tmpDistanceField.name}");
                    }
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        if (restoredCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[FixTMPFontMaterials] ✅ Se restauraron {restoredCount} materiales de fuente TextMeshPro correctamente.");
        }
    }
}
