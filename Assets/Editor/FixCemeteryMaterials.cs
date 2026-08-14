using UnityEngine;
using UnityEditor;

public class FixCemeteryMaterials : MonoBehaviour
{
    [MenuItem("Tools/Herramientas Lobo/Fix Cemetery Materials")]
    public static void FixMaterials()
    {
        string[] matNames = { "MaterialCandle", "MaterialCrypt", "MaterialFloor", "MaterialTombstones", "MaterialWoodTile", "MetalObjects", "MeterialColumn" };

        int fixedCount = 0;

        foreach (string matName in matNames)
        {
            string[] matGuids = AssetDatabase.FindAssets(matName + " t:Material");
            if (matGuids.Length == 0) continue;
            string matPath = AssetDatabase.GUIDToAssetPath(matGuids[0]);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            if (mat != null)
            {
                // Apagamos la Emisi\u00f3n que los hace brillar en blanco puro
                if (matName != "MaterialCandle") // Tal vez la vela s\u00ed deba brillar, pero por si acaso, la apagamos tambi\u00e9n si est\u00e1 en blanco global
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                    mat.SetColor("_EmissionColor", Color.black);
                }
                
                // Nos aseguramos que el color base sea gris/blanco neutro pero no brillante
                mat.SetColor("_BaseColor", Color.white);
                
                EditorUtility.SetDirty(mat);
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Corregidos {fixedCount} materiales brillantes.");
    }
}
