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

        // Busca la cámara que está en la escena principal por su etiqueta "MainCamera" o nombre.
        var virtualCamera = GameObject.Find("PlayerFollowCamera"); 
        
        if (virtualCamera != null)
        {
            var cinemachineBrain = virtualCamera.GetComponent<CinemachineVirtualCamera>();
            if (cinemachineBrain != null)
            {
                cinemachineBrain.Follow = cameraTarget.transform;
            }
        }
        else
        {
            Debug.LogError("No se encontró el objeto 'PlayerFollowCamera' en la escena.");
        }
    }
}
