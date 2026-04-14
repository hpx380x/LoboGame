using UnityEngine;
using Unity.Netcode;
using Cinemachine;
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
            if (TryGetComponent(out ThirdPersonController tpc)) tpc.enabled = false;
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
        var virtualCamera = FindFirstObjectByType<CinemachineVirtualCamera>(); 
        
        if (virtualCamera != null)
        {
            if (cameraTarget != null)
            {
                virtualCamera.Follow = cameraTarget.transform;
                virtualCamera.LookAt = cameraTarget.transform;
                Debug.Log("[Setup] Virtual Camera vinculada al PlayerCameraRoot.");
            }
            else 
            {
                virtualCamera.Follow = this.transform;
                virtualCamera.LookAt = this.transform;
                Debug.Log("[Setup] Virtual Camera vinculada al Transform (cameraTarget era null).");
            }
        }
        else
        {
            Debug.LogWarning("[Setup] No se encontró CinemachineVirtualCamera en esta escena. La cámara no seguirá al jugador.");
        }

        // [Fix] Aseguramos que el PlayerInput y StarterAssetsInputs estén limpios y activos
        if (TryGetComponent(out PlayerInput playerInput))
        {
            playerInput.enabled = true;
            playerInput.ActivateInput(); // Forzamos el inicio del sistema de input
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

