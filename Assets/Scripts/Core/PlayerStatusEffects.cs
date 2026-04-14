using Unity.Netcode;
using UnityEngine;
using StarterAssets;

public class PlayerStatusEffects : NetworkBehaviour
{
    [Header("Buffos y Estados Alterados")]
    public NetworkVariable<bool> tieneCotaMalla = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isTonto = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> hasLoboAlbinoPower = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isStunned = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isSilencioso = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isPoisoned = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> hasSegundaVida = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private PlayerState playerState;
    private ThirdPersonController thirdPersonController;
    private Animator animator;

    private void Awake()
    {
        playerState = GetComponent<PlayerState>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        isStunned.OnValueChanged += OnStunStateChanged;

        if (IsServer)
        {
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null) gm.currentPhase.OnValueChanged += AlCambiarFaseServidor;
        }
    }

    public override void OnNetworkDespawn()
    {
        isStunned.OnValueChanged -= OnStunStateChanged;
        
        if (IsServer)
        {
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null) gm.currentPhase.OnValueChanged -= AlCambiarFaseServidor;
        }
        
        base.OnNetworkDespawn();
    }

    private void AlCambiarFaseServidor(GamePhase vieja, GamePhase nueva)
    {
        if (!IsServer) return;
        if (nueva == GamePhase.Dia)
        {
            if (isSilencioso.Value)
            {
                isSilencioso.Value = false;
                Debug.Log($"[Server] Las Botas Silenciosas de {OwnerClientId} se han desgastado con el nuevo día.");
            }
        }
    }

    private void OnStunStateChanged(bool estadoAnterior, bool estadoNuevo)
    {
        if (estadoNuevo == true)
        {
            if (thirdPersonController != null) thirdPersonController.enabled = false;
            if (animator != null) animator.SetFloat("Speed", 0f);
            Debug.Log($"<color=green>[Visión Local] ¡Jugador {OwnerClientId} se está asfixiando por Bomba Apestosa!</color>");
        }
        else
        {
            if (thirdPersonController != null && playerState != null && !playerState.isDead.Value) 
                thirdPersonController.enabled = true;
                
            Debug.Log($"<color=grey>[Visión Local] Jugador {OwnerClientId} volvió a respirar.</color>");
        }
    }

    // --- FUNCIONES SERVIDOR PARA APLICAR ALTERACIONES ---

    public void AplicarStunEnServidor(float duracion)
    {
        if (!IsServer || (playerState != null && playerState.isDead.Value)) return;
        
        isStunned.Value = true;
        CancelInvoke(nameof(RecuperarDeStun)); 
        Invoke(nameof(RecuperarDeStun), duracion);
        Debug.Log($"[Server] Jugador {OwnerClientId} aturdido por {duracion}s.");
    }
    
    private void RecuperarDeStun()
    {
        if (!IsServer || (playerState != null && playerState.isDead.Value)) return;
        isStunned.Value = false;
    }

    public void AplicarVenenoEnServidor()
    {
        if (!IsServer || (playerState != null && playerState.isDead.Value)) return;
        isPoisoned.Value = true;
        MostrarParticulasVenenoClientRpc();
        Invoke(nameof(EjecutarMuertePorVeneno), 10f); // 10 segundos de agonía
    }

    private void EjecutarMuertePorVeneno()
    {
        if (!IsServer || (playerState != null && playerState.isDead.Value)) return;
        
        if (hasSegundaVida.Value)
        {
            hasSegundaVida.Value = false;
            isPoisoned.Value = false;
            Debug.Log($"<color=cyan>[Server] El veneno mató a {OwnerClientId}, ¡pero resucitó gracias a la Poción de Vida consumida previamente!</color>");
            return;
        }

        if (playerState != null) playerState.isDead.Value = true;
        isPoisoned.Value = false;
        Debug.Log($"<color=purple>[Server] El jugador {OwnerClientId} ha fallecido trágicamente por el veneno.</color>");
        
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null) gm.CheckWinConditions();
    }

    [ClientRpc]
    private void MostrarParticulasVenenoClientRpc()
    {
        Debug.Log($"<color=purple>☠️ [Visual] El jugador {OwnerClientId} empezó a burbujear gas venenoso.</color>");
    }
}
