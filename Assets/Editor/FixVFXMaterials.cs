using UnityEngine;
using UnityEditor;

/// <summary>
/// Restaura los shaders originales de Vefects Free Fire VFX y otros paquetes de efectos especiales.
/// Menú: Tools > Restore VFX Shaders (Fire, Smoke, Haze)
/// </summary>
public class FixVFXMaterials : Editor
{
    [MenuItem("Tools/Restore VFX Shaders (Fire, Smoke, Haze)")]
    [InitializeOnLoadMethod]
    public static void RestoreVFXShaders()
    {
        Shader fireShader = Shader.Find("Vefects/SH_Vefects_VFX_SRP_Fire_Flames_01");
        Shader hazeShader = Shader.Find("Vefects/SH_Vefects_VFX_SRP_Heat_Haze_01");
        Shader erosionShader = Shader.Find("Vefects/SH_Vefects_VFX_SRP_Particles_Erosion_01");
        Shader billboardShader = Shader.Find("Vefects/SH_Vefects_Extra_Billboard_01");
        Shader gridShader = Shader.Find("Vefects/SH_Vefects_Extra_Grid_01");

        int restored = 0;
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Vefects" });

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                string matName = mat.name;

                if (matName.Contains("Fire") && fireShader != null)
                {
                    mat.shader = fireShader;
                    restored++;
                }
                else if (matName.Contains("Heat_Haze") && hazeShader != null)
                {
                    mat.shader = hazeShader;
                    restored++;
                }
                else if ((matName.Contains("Smoke") || matName.Contains("Ashes") || matName.Contains("Dust") || matName.Contains("Glow")) && erosionShader != null)
                {
                    mat.shader = erosionShader;
                    restored++;
                }
                else if (matName.Contains("Billboard") && billboardShader != null)
                {
                    mat.shader = billboardShader;
                    restored++;
                }
                else if (matName.Contains("Grid") && gridShader != null)
                {
                    mat.shader = gridShader;
                    restored++;
                }

                EditorUtility.SetDirty(mat);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        if (restored > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[FixVFXMaterials] ✅ Se restauraron los {restored} materiales de VFX/Fuego de Vefects a sus Shaders SRP originales.");
        }
    }
}
