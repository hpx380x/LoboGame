using UnityEngine;
using UnityEditor;

public class RemoveMissingScriptsTool
{
    [MenuItem("Tools/Limpiar Scripts Faltantes (Missing Scripts)")]
    public static void CleanupMissingScripts()
    {
        int totalRemoved = 0;

        // 1. Limpiar objetos seleccionados en la escena
        GameObject[] seleccion = Selection.gameObjects;
        if (seleccion.Length > 0)
        {
            foreach (GameObject go in seleccion)
            {
                totalRemoved += CleanupGameObject(go);
            }
            Debug.Log($"[Limpieza] Limpiados {totalRemoved} scripts faltantes en los objetos seleccionados.");
        }
        else
        {
            // 2. Si no hay nada seleccionado, buscar en todos los prefabs del proyecto
            string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in prefabPaths)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    int removed = CleanupGameObject(prefab);
                    if (removed > 0)
                    {
                        totalRemoved += removed;
                        EditorUtility.SetDirty(prefab);
                        Debug.Log($"[Limpieza] Eliminados {removed} scripts rotos del prefab: {prefab.name}");
                    }
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Limpieza] Revisión de Prefabs terminada. Se eliminaron {totalRemoved} scripts faltantes en total.");
        }
    }

    private static int CleanupGameObject(GameObject go)
    {
        int count = 0;
        // Limpia el objeto y todos sus hijos recurrentemente
        count += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        foreach (Transform child in go.transform)
        {
            count += CleanupGameObject(child.gameObject);
        }
        return count;
    }
}
