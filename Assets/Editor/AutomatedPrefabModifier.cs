using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class AutomatedPrefabModifier
{
    static AutomatedPrefabModifier()
    {
        EditorApplication.delayCall += ExecuteModification;
    }

    private static void ExecuteModification()
    {
        string prefabPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
        string markerPath = "Assets/Editor/AutomatedPrefabModifierRun.txt"; // Un archivo marcador para no ejecutar esto dos veces.

        // Si ya lo ejecutamos, no hacer nada.
        if (System.IO.File.Exists(markerPath))
        {
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[AutomatedPrefabModifier] No se encontró el Prefab en: {prefabPath}");
            return;
        }

        // Modificando el Prefab usando PrefabUtility
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject prefabRoot = editingScope.prefabContentsRoot;

            // 1. Añadimos el componente si no existe
            var networkSetup = prefabRoot.GetComponent<NetworkPlayerSetup>();
            if (networkSetup == null)
            {
                networkSetup = prefabRoot.AddComponent<NetworkPlayerSetup>();
                Debug.Log("[AutomatedPrefabModifier] Componente NetworkPlayerSetup añadido al componente raíz.");
            }

            // 2. Buscamos el hijo 'PlayerCameraRoot'
            Transform cameraRoot = prefabRoot.transform.Find("PlayerCameraRoot");
            if (cameraRoot != null)
            {
                // Asignamos el cameraTarget reflectivamente usando SerializedObject para respetar el modificador private/SerializeField
                SerializedObject so = new SerializedObject(networkSetup);
                SerializedProperty cameraTargetProp = so.FindProperty("cameraTarget");
                
                if (cameraTargetProp != null)
                {
                    cameraTargetProp.objectReferenceValue = cameraRoot.gameObject;
                    so.ApplyModifiedProperties();
                    Debug.Log("[AutomatedPrefabModifier] 'PlayerCameraRoot' asignado a 'cameraTarget'.");
                }
                else
                {
                    Debug.LogError("[AutomatedPrefabModifier] No se encontró la propiedad 'cameraTarget' en NetworkPlayerSetup.");
                }
            }
            else
            {
                Debug.LogError("[AutomatedPrefabModifier] No se encontró un hijo llamado 'PlayerCameraRoot' en el Prefab.");
            }
        }

        // Crear el marcador para que no se vuelva a ejecutar automáticamente cada vez que se recompile
        System.IO.File.WriteAllText(markerPath, "Completado");
        Debug.Log("[AutomatedPrefabModifier] ¡Modificación del Prefab completada con éxito!");
    }
}
