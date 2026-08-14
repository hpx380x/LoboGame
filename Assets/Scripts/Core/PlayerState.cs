using Unity.Netcode;
using UnityEngine;
using StarterAssets;
using Core.Enums;


public class PlayerState : NetworkBehaviour
{
    [Header("Identidad y Estado (Server-Auth)")]
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

    public NetworkVariable<RolAldea> rolAldea = new NetworkVariable<RolAldea>(
        RolAldea.Ninguno,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Refugio Nocturno")]
    public NetworkVariable<ulong> myHouseId = new NetworkVariable<ulong>(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Sistema de Tareas")]
    public NetworkVariable<int> tareasCompletadasHoy = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Vinculos de Jugador")]
    public NetworkVariable<ulong> amanteId = new NetworkVariable<ulong>(
        9999, // 9999 significa que no tiene amante
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    [Header("Identidad y Nombre")]
    public NetworkVariable<Unity.Collections.FixedString32Bytes> playerName = new NetworkVariable<Unity.Collections.FixedString32Bytes>(
        "Jugador",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Sistema de Huellas")]
    public static System.Collections.Generic.List<GameObject> todasLasHuellas = new System.Collections.Generic.List<GameObject>();

    private CharacterController characterController;
    private ThirdPersonController thirdPersonController;
    private Animator animator;
    private SpectatorController _spectatorController;
    private PlayerStatusEffects statusEffects;
    private ScrollController scrollController;

    [Header("Herencia de Oficios")]
    [SerializeField] private RolePrefabMapping[] prefabsOficio;

    [System.Serializable]
    public struct RolePrefabMapping
    {
        public RolAldea rol;
        public GameObject prefab;
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        animator = GetComponent<Animator>();
        _spectatorController = GetComponent<SpectatorController>();
        statusEffects = GetComponent<PlayerStatusEffects>(); // Puede existir o no, la lógica vital la usamos como consulta
        scrollController = GetComponent<ScrollController>();
    }

    // Propiedad pública generalizada
    public bool isInputLocked => isDead.Value || (statusEffects != null && statusEffects.isStunned.Value);

    private void Update()
    {
        // [TESTING] Tecla K para suicidio
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.kKey.wasPressedThisFrame)
        {
            if (IsOwner) SuicideServerRpc();
        }

        if (!IsOwner) return;

        if (isInputLocked)
        {
            if (thirdPersonController != null) thirdPersonController.CanMove = false;
        }
        else
        {
            // Solo restaurar si no está leyendo el pergamino
            bool reading = scrollController != null && scrollController.IsReading;
            if (!reading)
            {
                if (thirdPersonController != null) thirdPersonController.CanMove = true;
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        isDead.OnValueChanged += OnDeathStateChanged;
        
        if (isDead.Value) ApplyDeathPhysics();

        // [LIMPIEZA] Forzar que la animación de "Sentado" y de "Interacción" estén apagadas.
        // Esto previene que herede el estado del prefab del Lobby al entrar a Gameplay o inicie bloqueada.
        if (animator != null)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == "isSitting")
                {
                    animator.SetBool("isSitting", false);
                }
                if (param.name == "IsInteracting")
                {
                    animator.SetBool("IsInteracting", false);
                }
            }

            // [NUEVO/FIX] Forzar que el peso de la capa de sentado (Lobby Layer) sea 0 en gameplay y test scenes
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Scene_Menu")
            {
                int lobbyLayerIdx = animator.GetLayerIndex("Lobby Layer");
                if (lobbyLayerIdx != -1)
                {
                    animator.SetLayerWeight(lobbyLayerIdx, 0f);
                    Debug.Log($"[PlayerState] {name}: Peso de la capa 'Lobby Layer' forzado a 0f en OnNetworkSpawn fuera de la escena del Lobby.");
                }
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        isDead.OnValueChanged -= OnDeathStateChanged;
        base.OnNetworkDespawn();
    }

    private void OnDeathStateChanged(bool estadoAnterior, bool estadoNuevo)
    {
        if (estadoNuevo == true && estadoAnterior == false)
        {
            ApplyDeathPhysics();
            
            // MECÁNICA AMANTES
            if (IsServer && amanteId.Value != 9999)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(amanteId.Value, out NetworkObject amanteObj))
                {
                    PlayerState psAmante = amanteObj.GetComponent<PlayerState>();
                    if (psAmante != null && !psAmante.isDead.Value) psAmante.isDead.Value = true;
                }
            }

            // [NUEVO] Si el jugador tenía un oficio, suelta el objeto al morir (Herencia)
            if (IsServer)
            {
                SoltarObjetoOficio(rolAldea.Value);
            }
        }
        else if (estadoNuevo == false && estadoAnterior == true)
        {
            ApplyResurrectionPhysics();
        }
    }

    private void SoltarObjetoOficio(RolAldea rol)
    {
        if (rol == RolAldea.Ninguno || prefabsOficio == null) return;

        foreach (var mapping in prefabsOficio)
        {
            if (mapping.rol == rol && mapping.prefab != null)
            {
                GameObject obj = Instantiate(mapping.prefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
                NetworkObject netObj = obj.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn();
                    Debug.Log($"[Servidor] El jugador {OwnerClientId} era {rol} y ha soltado su herramienta al morir.");
                }
                break;
            }
        }
    }

    private void ApplyResurrectionPhysics()
    {
        if (characterController != null) characterController.enabled = true;
        if (thirdPersonController != null) thirdPersonController.enabled = true;
        
        if (IsOwner)
        {
            if (TryGetComponent(out UnityEngine.InputSystem.PlayerInput pi)) pi.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (_spectatorController != null) _spectatorController.enabled = false;
        }

        if (animator != null)
        {
            animator.ResetTrigger("Muerte");
            animator.SetTrigger("Revivir"); 
        }
    }

    private void ApplyDeathPhysics()
    {
        if (IsServer)
        {
            GameManager gm = FindAnyObjectByType<GameManager>();
            if (gm != null) gm.CheckWinConditions();
        }

        if (IsOwner)
        {
            if (TryGetComponent(out ThirdPersonController tpc))
            {
                tpc.CanMove = false;
            }

            GameplayUI ui = FindAnyObjectByType<GameplayUI>();
            if (ui != null) ui.ToggleScroll(false);

            if (_spectatorController != null) _spectatorController.enabled = true;
        }
        
        if (animator != null)
        {
            animator.SetTrigger("Muerte");
            animator.SetBool("IsReadingScroll", false);
            animator.SetFloat("Speed", 0f);
            animator.SetFloat("MotionSpeed", 0f);
        }
    }

    [ServerRpc]
    public void SuicideServerRpc()
    {
        isDead.Value = true;
    }

    [Rpc(SendTo.Server)] 
    public void EnviarMiVotoServerRpc(ulong candidatoId)
    {
        GameManager gm = FindAnyObjectByType<GameManager>();
        if (gm != null) gm.RegistrarVotoCentralizado(OwnerClientId, candidatoId);
    }

    [Rpc(SendTo.Server)]
    public void NotificarTareaCompletadaServerRpc()
    {
        tareasCompletadasHoy.Value++;
    }

    [ClientRpc]
    public void CambioRolPrivadoClientRpc(bool nuevoRolEsLobo, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"<color=yellow>🎭 [Ladrón] Ahora eres: {(nuevoRolEsLobo ? "Lobo" : "Aldeano")} 🎭</color>");
        
        GameplayUI ui = FindAnyObjectByType<GameplayUI>();
        if (ui != null)
        {
            // Forzar repintado de UI
            PlayerInventory inv = GetComponent<PlayerInventory>();
            if (inv != null) ui.ActualizarMonedas(inv.monedas.Value); 
        }
    }

    [ClientRpc]
    public void ForzarTeletransporteClientRpc(Vector3 nuevaPosicion, Quaternion nuevaRotacion, ClientRpcParams rpcParams = default)
    {
        if (characterController != null) characterController.enabled = false;
        if (thirdPersonController != null) thirdPersonController.enabled = false;

        transform.position = nuevaPosicion + Vector3.up * 1.5f; 
        transform.rotation = nuevaRotacion;
        
        Physics.SyncTransforms();

        if (characterController != null) characterController.enabled = true;
        if (thirdPersonController != null) thirdPersonController.enabled = true;
    }

    [ClientRpc]
    public void RepelerJugadorClientRpc(Vector3 fuerzaEmpuje, ClientRpcParams rpcParams = default)
    {
        if (characterController != null) characterController.enabled = false;
        if (thirdPersonController != null) thirdPersonController.enabled = false;

        transform.position += fuerzaEmpuje;

        Physics.SyncTransforms();

        if (characterController != null) characterController.enabled = true;
        if (thirdPersonController != null) thirdPersonController.enabled = true;
    }
}

