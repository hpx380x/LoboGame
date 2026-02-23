using UnityEngine;
using Unity.Netcode;
using Cinemachine;

public class NetworkPlayerSetup : NetworkBehaviour
{
    [Header("Referencias del Jugador")]
    [Tooltip("El 'PlayerCameraRoot' que está dentro de tu jugador (donde mira la cámara)")]
    [SerializeField] private GameObject cameraTarget;

    public override void OnNetworkSpawn()
    {
        // [Regla 3] Si no soy el dueño local de este personaje, apaga sus controles para que no lo mueva yo
        if (!IsOwner)
        {
            // Apaga el ThirdPersonController y el PlayerInput para los "clones" (los otros jugadores)
            if (TryGetComponent(out StarterAssets.ThirdPersonController tpc)) tpc.enabled = false;
            if (TryGetComponent(out UnityEngine.InputSystem.PlayerInput pi)) pi.enabled = false;
            return; // Detenemos aquí la ejecución
        }

        // --- SOLO EL DUEÑO (LOCAL) LLEGA A ESTE PUNTO ---

        // -- Buscador Universal de Cámaras Cinemachine --
        var virtualCamera = FindFirstObjectByType<CinemachineVirtualCamera>(); 
        
        if (virtualCamera != null)
        {
            if (cameraTarget != null)
            {
                virtualCamera.Follow = cameraTarget.transform;
            }
            else 
            {
                virtualCamera.Follow = this.transform; // Fallback, seguir al cuerpo
            }
        }
        else
        {
            Debug.LogWarning("[Sistema de Red] No se detectó ninguna 'CinemachineVirtualCamera' activa en el mapa. ¿Estás en un menú vacío o olvidaste añadir una cámara en la escena?");
        }
    }
}
