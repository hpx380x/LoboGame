using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using StarterAssets;

public static class CheckPlayerInput
{
    [MenuItem("LoboGame/Check Player Input")]
    public static void RunCheck()
    {
        Debug.Log("================ CHECK PLAYER INPUT START ================");
        
        // Find PlayerArmature(Clone)
        GameObject player = GameObject.Find("PlayerArmature(Clone)");
        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
        }
        if (player == null)
        {
            // Try by type
            var tpc = Object.FindAnyObjectByType<ThirdPersonController>();
            if (tpc != null) player = tpc.gameObject;
        }

        if (player == null)
        {
            Debug.LogError("No Player GameObject (PlayerArmature(Clone) or ThirdPersonController) found in the active scene!");
            Debug.Log("================ CHECK PLAYER INPUT END ================");
            return;
        }

        Debug.Log($"Found Player GameObject: {player.name} (Entity ID: {player.GetHashCode()})");
        Debug.Log($"Active Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        Debug.Log($"Editor isPlaying: {EditorApplication.isPlaying}");

        // Print all components
        Component[] components = player.GetComponents<Component>();
        Debug.Log($"Components on {player.name}:");
        foreach (var c in components)
        {
            if (c == null)
            {
                Debug.LogError("Found MISSING/NULL script component on Player!");
                continue;
            }
            Debug.Log($"- {c.GetType().Name} (enabled: {GetEnabledProperty(c)})");
        }

        // Inspect PlayerInput
        if (player.TryGetComponent<PlayerInput>(out var playerInput))
        {
            Debug.Log($"[PlayerInput] enabled: {playerInput.enabled}");
            Debug.Log($"[PlayerInput] actions Asset: {(playerInput.actions != null ? playerInput.actions.name : "NULL")}");
            if (playerInput.actions != null)
            {
                Debug.Log($"[PlayerInput] currentActionMap: {(playerInput.currentActionMap != null ? playerInput.currentActionMap.name : "NONE")}");
                Debug.Log($"[PlayerInput] defaultActionMap: {playerInput.defaultActionMap}");
                foreach (var map in playerInput.actions.actionMaps)
                {
                    Debug.Log($"- ActionMap: {map.name} (enabled: {map.enabled})");
                }
            }
            Debug.Log($"[PlayerInput] behavior: {playerInput.notificationBehavior}");
        }
        else
        {
            Debug.LogError("PlayerInput component is MISSING from the Player!");
        }

        // Inspect StarterAssetsInputs
        if (player.TryGetComponent<StarterAssetsInputs>(out var saInputs))
        {
            Debug.Log($"[StarterAssetsInputs] enabled: {saInputs.enabled}");
            Debug.Log($"[StarterAssetsInputs] isInputLocked: {saInputs.isInputLocked}");
            Debug.Log($"[StarterAssetsInputs] cursorLocked: {saInputs.cursorLocked}");
            Debug.Log($"[StarterAssetsInputs] cursorInputForLook: {saInputs.cursorInputForLook}");
            Debug.Log($"[StarterAssetsInputs] current move: {saInputs.move}, look: {saInputs.look}");
        }
        else
        {
            Debug.LogError("StarterAssetsInputs component is MISSING from the Player!");
        }

        // Inspect ThirdPersonController
        if (player.TryGetComponent<ThirdPersonController>(out var tpcController))
        {
            Debug.Log($"[ThirdPersonController] enabled: {tpcController.enabled}");
            Debug.Log($"[ThirdPersonController] MoveSpeed: {tpcController.MoveSpeed}, SprintSpeed: {tpcController.SprintSpeed}");
            Debug.Log($"[ThirdPersonController] Grounded: {tpcController.Grounded}");
        }
        else
        {
            Debug.LogError("ThirdPersonController component is MISSING from the Player!");
        }

        // Inspect CharacterController
        if (player.TryGetComponent<CharacterController>(out var cc))
        {
            Debug.Log($"[CharacterController] enabled: {cc.enabled}");
            Debug.Log($"[CharacterController] center: {cc.center}, bounds: {cc.bounds}");
        }
        else
        {
            Debug.LogError("CharacterController component is MISSING from the Player!");
        }

        // Inspect PlayerState
        if (player.TryGetComponent<PlayerState>(out var state))
        {
            Debug.Log($"[PlayerState] enabled: {state.enabled}");
            Debug.Log($"[PlayerState] isDead: {state.isDead.Value}");
            Debug.Log($"[PlayerState] isInputLocked: {state.isInputLocked}");
        }

        // Inspect EventSystem
        var eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            Debug.Log($"[EventSystem] Found: {eventSystem.name} (enabled: {eventSystem.enabled})");
        }
        else
        {
            Debug.LogWarning("[EventSystem] No EventSystem found in scene!");
        }

        Debug.Log("================ CHECK PLAYER INPUT END ================");
    }

    private static string GetEnabledProperty(Component c)
    {
        var prop = c.GetType().GetProperty("enabled");
        if (prop != null)
        {
            return prop.GetValue(c).ToString();
        }
        return "N/A";
    }
}
