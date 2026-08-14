using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

[InitializeOnLoad]
public class AutomatedSceneModifier
{
    static AutomatedSceneModifier()
    {
        EditorApplication.delayCall += ExecuteModification;
    }

    private static void ExecuteModification()
    {
        string markerPath = "Assets/Editor/AutomatedSceneModifierRun.txt";

        // Si ya ha sido ejecutado, no hacemos nada.
        if (System.IO.File.Exists(markerPath))
        {
            return;
        }

        // Revisamos si ya existe uno en la escena
        SpawnManager existingManager = Object.FindAnyObjectByType<SpawnManager>();
        if (existingManager != null)
        {
            Debug.Log("[AutomatedSceneModifier] Ya existe un SpawnManager en la escena, saltando creación.");
            System.IO.File.WriteAllText(markerPath, "Completado");
            return;
        }

        // 1. Crear el objeto contenedor
        GameObject managerObj = new GameObject("SpawnManager");
        managerObj.transform.position = Vector3.zero;

        // 2. Añadir los componentes vitales de red
        managerObj.AddComponent<NetworkObject>();
        SpawnManager spawnManagerComp = managerObj.AddComponent<SpawnManager>();

        // 3. Crear 3 Puntos de Aparición (Hijos)
        List<Transform> spawns = new List<Transform>();
        
        // Puntos formando un triángulo alrededor del centro
        Vector3[] posiciones = new Vector3[]
        {
            new Vector3(4f, 0f, 0f),   // Derecha
            new Vector3(-4f, 0f, 0f),  // Izquierda
            new Vector3(0f, 0f, -4f)   // Atrás
        };

        for (int i = 0; i < posiciones.Length; i++)
        {
            GameObject spawnObj = new GameObject($"Spawn_{i + 1}");
            spawnObj.transform.SetParent(managerObj.transform);
            spawnObj.transform.position = posiciones[i];
            
            // Hacemos que miren hacia el centro (donde podría haber una mesa o fogata)
            spawnObj.transform.LookAt(Vector3.zero);
            
            spawns.Add(spawnObj.transform);
        }

        // 4. Asignar la lista al script (saltando la protección private con SerializedObject)
        SerializedObject so = new SerializedObject(spawnManagerComp);
        SerializedProperty spawnPointsProp = so.FindProperty("spawnPoints");
        
        spawnPointsProp.ClearArray();
        for (int i = 0; i < spawns.Count; i++)
        {
            spawnPointsProp.InsertArrayElementAtIndex(i);
            spawnPointsProp.GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];
        }
        
        so.ApplyModifiedProperties();

        // 5. Guardar la escena activa mágicamente
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        // 6. Marcar como completado
        System.IO.File.WriteAllText(markerPath, "Completado");
        Debug.Log("[AutomatedSceneModifier] ¡SpawnManager creado y guardado en la escena con éxito!");
    }
}

