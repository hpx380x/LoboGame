using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor Tool para arreglar materiales púrpuras/rotos de Polytope Studio en URP.
/// Menú: Tools > Fix Polytope Studio Materials
/// </summary>
public class FixPolytopeMaterials : Editor
{
    private const string FOLDER_PATH = "Assets/Polytope Studio";
    private const string URP_LIT_SHADER = "Universal Render Pipeline/Lit";

    [MenuItem("Tools/Fix Polytope Studio Materials")]
    public static void FixMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { FOLDER_PATH });

        Shader urpLitShader = Shader.Find(URP_LIT_SHADER);
        if (urpLitShader == null)
        {
            Debug.LogError("[FixPolytopeMaterials] ERROR: URP Lit shader no encontrado. Asegúrate de que URP esté instalado.");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Fix Polytope Studio Materials",
            $"Se encontraron {guids.Length} materiales en Polytope Studio.\n\n" +
            "Se convertirán los materiales al shader URP Lit y se reasignarán las texturas (_BaseTexture / _MainTex -> _BaseMap).\n\n¿Continuar?",
            "Sí, convertir", "Cancelar");

        if (!confirm) return;

        int converted = 0, alreadyURP = 0;
        AssetDatabase.StartAssetEditing();

        try
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                string shaderName = mat.shader != null ? mat.shader.name : "";

                // Intentar recuperar texturas desde varias propiedades conocidas de Polytope Studio
                Texture mainTex = null;
                if (mat.HasProperty("_BaseTexture") && mat.GetTexture("_BaseTexture") != null)
                    mainTex = mat.GetTexture("_BaseTexture");
                else if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
                    mainTex = mat.GetTexture("_MainTex");
                else if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null)
                    mainTex = mat.GetTexture("_BaseMap");

                // Colores
                Color baseColor = Color.white;
                if (mat.HasProperty("_Color")) baseColor = mat.GetColor("_Color");
                else if (mat.HasProperty("_TopColor")) baseColor = mat.GetColor("_TopColor");
                else if (mat.HasProperty("_BaseColor")) baseColor = mat.GetColor("_BaseColor");

                // Bump / Normal
                Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;

                // Cambiar Shader a URP Lit
                mat.shader = urpLitShader;

                // Reasignar propiedades URP Lit
                if (mainTex != null)
                {
                    mat.SetTexture("_BaseMap", mainTex);
                }
                
                mat.SetColor("_BaseColor", baseColor);

                if (bumpMap != null)
                {
                    mat.SetTexture("_BumpMap", bumpMap);
                    mat.EnableKeyword("_NORMALMAP");
                }

                // Ajustes de suavizado por defecto para estilo Lowpoly
                mat.SetFloat("_Smoothness", 0.1f);
                mat.SetFloat("_Metallic", 0.0f);

                EditorUtility.SetDirty(mat);
                converted++;
                Debug.Log($"[FixPolytopeMaterials] ✅ {Path.GetFileName(path)} convertid@ a URP Lit");
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string result = $"CONVERSIÓN DE POLYTOPE STUDIO COMPLETA:\n• Convertidos/Arreglados: {converted}\n• Total: {guids.Length}";
        Debug.Log("[FixPolytopeMaterials] " + result);
        EditorUtility.DisplayDialog("Conversión completa", result, "OK");
    }
}
