using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class ConvertMapToPrefab : Editor
{
    [MenuItem("Tools/Convert Map To Prefab and Add to Scene_Gameplay")]
    public static void Execute()
    {
        // 1. Abrir la escena del mapa original si no está abierta
        string sourceScenePath = "Assets/Models/Environment_Free.unity";
        var currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.path != sourceScenePath)
        {
            currentScene = EditorSceneManager.OpenScene(sourceScenePath);
        }

        // 2. Asegurar que existe la carpeta Assets/Prefabs
        if (!Directory.Exists("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // 3. Agrupar todo el entorno bajo un GameObject raíz "Map_Environment_Free"
        GameObject mapRoot = GameObject.Find("Map_Environment_Free");
        if (mapRoot == null)
        {
            mapRoot = new GameObject("Map_Environment_Free");
            GameObject[] rootObjects = currentScene.GetRootGameObjects();
            foreach (var go in rootObjects)
            {
                // Ignorar objetos de jugador/cámara si no forman parte del mapa
                if (go.name == "Map_Environment_Free" || go.name == "Player" || go.name == "Main Camera")
                    continue;

                go.transform.SetParent(mapRoot.transform);
            }
        }

        // 4. Guardar como Prefab en Assets/Prefabs/Map_Environment_Free.prefab
        string prefabPath = "Assets/Prefabs/Map_Environment_Free.prefab";
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(mapRoot, prefabPath);
        Debug.Log($"[ConvertMapToPrefab] ✅ Prefab creado con éxito en '{prefabPath}'");

        // 5. Abrir Scene_Gameplay
        string gameplayScenePath = "Assets/Scenes/Scene_Gameplay.unity";
        var gameplayScene = EditorSceneManager.OpenScene(gameplayScenePath);

        // 6. Eliminar instancia antigua si existía
        GameObject existingMap = GameObject.Find("Map_Environment_Free");
        if (existingMap != null)
        {
            DestroyImmediate(existingMap);
        }

        // 7. Instanciar el nuevo Prefab del mapa en Scene_Gameplay
        GameObject mapInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
        mapInstance.name = "Map_Environment_Free";

        // 8. Guardar la escena Gameplay
        EditorSceneManager.MarkSceneDirty(gameplayScene);
        EditorSceneManager.SaveScene(gameplayScene);

        Debug.Log($"[ConvertMapToPrefab] ✅ ¡Mapa colocado correctamente en '{gameplayScenePath}' y guardado!");
    }
}
