#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Fija el Root Transform Position Y de todas las animaciones FBX
/// en Assets/Animations/ para que los personajes NO se levanten al
/// reproducir clips de sentado. Configura "Based Upon: Original" en Y.
/// </summary>
public class FixAnimationRootMotion : EditorWindow
{
    [MenuItem("Tools/Herramientas Lobo/Fijar Root Motion Animaciones Sentado")]
    public static void FixAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:ModelImporter", new[] { "Assets/Animations" });
        int fixed_count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            bool changed = false;

            // Configura cada clip de animacion en el FBX
            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;

            foreach (var clip in clips)
            {
                // Root Transform Position Y: "Based Upon Original" = no desplaza el personaje en Y
                if (clip.lockRootPositionXZ != false || 
                    clip.keepOriginalPositionY != true ||
                    clip.heightFromFeet != false)
                {
                    clip.lockRootPositionXZ = false;   // No bloquea XZ (para que siga sentado)
                    clip.keepOriginalPositionY = true;  // Mantiene la Y original del clip
                    clip.heightFromFeet = false;        // NO calcular altura desde los pies
                    clip.lockRootHeightY = true;        // Bloquea el desplazamiento en Y
                    changed = true;
                }
            }

            if (changed)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
                fixed_count++;
                Debug.Log($"[FixRootMotion] Corregido: {Path.GetFileName(path)}");
            }
        }

        if (fixed_count == 0)
            Debug.Log("[FixRootMotion] Todos los FBX ya estaban correctamente configurados.");
        else
            Debug.Log($"[FixRootMotion] Listo. {fixed_count} FBX corregidos. Los personajes ya no se levantaran.");

        EditorUtility.DisplayDialog(
            "Fijar Root Motion - Completado",
            fixed_count == 0
                ? "Los FBX ya estaban correctamente configurados."
                : $"{fixed_count} animaciones corregidas.\nLos personajes NO se levantaran al reproducir clips de sentado.",
            "OK");
    }
}
#endif
