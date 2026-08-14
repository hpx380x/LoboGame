using UnityEngine;
using UnityEditor;
using Unity.Netcode;

[InitializeOnLoad]
public class AutomatedFinalFixes
{
    static AutomatedFinalFixes()
    {
        EditorApplication.delayCall += ExecuteFix;
    }

    private static void ExecuteFix()
    {
        string marker = "Assets/Editor/AutomatedFinalFixesRun.txt";
        if (System.IO.File.Exists(marker)) return;

        NetworkManager netManager = Object.FindAnyObjectByType<NetworkManager>();
        if (netManager != null)
        {
            // Forzar el auto-spawn a NULO limpiando el objeto seriado para que Unity guarde sí o sí a nivel inspector
            SerializedObject so = new SerializedObject(netManager);
            SerializedProperty netConfig = so.FindProperty("NetworkConfig");
            if (netConfig != null)
            {
                SerializedProperty playerPrefabProp = netConfig.FindPropertyRelative("PlayerPrefab");
                if (playerPrefabProp != null && playerPrefabProp.objectReferenceValue != null)
                {
                    playerPrefabProp.objectReferenceValue = null;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(netManager);
                    Debug.Log("[AutomatedFinalFixes] PlayerPrefab del NetworkManager ha sido destrozado desde la raíz serializada.");
                }
            }
        }

        System.IO.File.WriteAllText(marker, "Done");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }
}

