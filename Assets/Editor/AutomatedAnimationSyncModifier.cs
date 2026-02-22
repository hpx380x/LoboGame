using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class AutomatedAnimationSyncModifier
{
    static AutomatedAnimationSyncModifier()
    {
        EditorApplication.delayCall += ExecuteModification;
    }

    private static void ExecuteModification()
    {
        string prefabPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
        string markerPath = "Assets/Editor/AutomatedAnimationSyncModifierRun.txt";

        if (System.IO.File.Exists(markerPath)) return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("[AutomatedAnimSync] No se encontró el Prefab en: " + prefabPath);
            return;
        }

        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject prefabRoot = editingScope.prefabContentsRoot;

            if (prefabRoot.GetComponent<PlayerAnimationSync>() == null)
            {
                prefabRoot.AddComponent<PlayerAnimationSync>();
                Debug.Log("[AutomatedAnimSync] Componente PlayerAnimationSync añadido con éxito al PlayerArmature.");
            }
        }

        System.IO.File.WriteAllText(markerPath, "Completado");
    }
}
