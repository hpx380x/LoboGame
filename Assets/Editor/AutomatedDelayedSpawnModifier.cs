using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class AutomatedDelayedSpawnModifier
{
    static AutomatedDelayedSpawnModifier()
    {
        EditorApplication.delayCall += ExecuteModification;
    }

    private static void ExecuteModification()
    {
        string markerPath = "Assets/Editor/AutomatedDelayedSpawnRun.txt";
        
        // Evitamos bucles infinitos en futuras compilaciones
        if (System.IO.File.Exists(markerPath)) return;

        bool sceneDirty = false;

        // 1. Encontrar el NetworkManager y el GameManager
        NetworkManager netManager = Object.FindFirstObjectByType<NetworkManager>();
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();

        if (netManager != null && gameManager != null)
        {
            // Extraer el prefab del jugador actual (el azul)
            GameObject playerPrefab = netManager.NetworkConfig.PlayerPrefab;

            if (playerPrefab != null)
            {
                // A) Quitarle a NetworkManager el auto-spawn (Desvinculamos el PlayerPrefab central)
                netManager.NetworkConfig.PlayerPrefab = null;

                // B) Asegurarnos de que el prefab todavía existe en la lista general de NetworkPrefabs
                bool alreadyInList = netManager.NetworkConfig.Prefabs.Contains(playerPrefab);

                if (!alreadyInList)
                {
                    netManager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = playerPrefab });
                }

                Debug.Log("[AutomatedDelayedSpawn] Auto-aparición de jugadores desactivada con éxito en NetworkManager.");

                // C) Pasar ese prefab al GameManager para que él lo invoque manualmente
                SerializedObject so = new SerializedObject(gameManager);
                SerializedProperty playerPrefabProp = so.FindProperty("playerPrefab");
                if (playerPrefabProp != null)
                {
                    playerPrefabProp.objectReferenceValue = playerPrefab;
                    so.ApplyModifiedProperties();
                    Debug.Log("[AutomatedDelayedSpawn] Prefab transferido al GameManager con éxito.");
                }

                sceneDirty = true;
            }
            else
            {
                // Si NetworkManager ya no tiene playerPrefab, a lo mejor ya se transfirió manualmente
            }
        }
        else
        {
            Debug.LogWarning("[AutomatedDelayedSpawn] No se encontró NetworkManager o GameManager en la escena activa.");
        }

        // 2. Guardado seguro usando la API de Unity
        if (sceneDirty)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        // Marcar finalizado para no volver a entrar
        System.IO.File.WriteAllText(markerPath, "Completado");
    }
}
