using UnityEditor;
using UnityEngine;
using UnityEditor.Formats.Fbx.Exporter;
using System.IO;
using System.Text;

public static class ExportAnimationsToFbx
{
    [MenuItem("LoboGame/Export All Animations to FBX")]
    public static void ExportAll()
    {
        string logPath = "Assets/fbx_export_run_log.txt";
        StringBuilder log = new StringBuilder();
        log.AppendLine("================ EXPORT FBX ANIMATIONS START ================");

        // Create folder for exports
        string exportDir = "Assets/AnimationsExported";
        if (!Directory.Exists(exportDir))
        {
            Directory.CreateDirectory(exportDir);
        }

        // Load PlayerArmature prefab
        string prefabPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (playerPrefab == null)
        {
            log.AppendLine("PlayerArmature prefab not found!");
            File.WriteAllText(logPath, log.ToString());
            return;
        }

        // List of animations to export (path, output name)
        var anims = new[]
        {
            new { path = "Assets/Scenes/axe_wood.anim", name = "player_talar" },
            new { path = "Assets/Scenes/barrer_animation.anim", name = "player_barrer" },
            new { path = "Assets/StarterAssets/ThirdPersonController/Character/Animations/regar.anim", name = "player_regar" },
            new { path = "Assets/StarterAssets/ThirdPersonController/Character/Animations/tableaction.anim", name = "player_tableaction" }
        };

        foreach (var anim in anims)
        {
            log.AppendLine($"\nProcessing: {anim.name} ({anim.path})");
            try
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(anim.path);
                if (clip == null)
                {
                    log.AppendLine($"  Warning: Animation clip not found at path: {anim.path}");
                    continue;
                }

                // Instantiate player prefab temporarily
                GameObject tempInstance = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
                if (tempInstance == null)
                {
                    log.AppendLine("  Error: Could not instantiate player prefab!");
                    continue;
                }

                // Position at origin
                tempInstance.transform.position = Vector3.zero;
                tempInstance.transform.rotation = Quaternion.identity;

                // Remove Light and Camera components to avoid Blender 5.0 importer bugs
                int lightsRemoved = 0;
                foreach (var light in tempInstance.GetComponentsInChildren<Light>(true))
                {
                    if (light != null && light.gameObject != tempInstance)
                    {
                        Object.DestroyImmediate(light.gameObject);
                        lightsRemoved++;
                    }
                }
                log.AppendLine($"  Removed light objects: {lightsRemoved}");

                int camsRemoved = 0;
                foreach (var cam in tempInstance.GetComponentsInChildren<Camera>(true))
                {
                    if (cam != null && cam.gameObject != tempInstance)
                    {
                        Object.DestroyImmediate(cam.gameObject);
                        camsRemoved++;
                    }
                }
                log.AppendLine($"  Removed camera objects: {camsRemoved}");

                Animator animator = tempInstance.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    // Create a temporary Animator Controller to guarantee the clip is exported
                    string tempControllerPath = $"Assets/temp_export_controller_{anim.name}.controller";
                    var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(tempControllerPath);
                    controller.AddLayer("Default");
                    var rootStateMachine = controller.layers[0].stateMachine;
                    var state = rootStateMachine.AddState(clip.name);
                    state.motion = clip;
                    rootStateMachine.defaultState = state;

                    animator.runtimeAnimatorController = controller;
                    log.AppendLine($"  Assigned temporary controller with clip: {clip.name}");
                }
                else
                {
                    log.AppendLine("  Warning: Animator not found on instance.");
                }

                // Export path
                string fbxFilePath = $"{exportDir}/{anim.name}.fbx";

                // Create export options for Binary FBX and include animation
                var exportOptions = new ExportModelOptions
                {
                    ExportFormat = ExportFormat.Binary,
                    ModelAnimIncludeOption = Include.ModelAndAnim,
                    AnimateSkinnedMesh = true
                };

                log.AppendLine($"  Starting FBX export to: {fbxFilePath}...");
                ModelExporter.ExportObject(fbxFilePath, tempInstance, exportOptions);
                log.AppendLine($"  Successfully exported to: {fbxFilePath}");

                // Clean up temporary instance
                Object.DestroyImmediate(tempInstance);
                
                // Clean up temporary controller
                string tempControllerPathCleanup = $"Assets/temp_export_controller_{anim.name}.controller";
                AssetDatabase.DeleteAsset(tempControllerPathCleanup);
            }
            catch (System.Exception ex)
            {
                log.AppendLine($"  Exception during export of {anim.name}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        log.AppendLine("================ EXPORT FBX ANIMATIONS END ================");
        File.WriteAllText(logPath, log.ToString());
        AssetDatabase.ImportAsset(logPath);
        Debug.Log("Export process finished. See Assets/fbx_export_run_log.txt for detailed log.");
    }
}
