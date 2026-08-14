using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;
using StarterAssets;
using UnityEngine.InputSystem;

public class NetworkPlayerSetup : NetworkBehaviour
{
    [Header("Referencias del Jugador")]
    [Tooltip("El 'PlayerCameraRoot' que está dentro de tu jugador (donde mira la cámara)")]
    [SerializeField] private GameObject cameraTarget;

    public override void OnNetworkSpawn()
    {
        // [NUEVO] Si estamos en el menú de Lobby, no ejecutamos la lógica de cámaras de gameplay
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Scene_Menu")
        {
            return;
        }

        // [Regla 3] Si no soy el dueño local de este personaje, apaga sus controles para que no lo mueva yo.
        if (!IsOwner && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            Debug.Log($"[Setup] Desactivando controles para clon {OwnerClientId}");
            if (TryGetComponent(out ThirdPersonController cloneTpc)) cloneTpc.enabled = false;
            if (TryGetComponent(out PlayerInput pi)) pi.enabled = false;
            return; 
        }

        // --- SOLO EL DUEÑO (LOCAL) LLEGA A ESTE PUNTO ---
        Debug.Log("[Setup] Configurando Jugador Local en la escena activa...");

        // [Fix] Aseguramos que la Main Camera de la escena actual tenga un CinemachineBrain
        GameObject mainCameraObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCameraObj != null)
        {
            if (!mainCameraObj.TryGetComponent<CinemachineBrain>(out var brain))
            {
                Debug.Log("[Setup] Añadiendo CinemachineBrain faltante a la Main Camera...");
                mainCameraObj.AddComponent<CinemachineBrain>();
            }
        }

        // [Fix] Reconexión universal con la Virtual Camera de la escena
        var virtualCamera = FindAnyObjectByType<CinemachineVirtualCamera>(); 
        
        // [Fix Auto-Cámara] Si no se asignó cameraTarget en el Inspector, buscarlo automáticamente
        // por nombre en la jerarquía del jugador (Unity Starter Assets lo llama "PlayerCameraRoot")
        if (cameraTarget == null)
        {
            Transform raiz = this.transform;
            foreach (Transform hijo in GetComponentsInChildren<Transform>(true))
            {
                if (hijo.name == "PlayerCameraRoot" || hijo.name == "CameraRoot" || hijo.name == "CameraTarget")
                {
                    cameraTarget = hijo.gameObject;
                    Debug.Log($"[Setup] ✅ CameraTarget encontrado automáticamente: '{hijo.name}'");
                    break;
                }
            }
        }

        if (virtualCamera != null)
        {
            if (cameraTarget != null)
            {
                virtualCamera.Follow = cameraTarget.transform;
                virtualCamera.LookAt = cameraTarget.transform;
                Debug.Log($"[Setup] Virtual Camera vinculada a '{cameraTarget.name}'.");
            }
            else 
            {
                // Fallback: Buscar el ThirdPersonController que tiene cinemachineCameraTarget
                var fallbackTpc = GetComponent<StarterAssets.ThirdPersonController>();
                if (fallbackTpc != null && fallbackTpc.CinemachineCameraTarget != null)
                {
                    virtualCamera.Follow = fallbackTpc.CinemachineCameraTarget.transform;
                    virtualCamera.LookAt = fallbackTpc.CinemachineCameraTarget.transform;
                    Debug.Log($"[Setup] Virtual Camera vinculada via TPC a '{fallbackTpc.CinemachineCameraTarget.name}'.");
                }
                else
                {
                    virtualCamera.Follow = this.transform;
                    virtualCamera.LookAt = this.transform;
                    Debug.LogWarning("[Setup] ⚠️ CameraTarget no encontrado. Asigna 'PlayerCameraRoot' en el Inspector de NetworkPlayerSetup.");
                }
            }
        }
        else
        {
            Debug.LogWarning("[Setup] No se encontró CinemachineVirtualCamera en esta escena. La cámara no seguirá al jugador.");
        }


        // [Fix] Aseguramos que el PlayerInput se conecte al ActionMap sin reinicios violentos
        if (TryGetComponent(out PlayerInput playerInput))
        {
            // [ANTI-AMNESIA] Inmunidad contra la pérdida de referencia en el Inspector
            if (playerInput.actions == null)
            {
                Debug.LogWarning("[Setup Anti-Amnesia] El PlayerInput perdió sus Actions en el Inspector. Recargando dinámicamente desde Resources...");
                playerInput.actions = Resources.Load<InputActionAsset>("StarterAssets");
                
                if (playerInput.actions == null)
                {
                    Debug.LogError("[Setup FATAL] ¡Fallo Anti-Amnesia! No se encontró el archivo 'StarterAssets.inputactions' dentro de una carpeta 'Resources'. El jugador quedará congelado.");
                }
            }

            playerInput.enabled = true;
            if (playerInput.actions != null)
            {
                playerInput.actions.Enable(); // Fuerza la activación de los controles internamente
            }
        }

        if (TryGetComponent(out ThirdPersonController ownerTpc))
        {
            ownerTpc.enabled = true;
        }

        if (TryGetComponent(out CharacterController cc))
        {
            cc.enabled = true;
        }

        if (TryGetComponent(out StarterAssetsInputs inputs))
        {
            inputs.cursorLocked = true;
            inputs.cursorInputForLook = true;
            inputs.isInputLocked = false;
            
            // Forzar el estado del cursor para el jugador local
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}



