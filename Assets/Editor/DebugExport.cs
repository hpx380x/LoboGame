using UnityEditor;
using UnityEngine;
using System.IO;

public static class DebugExport
{
    [MenuItem("LoboGame/Debug Export")]
    public static void Run()
    {
        string logPath = "Assets/export_log.txt";
        using (StreamWriter writer = new StreamWriter(logPath, false))
        {
            writer.WriteLine("Starting debug export...");

            string prefabPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            writer.WriteLine($"PlayerArmature Prefab found: {playerPrefab != null}");

            var anims = new[]
            {
                new { path = "Assets/Scenes/axe_wood.anim", name = "player_talar" },
                new { path = "Assets/Scenes/barrer_animation.anim", name = "player_barrer" },
                new { path = "Assets/StarterAssets/ThirdPersonController/Character/Animations/regar.anim", name = "player_regar" },
                new { path = "Assets/StarterAssets/ThirdPersonController/Character/Animations/tableaction.anim", name = "player_tableaction" }
            };

            foreach (var anim in anims)
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(anim.path);
                writer.WriteLine($"Clip at {anim.path}: {clip != null}");
            }
        }
        AssetDatabase.ImportAsset(logPath);
    }
}
