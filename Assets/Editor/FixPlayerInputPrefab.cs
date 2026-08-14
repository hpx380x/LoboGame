using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class FixPlayerInputPrefab
{
    [MenuItem("LoboGame/Fix Player Input Prefab")]
    public static void RunFix()
    {
        Debug.Log("================ FIX PLAYER INPUT PREFAB START ================");

        // Load the Input Actions asset
        string actionsPath = "Assets/StarterAssets/InputSystem/StarterAssets.inputactions";
        InputActionAsset actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(actionsPath);
        if (actionsAsset == null)
        {
            Debug.LogError($"Could not find InputActionAsset at path: {actionsPath}");
            Debug.Log("================ FIX PLAYER INPUT PREFAB END ================");
            return;
        }
        Debug.Log($"Loaded InputActionAsset: {actionsAsset.name}");

        // Load the PlayerArmature prefab
        string prefabPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"Could not load prefab at path: {prefabPath}");
            Debug.Log("================ FIX PLAYER INPUT PREFAB END ================");
            return;
        }

        // Get PlayerInput component
        if (prefabRoot.TryGetComponent<PlayerInput>(out var playerInput))
        {
            // Assign the actions asset
            playerInput.actions = actionsAsset;
            
            // Set default action map to "Player" if it exists
            var playerMap = actionsAsset.FindActionMap("Player");
            if (playerMap != null)
            {
                playerInput.defaultActionMap = "Player";
            }
            
            // Ensure UI input module is configured or behavior is SendMessages
            playerInput.notificationBehavior = PlayerNotifications.SendMessages;

            Debug.Log("Assigned actions asset to PlayerInput component on PlayerArmature prefab.");
            
            // Save the prefab
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Debug.Log("Saved PlayerArmature prefab successfully.");
        }
        else
        {
            Debug.LogError("PlayerInput component not found on PlayerArmature prefab root!");
        }

        // Unload prefab contents
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        // Do the same for PlayerArmatureLobby prefab if it exists and has PlayerInput
        string lobbyPrefabPath = "Assets/PlayerArmatureLobby.prefab";
        GameObject lobbyPrefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(lobbyPrefabPath) != null ? PrefabUtility.LoadPrefabContents(lobbyPrefabPath) : null;
        if (lobbyPrefabRoot != null)
        {
            if (lobbyPrefabRoot.TryGetComponent<PlayerInput>(out var lobbyPlayerInput))
            {
                lobbyPlayerInput.actions = actionsAsset;
                var playerMap = actionsAsset.FindActionMap("Player");
                if (playerMap != null)
                {
                    lobbyPlayerInput.defaultActionMap = "Player";
                }
                lobbyPlayerInput.notificationBehavior = PlayerNotifications.SendMessages;
                Debug.Log("Assigned actions asset to PlayerInput component on PlayerArmatureLobby prefab.");
                PrefabUtility.SaveAsPrefabAsset(lobbyPrefabRoot, lobbyPrefabPath);
                Debug.Log("Saved PlayerArmatureLobby prefab successfully.");
            }
            PrefabUtility.UnloadPrefabContents(lobbyPrefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("================ FIX PLAYER INPUT PREFAB END ================");
    }
}
