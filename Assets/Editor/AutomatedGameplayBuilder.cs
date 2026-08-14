using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections.Generic;

[InitializeOnLoad]
public class AutomatedGameplayBuilder
{
    static AutomatedGameplayBuilder()
    {
        EditorApplication.delayCall += Execute;
    }

    private static void Execute()
    {
        string marker = "Assets/Editor/AutomatedGameplayBuilderRun.txt";
        if (System.IO.File.Exists(marker)) return;

        string menuGuid = "";
        string gameplayGuid = "";

        string[] mGuids = AssetDatabase.FindAssets("Scene_Menu t:Scene");
        if (mGuids.Length > 0) menuGuid = mGuids[0];

        string[] gGuids = AssetDatabase.FindAssets("Scene_Gameplay t:Scene");
        if (gGuids.Length > 0) gameplayGuid = gGuids[0];

        if (string.IsNullOrEmpty(gameplayGuid) || string.IsNullOrEmpty(menuGuid))
        {
            Debug.LogError("[AutomatedGameplayBuilder] No se encontraron las escenas 'Scene_Menu' o 'Scene_Gameplay'. ¿Te aseguraste de nombrarlas exactamente así?");
            return;
        }

        string gameplayPath = AssetDatabase.GUIDToAssetPath(gameplayGuid);
        string menuPath = AssetDatabase.GUIDToAssetPath(menuGuid);

        EditorSceneManager.SaveOpenScenes();
        Scene gameplayScene = EditorSceneManager.OpenScene(gameplayPath, OpenSceneMode.Single);

        bool changed = false;

        if (GameObject.Find("TerrenoVerde") == null)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "TerrenoVerde";
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(5, 1, 5); // 50x50m
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0.5f, 0.2f);
            plane.GetComponent<MeshRenderer>().sharedMaterial = mat;
            changed = true;
        }

        if (Object.FindAnyObjectByType<Light>() == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            changed = true;
        }

        if (GameObject.Find("MainCamera") == null)
        {
            string camPrefabPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/MainCamera.prefab";
            GameObject camPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(camPrefabPath);
            if (camPrefab) { PrefabUtility.InstantiatePrefab(camPrefab); changed = true; }
        }
        
        if (GameObject.Find("PlayerFollowCamera") == null)
        {
            string followCamPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerFollowCamera.prefab";
            GameObject followCamPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(followCamPath);
            if(followCamPrefab) { PrefabUtility.InstantiatePrefab(followCamPrefab); changed = true; }
        }

        if (Object.FindAnyObjectByType<SpawnManager>() == null)
        {
            GameObject smObj = new GameObject("SpawnManager");
            smObj.AddComponent<NetworkObject>();
            SpawnManager sm = smObj.AddComponent<SpawnManager>();

            Vector3[] pos = { new Vector3(4,0,0), new Vector3(-4,0,0), new Vector3(0,0,-4) };
            List<Transform> sps = new List<Transform>();
            for(int i=0; i<3; i++) {
                GameObject s = new GameObject($"Spawn_{i+1}");
                s.transform.SetParent(smObj.transform);
                s.transform.position = pos[i];
                s.transform.LookAt(Vector3.zero);
                sps.Add(s.transform);
            }
            SerializedObject so = new SerializedObject(sm);
            SerializedProperty sProp = so.FindProperty("spawnPoints");
            sProp.ClearArray();
            for(int i=0; i<3; i++) {
                sProp.InsertArrayElementAtIndex(i);
                sProp.GetArrayElementAtIndex(i).objectReferenceValue = sps[i];
            }
            so.ApplyModifiedProperties();
            changed = true;
        }

        if (changed) {
            EditorSceneManager.MarkSceneDirty(gameplayScene);
            EditorSceneManager.SaveOpenScenes();
        }

        System.IO.File.WriteAllText(marker, "Done");
        EditorSceneManager.OpenScene(menuPath, OpenSceneMode.Single);
        Debug.Log("<color=green>[AutomatedGameplayBuilder] ¡Tu mapa Scene_Gameplay ha sido construido mágicamente! (Suelo, luz, cámaras y SpawnManager listos). Y he devuelto tu Unity a la escena Scene_Menu de forma segura.</color>");
    }
}

