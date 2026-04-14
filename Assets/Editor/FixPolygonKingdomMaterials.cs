using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor Tool para arreglar materiales púrpuras de PolygonFantasyKingdom en URP.
/// Menú: Tools > Fix Polygon Kingdom Materials
///       Tools > Fix Polygon Kingdom FX Materials
/// </summary>
public class FixPolygonKingdomMaterials : Editor
{
    private const string FOLDER_PATH      = "Assets/PolygonFantasyKingdom";
    private const string FX_FOLDER_PATH   = "Assets/PolygonFantasyKingdom/Materials/FX_Materials";
    private const string URP_LIT_SHADER   = "Universal Render Pipeline/Lit";

    // ─────────────────────────────────────────────────────────────────────────
    // HERRAMIENTA 1: Materiales principales (Standard → URP Lit)
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Fix Polygon Kingdom Materials")]
    public static void FixMainMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { FOLDER_PATH });

        Shader urpLitShader = Shader.Find(URP_LIT_SHADER);
        if (urpLitShader == null)
        {
            Debug.LogError("[FixPolygonKingdomMaterials] ERROR: URP Lit shader no encontrado.");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Fix Polygon Kingdom Materials",
            $"Se encontraron {guids.Length} materiales en PolygonFantasyKingdom.\n\n" +
            "Se convertirán del shader Standard (Built-in) al shader URP Lit.\n\n¿Continuar?",
            "Sí, convertir", "Cancelar");

        if (!confirm) return;

        int converted = 0, alreadyURP = 0;
        AssetDatabase.StartAssetEditing();

        try
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // Saltar la subcarpeta FX (se maneja por separado)
                if (path.Contains("/FX_Materials/")) continue;

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                string shaderName = mat.shader.name;
                if (shaderName.StartsWith("Universal Render Pipeline") ||
                    shaderName.StartsWith("Sprites/"))
                { alreadyURP++; continue; }

                // Guardar propiedades antes de cambiar shader
                Texture mainTex       = mat.HasProperty("_MainTex")       ? mat.GetTexture("_MainTex")       : null;
                Color   baseColor     = mat.HasProperty("_Color")         ? mat.GetColor("_Color")           : Color.white;
                float   metallic      = mat.HasProperty("_Metallic")      ? mat.GetFloat("_Metallic")        : 0f;
                float   smoothness    = mat.HasProperty("_Glossiness")    ? mat.GetFloat("_Glossiness")      : 0.2f;
                Texture bumpMap       = mat.HasProperty("_BumpMap")       ? mat.GetTexture("_BumpMap")       : null;
                Texture emissionMap   = mat.HasProperty("_EmissionMap")   ? mat.GetTexture("_EmissionMap")   : null;
                Color   emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor")   : Color.black;
                bool    wasEmissive   = mat.IsKeywordEnabled("_EMISSION");

                // Cambiar a URP Lit
                mat.shader = urpLitShader;

                if (mainTex != null)   mat.SetTexture("_BaseMap", mainTex);
                mat.SetColor("_BaseColor", baseColor);
                mat.SetFloat("_Metallic",   metallic);
                mat.SetFloat("_Smoothness", smoothness);

                if (bumpMap != null)
                {
                    mat.SetTexture("_BumpMap", bumpMap);
                    mat.EnableKeyword("_NORMALMAP");
                }
                if (wasEmissive && emissionMap != null)
                {
                    mat.SetTexture("_EmissionMap", emissionMap);
                    mat.SetColor("_EmissionColor", emissionColor);
                    mat.EnableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }

                EditorUtility.SetDirty(mat);
                converted++;
                Debug.Log($"[FixPolygonKingdomMaterials] ✅ {Path.GetFileName(path)} → URP Lit");
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string result = $"CONVERSIÓN COMPLETA:\n• Convertidos: {converted}\n• Ya eran URP: {alreadyURP}\n• Total: {guids.Length}";
        Debug.Log("[FixPolygonKingdomMaterials] " + result);
        EditorUtility.DisplayDialog("Conversión completa", result, "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HERRAMIENTA 2: Materiales FX (Partículas → URP Particles/Unlit|Lit)
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Fix Polygon Kingdom FX Materials")]
    public static void FixFXMaterials()
    {
        Shader particlesUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Shader particlesLit   = Shader.Find("Universal Render Pipeline/Particles/Lit");

        if (particlesUnlit == null || particlesLit == null)
        {
            Debug.LogError("[FixPolygonKingdomMaterials] ERROR: Shaders URP Particles no encontrados. " +
                           "Asegúrate de que URP esté instalado.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { FX_FOLDER_PATH });

        if (guids.Length == 0)
        {
            Debug.LogWarning("[FixPolygonKingdomMaterials] No se encontraron materiales FX.");
            return;
        }

        int converted = 0;
        AssetDatabase.StartAssetEditing();

        try
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                string matName = mat.name.ToLower();

                // Guardar textura y color antes de cambiar shader
                Texture baseTex   = mat.HasProperty("_BaseMap")  ? mat.GetTexture("_BaseMap")  : null;
                Texture mainTex   = mat.HasProperty("_MainTex")  ? mat.GetTexture("_MainTex")  : null;
                Color   baseColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
                Color   tintColor = mat.HasProperty("_Color")     ? mat.GetColor("_Color")     : Color.white;

                Texture finalTex  = baseTex != null ? baseTex : mainTex;
                Color   finalColor = (baseColor != Color.white) ? baseColor : tintColor;

                // Arrow (mesh sólido) → Particles/Lit
                // Todo lo demás (fuego, humo, llama, brillo) → Particles/Unlit
                bool useLit = matName.Contains("arrow");
                mat.shader  = useLit ? particlesLit : particlesUnlit;

                // Re-asignar textura
                if (finalTex != null) mat.SetTexture("_BaseMap", finalTex);
                mat.SetColor("_BaseColor", finalColor);

                // Superficie: Transparent
                mat.SetFloat("_Surface", 1f);

                // Fuego / llama / brillo / rayos de sol → Additive (src=One, dst=One)
                bool isAdditive = matName.Contains("fire")   || matName.Contains("flame") ||
                                  matName.Contains("glow")   || matName.Contains("sun")   ||
                                  matName.Contains("beam")   || matName.Contains("circle");

                if (isAdditive)
                {
                    // Blend: Additive
                    mat.SetFloat("_Blend",    4f);  // Additive enum
                    mat.SetFloat("_SrcBlend", 1f);  // One
                    mat.SetFloat("_DstBlend", 1f);  // One
                    mat.SetFloat("_ZWrite",   0f);
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.renderQueue = 3000;
                }
                else
                {
                    // Blend: Alpha (para humo y default)
                    mat.SetFloat("_Blend",    0f);  // Alpha
                    mat.SetFloat("_SrcBlend", 5f);  // SrcAlpha
                    mat.SetFloat("_DstBlend", 10f); // OneMinusSrcAlpha
                    mat.SetFloat("_ZWrite",   0f);
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.renderQueue = 3000;
                }

                // Limpiar keywords inválidas de Built-in que causan warnings
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_GLOSSYREFLECTIONS_OFF");
                mat.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");

                EditorUtility.SetDirty(mat);
                converted++;
                Debug.Log($"[FixFX] ✅ {mat.name} → {mat.shader.name} ({(isAdditive ? "Additive" : "Alpha")})");
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FixPolygonKingdomMaterials] FX COMPLETO: {converted}/{guids.Length} materiales convertidos a URP Particles.");
    }
}
