using Unity.Netcode;
using UnityEngine;
using StarterAssets;

public class PlayerState : NetworkBehaviour
{
    [Header("Identidad y Estado (Server-Auth)")]
    // [Regla de Netcode] Solo el servidor puede modificar estas variables, pero todos las pueden leer.
    public NetworkVariable<bool> isWolf = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    // Referencias a los componentes de movimiento de tu personaje de StarterAssets
    private CharacterController characterController;
    private ThirdPersonController thirdPersonController;
    private Animator animator;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        // Nos suscribimos matemáticamente: Si el servidor decreta mi muerte, mi juego reaccionará localmente.
        isDead.OnValueChanged += OnDeathStateChanged;
        
        // Comprobación de seguridad al nacer (por si un cliente se une tarde y el cuerpo ya era un cadáver)
        if (isDead.Value)
        {
            ApplyDeathPhysics();
        }
    }

    public override void OnNetworkDespawn()
    {
        isDead.OnValueChanged -= OnDeathStateChanged;
        base.OnNetworkDespawn();
    }

    private void OnDeathStateChanged(bool estadoAnterior, bool estadoNuevo)
    {
        // Si acabamos de morir de verdad
        if (estadoNuevo == true && estadoAnterior == false)
        {
            ApplyDeathPhysics();
        }
    }

    private void ApplyDeathPhysics()
    {
        Debug.Log($"[Resolución de Combate] Reproduciendo animación de Muerte para el jugador {OwnerClientId}.");

        // [NUEVO] Le avisamos al Juez para que compruebe si la partida terminó con esta muerte
        if (IsServer)
        {
            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null) gm.CheckWinConditions();
        }

        // Desactivamos el control del motor de colisión para que sea un objeto estático
        if (characterController != null) characterController.enabled = false;
        
        // Desactivamos el script principal de movimiento (ya no puede correr ni saltar)
        if (thirdPersonController != null) thirdPersonController.enabled = false;

        // [NUEVO] ¡Ya no apagamos el Animador! Le ordenamos que dispare su animación de muerte.
        if (animator != null)
        {
            animator.SetTrigger("Muerte");
            
            // Opcional: Asegurarnos de que el jugador no siga "corriendo" visualmente en la muerte
            animator.SetFloat("Speed", 0f);
            animator.SetFloat("MotionSpeed", 0f);
        }

        // Ya no rotamos forzosamente 90 grados ni tocamos el Transform, la animación se encargará de tirarlo.
    }

    [Rpc(SendTo.Server)] // Mandado desde tu personaje (Tiene permiso Owner 100% garantizado)
    public void EnviarMiVotoServerRpc(ulong candidatoId)
    {
        // Esto solo lo lee el Servidor cuando recibe tu voto
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.RegistrarVotoCentralizado(OwnerClientId, candidatoId);
        }
    }
}
