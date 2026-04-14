using UnityEngine;
using UnityEditor;
using StarterAssets;
using Unity.Netcode.Components;

public class RepairGameSystems : MonoBehaviour
{
    [MenuItem("Antigravity/Repair Gameplay Systems")]
    public static void Repair()
    {
        // 1. Fix PlayerArmature Prefab
        string prefabPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        
        // Add CinemachineCameraTarget
        Transform cameraTarget = root.transform.Find("CinemachineCameraTarget");
        if (cameraTarget == null)
        {
            GameObject camTargetObj = new GameObject("CinemachineCameraTarget");
            camTargetObj.transform.SetParent(root.transform);
            camTargetObj.transform.localPosition = new Vector3(0, 1.4f, 0); // Approx head position
            cameraTarget = camTargetObj.transform;
            Debug.Log("Added CinemachineCameraTarget.");
        }

        // Repair ThirdPersonController reference
        ThirdPersonController tpc = root.GetComponent<ThirdPersonController>();
        if (tpc != null)
        {
            tpc.CinemachineCameraTarget = cameraTarget.gameObject;
            Debug.Log("Linked CinemachineCameraTarget to ThirdPersonController.");
        }

        // Repair Animator
        Animator anim = root.GetComponent<Animator>();
        if (anim != null)
        {
            // Avatar
            string fbxPath = "Assets/StarterAssets/ThirdPersonController/Character/Models/Armature.fbx";
            Avatar robotAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(fbxPath);
            if (robotAvatar != null)
            {
                anim.avatar = robotAvatar;
                Debug.Log("Restored Avatar to Animator.");
            }

            // Controller - CORRECTED PATH
            string ctrlPath = "Assets/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller";
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrlPath);
            if (ctrl != null)
            {
                anim.runtimeAnimatorController = ctrl;
                Debug.Log("Restored RuntimeAnimatorController: " + ctrl.name);
            }
            else
            {
                Debug.LogError("Could not find controller at: " + ctrlPath);
            }
        }

        // Repair NetworkAnimator
        NetworkAnimator netAnim = root.GetComponent<NetworkAnimator>();
        if (netAnim != null && anim != null)
        {
            SerializedObject so = new SerializedObject(netAnim);
            SerializedProperty prop = so.FindProperty("m_Animator");
            if (prop != null)
            {
                prop.objectReferenceValue = anim;
                so.ApplyModifiedProperties();
                Debug.Log("Repaired NetworkAnimator reference.");
            }
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("PlayerArmature Repaired Successfully!");

        // 2. Remove Duplicate AudioListener
        GameObject camaraEstudio = GameObject.Find("Camara_Estudio");
        if (camaraEstudio != null)
        {
            AudioListener audioListener = camaraEstudio.GetComponent<AudioListener>();
            if (audioListener != null)
            {
                Object.DestroyImmediate(audioListener);
                Debug.Log("Removed duplicate AudioListener from Camara_Estudio.");
            }
        }
        else
        {
            // Fallback for inactive objects
            AudioListener[] allListeners = Resources.FindObjectsOfTypeAll<AudioListener>();
            foreach (var listener in allListeners)
            {
                if (listener.gameObject.name == "Camara_Estudio")
                {
                    Object.DestroyImmediate(listener, true);
                    Debug.Log("Removed duplicate AudioListener from inactive Camara_Estudio.");
                    break;
                }
            }
        }
    }
}