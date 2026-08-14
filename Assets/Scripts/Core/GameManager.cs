using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core.Enums;
using Core.Environment;


public enum GamePhase
{
    Lobby,
    AsignandoRoles,
    Noche,
    Dia,
    Votacion
}

public class GameManager : NetworkBehaviour
{
    [Header("Configuración")]
    [Tooltip("El prefab del jugador (PlayerArmature) que se instanciará al iniciar la partida")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Estado de la Partida")]
    public NetworkVariable<GamePhase> currentPhase = new NetworkVariable<GamePhase>(
        GamePhase.Lobby, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    [Header("Lista de Jugadores en Sala")]
    public NetworkVariable<FixedString4096Bytes> listaJugadoresNetwork = new NetworkVariable<FixedString4096Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public event System.Action<string> OnListaJugadoresModificada;

    [Header("Código de Sala (Relay)")]
    public NetworkVariable<FixedString32Bytes> codigoSalaNetwork = new NetworkVariable<FixedString32Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    // [Regla 5] Evento local para mostrar el código de la sala en toda la UI de todos los clientes
    public event System.Action<string> OnCodigoSalaModificado;

    [Header("Reloj del Servidor")]
    private float tiempoRestanteFase = 0f;
    [Tooltip("Duración en segundos de la fase de Día")]
    [SerializeField] private float tiempoDiaSegundos = 60f;
    [Tooltip("Duración en segundos de la fase de Noche (Asesinatos)")]
    [SerializeField] private float tiempoNocheSegundos = 30f;
    [Tooltip("Duración en segundos de la Asamblea de Votación")]
    [SerializeField] private float tiempoVotacionSegundos = 30f;
    [Header("Sistema de Tareas")]
    // [Tooltip("Número de tareas que se asignan a cada jugador por día.")]
    // [SerializeField] private int tareasPerJugador = 2; // (No se usa actualmente)

    [Header("DEBUG - Testing de Misiones")]
    [Tooltip("Arrastra aquí el asset QuestData para probarlo directamente")]
    [SerializeField] private Core.QuestSystem.QuestData debugQuestAsset;
    [Tooltip("Escribe aquí el questID exacto que quieres probar (se ignora si usas debugQuestAsset arriba)")]
    public string debugQuestID = "mision_manzana_oro";
    [Tooltip("El ID del cliente en red (0 suele ser el Host)")]
    public ulong debugTargetPlayerID = 0;

    [ContextMenu("Debug: Asignar Quest al Jugador")]
    public void DebugForceAssignQuest()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[GameManager Debug] Solo el servidor/host puede asignar misiones de test.");
            return;
        }

        string questIDParaAsignar = debugQuestAsset != null ? debugQuestAsset.questID : debugQuestID;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(debugTargetPlayerID, out var clientInfo))
        {
            if (clientInfo.PlayerObject != null)
            {
                var tracker = clientInfo.PlayerObject.GetComponent<Core.QuestSystem.PlayerQuestTracker>();
                if (tracker != null)
                {
                    tracker.ServerAssignQuest(questIDParaAsignar);
                }
            }
        }
        else
        {
            Debug.LogWarning($"[GameManager Debug] No se encontró al jugador con ID {debugTargetPlayerID} en la sala.");
        }
    }

    [ContextMenu("Debug: Asignar Quest a TODOS")]
    public void DebugForceAssignQuestToAll()
    {
        if (!IsServer) return;

        string questIDParaAsignar = debugQuestAsset != null ? debugQuestAsset.questID : debugQuestID;

        foreach (var clientInfo in NetworkManager.Singleton.ConnectedClients.Values)
        {
            if (clientInfo.PlayerObject != null)
            {
                var tracker = clientInfo.PlayerObject.GetComponent<Core.QuestSystem.PlayerQuestTracker>();
                if (tracker != null)
                {
                    tracker.ServerAssignQuest(questIDParaAsignar);
                }
            }
        }
        Debug.Log($"[GameManager Debug] Misión '{questIDParaAsignar}' forzada a TODOS los jugadores de la sala.");
    }

    private Dictionary<ulong, int> conteoVotos = new Dictionary<ulong, int>();
    private HashSet<ulong> jugadoresQueVotaron = new HashSet<ulong>();

    private bool isStarting = false;
    private bool esPrimerDia = true;
    private bool partidaTerminada = false;
    private int muertesAlInicioDeLaNoche = 0;

    // Cache de objetos de la escena para evitar FindAnyObjectByType recurrentes
    private GameplayUI cachedGameplayUI = null;
    private Light cachedDirectionalLight = null;
    private Color colorDiaOriginal = Color.white;
    private bool colorOriginalObtenido = false;
    private UnityEngine.EventSystems.EventSystem cachedEventSystem = null;
    private VotingUI cachedVotingUI = null;

    private GameplayUI GetGameplayUI()
    {
        if (cachedGameplayUI == null) cachedGameplayUI = Object.FindAnyObjectByType<GameplayUI>(FindObjectsInactive.Include);
        return cachedGameplayUI;
    }

    private Light GetDirectionalLight()
    {
        if (cachedDirectionalLight == null)
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    cachedDirectionalLight = l;
                    break;
                }
            }
        }
        return cachedDirectionalLight;
    }

    private UnityEngine.EventSystems.EventSystem GetEventSystem()
    {
        if (cachedEventSystem == null) cachedEventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
        return cachedEventSystem;
    }

    private VotingUI GetVotingUI()
    {
        if (cachedVotingUI == null) cachedVotingUI = Object.FindAnyObjectByType<VotingUI>(FindObjectsInactive.Include);
        return cachedVotingUI;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        cachedGameplayUI = null;
        cachedDirectionalLight = null;
        cachedEventSystem = null;
        cachedVotingUI = null;
    }

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // [Regla Especial] Hacemos que la "nave" del GameManager sea indestructible
        // para que sobreviva al agujero negro del cambio de escena y llegue a la Tierra (Scene_Gameplay)
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.DestroyWithScene = false;
        }
        DontDestroyOnLoad(this.gameObject);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Nos suscribimos para escuchar cada vez que la fase cambie en la red
        currentPhase.OnValueChanged += AlCambiarDeFase;
        
        // Cargar lista de jugadores en la UI
        listaJugadoresNetwork.OnValueChanged += AlCambiarListaJugadores;
        codigoSalaNetwork.OnValueChanged += AlCambiarCodigoSala;
        
        // Disparo inicial por si el cliente cargó la escena unos segundos tarde
        AlCambiarDeFase(currentPhase.Value, currentPhase.Value);
        AlCambiarListaJugadores(new FixedString4096Bytes(""), listaJugadoresNetwork.Value);
        AlCambiarCodigoSala(new FixedString32Bytes(""), codigoSalaNetwork.Value);

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += ActualizarListaJugadoresServidor;
            NetworkManager.Singleton.OnClientDisconnectCallback += ActualizarListaJugadoresServidor;
            ActualizarListaJugadoresServidor(NetworkManager.Singleton.LocalClientId); // Primer disparo para el test
        }

        // [Nuevo] Nos preparamos para el peor caso: que el Host apague su PC a mitad de partida
        if (IsClient)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += EnDesconexionInesperada;
        }
    }

    public override void OnNetworkDespawn()
    {
        currentPhase.OnValueChanged -= AlCambiarDeFase;
        listaJugadoresNetwork.OnValueChanged -= AlCambiarListaJugadores;
        codigoSalaNetwork.OnValueChanged -= AlCambiarCodigoSala;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= EnDesconexionInesperada;
            
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= ActualizarListaJugadoresServidor;
                NetworkManager.Singleton.OnClientDisconnectCallback -= ActualizarListaJugadoresServidor;
            }
        }

        base.OnNetworkDespawn();
    }

    private void EnDesconexionInesperada(ulong clientId)
    {
        // 0 normalmente significa que el servidor se cerró de repente
        // Y LocalClientId significa que fuimos nosotros mismos quienes cerramos o nos kickearon
        if (clientId == 0 || clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (partidaTerminada) return; // Si ya ganamos/perdimos, la corrutina de victoria ya nos está devolviendo, ignorar.

            Debug.Log("<color=red>[GameManager] ¡Pánico! El servidor se ha desconectado. Evacuando al menú...</color>");
            partidaTerminada = true;

            // 1. Desbloqueamos el ratón para no quedarnos atrapados en el menú
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 2. Destruimos las conexiones de red sobrantes
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
                Destroy(NetworkManager.Singleton.gameObject);
            }

            // 3. Volvemos a la escena principal
            SceneManager.LoadScene("Scene_Menu");

            // 4. Auto-destrucción de esta "Nave" GameManager
            Destroy(gameObject);
        }
    }

    private void AlCambiarListaJugadores(FixedString4096Bytes valorViejo, FixedString4096Bytes valorNuevo)
    {
        // Notificamos a nuestra UI localmente usando el evento de C# (Desacoplamiento)
        OnListaJugadoresModificada?.Invoke(valorNuevo.ToString());
    }

    private void AlCambiarCodigoSala(FixedString32Bytes valorViejo, FixedString32Bytes valorNuevo)
    {
        OnCodigoSalaModificado?.Invoke(valorNuevo.ToString());
    }

    public void EstablecerCodigoSalaSincronizado(string nuevoCodigo)
    {
        if (IsServer)
        {
            codigoSalaNetwork.Value = new FixedString32Bytes(nuevoCodigo);
        }
    }

    private Dictionary<ulong, string> clientNicknames = new Dictionary<ulong, string>();

    public void RegistrarNicknameCliente(ulong clientId, string nickname)
    {
        if (!IsServer) return;
        clientNicknames[clientId] = nickname;
        // Forzar actualización inmediata de la lista
        ActualizarListaJugadoresServidor(clientId);
    }

    public string ObtenerNicknameCliente(ulong clientId)
    {
        if (clientNicknames.TryGetValue(clientId, out string nickname))
        {
            return nickname;
        }
        return $"Jugador {clientId}";
    }

    private void ActualizarListaJugadoresServidor(ulong clientId)
    {
        // Regla 3: Guard de Servidor
        if (!IsServer) return;
        
        // Limpiar nicknames de clientes desconectados
        List<ulong> aEliminar = new List<ulong>();
        foreach (var key in clientNicknames.Keys)
        {
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(key))
            {
                aEliminar.Add(key);
            }
        }
        foreach (var id in aEliminar)
        {
            clientNicknames.Remove(id);
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int conteo = 0;
        
        foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            conteo++;
            string nickname = ObtenerNicknameCliente(id);
            if (id == NetworkManager.Singleton.LocalClientId)
            {
                sb.Append($"- {nickname} (HOST)\n");
            }
            else
            {
                sb.Append($"- {nickname}\n");
            }
        }
        
        string cabecera = $"<color=yellow>JUGADORES EN LA SALA ({conteo}/10):</color>\n\n";
        listaJugadoresNetwork.Value = new FixedString4096Bytes(cabecera + sb.ToString());
    }


    private Coroutine serverPhaseTimer;

    private IEnumerator RutinaRelojFase(float duration, GamePhase nextPhase, System.Action onPhaseEnd)
    {
        yield return new WaitForSeconds(duration);
        if (partidaTerminada) yield break;
        
        onPhaseEnd?.Invoke();
        currentPhase.Value = nextPhase;
    }

    private int ContarJugadoresMuertos()
    {
        int count = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Value.PlayerObject == null) continue;
            PlayerState ps = client.Value.PlayerObject.GetComponent<PlayerState>();
            if (ps != null && ps.isDead.Value) count++;
        }
        return count;
    }

    private void EvaluarAmanecer()
    {
        if (!IsServer) return;

        int muertosActuales = ContarJugadoresMuertos();
        bool huboMuertosEstaNoche = muertosActuales > muertesAlInicioDeLaNoche;

        // Abrir todas las puertas de las casas obligatoriamente
        HouseController[] casas = FindObjectsByType<HouseController>(FindObjectsInactive.Exclude);
        foreach (var casa in casas)
        {
            casa.IsDoorLocked.Value = false;
        }

        // Optimización O(N + M): Crear diccionario indexado por NetworkObjectId
        Dictionary<ulong, HouseController> casasDict = new Dictionary<ulong, HouseController>();
        foreach (var casa in casas)
        {
            casasDict[casa.NetworkObjectId] = casa;
        }

        if (huboMuertosEstaNoche)
        {
            Debug.Log("[Servidor] Hubo sangre esta noche. Invocando a todos a la plaza central para asamblea.");
            SpawnManager spawnManager = FindAnyObjectByType<SpawnManager>();
            
            foreach (var clientInfo in NetworkManager.Singleton.ConnectedClients)
            {
                if (clientInfo.Value.PlayerObject == null) continue;
                PlayerState ps = clientInfo.Value.PlayerObject.GetComponent<PlayerState>();
                if (ps != null && !ps.isDead.Value)
                {
                    Transform spawnDestino = spawnManager != null ? spawnManager.GetNextVotingSpawnPoint() : null;
                    if (spawnDestino != null)
                    {
                        ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientInfo.Value.ClientId } } };
                        ps.ForzarTeletransporteClientRpc(spawnDestino.position, spawnDestino.rotation, rpcParams);
                    }
                }
            }
        }
        else
        {
            Debug.Log("[Servidor] Noche pacífica. Los jugadores amanecen en sus camas.");
            foreach (var clientInfo in NetworkManager.Singleton.ConnectedClients)
            {
                if (clientInfo.Value.PlayerObject == null) continue;
                PlayerState ps = clientInfo.Value.PlayerObject.GetComponent<PlayerState>();
                if (ps != null && !ps.isDead.Value)
                {
                    if (casasDict.TryGetValue(ps.myHouseId.Value, out HouseController miCasa))
                    {
                        if (miCasa.bedSpawnPoint != null)
                        {
                            ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientInfo.Value.ClientId } } };
                            ps.ForzarTeletransporteClientRpc(miCasa.bedSpawnPoint.position, miCasa.bedSpawnPoint.rotation, rpcParams);
                        }
                    }
                }
            }
        }
    }

    private void PrepararVotacion()
    {
        Debug.Log("[Servidor] Iniciando Asamblea de Votación. Teletransportando jugadores vivos...");
        conteoVotos.Clear();
        jugadoresQueVotaron.Clear();

        SpawnManager spawnManager = FindAnyObjectByType<SpawnManager>();

        foreach (var clientInfo in NetworkManager.Singleton.ConnectedClients)
        {
            NetworkObject netObj = clientInfo.Value.PlayerObject;
            if (netObj != null)
            {
                PlayerState ps = netObj.GetComponent<PlayerState>();
                if (ps != null && !ps.isDead.Value)
                {
                    // Notificamos al jugador que debe ir a votar, pero no lo forzamos.
                }
            }
        }
    }

    private void ResolverVotacion()
    {
        Debug.Log("[Servidor] Tiempo agotado. Resolviendo votación...");
        
        // Forzar voto nulo (ulong.MaxValue) para los vivos que no votaron
        foreach (var clientInfo in NetworkManager.Singleton.ConnectedClients)
        {
            if (clientInfo.Value.PlayerObject != null)
            {
                PlayerState ps = clientInfo.Value.PlayerObject.GetComponent<PlayerState>();
                if (ps != null && !ps.isDead.Value)
                {
                    if (!jugadoresQueVotaron.Contains(clientInfo.Key))
                    {
                        if (conteoVotos.ContainsKey(ulong.MaxValue))
                            conteoVotos[ulong.MaxValue]++;
                        else
                            conteoVotos[ulong.MaxValue] = 1;
                    }
                }
            }
        }

        // Si nadie voto absolutamente por nadie, continuamos
        if (conteoVotos.Count == 0 || (conteoVotos.Count == 1 && conteoVotos.ContainsKey(ulong.MaxValue)))
        {
            Debug.Log("[Servidor] Nadie recibió votos. La partida continúa normalmente.");
            return;
        }

        int maxVotos = 0;
        ulong candidatoElegido = ulong.MaxValue;
        bool empate = false;

        foreach (var par in conteoVotos)
        {
            ulong votado = par.Key;
            int votos = par.Value;

            if (votos > maxVotos)
            {
                maxVotos = votos;
                candidatoElegido = votado;
                empate = false;
            }
            else if (votos == maxVotos)
            {
                empate = true;
            }
        }

        if (empate)
        {
            Debug.Log("[Servidor] Empate: Nadie muere.");
        }
        else if (candidatoElegido != ulong.MaxValue && maxVotos > 0)
        {
            Debug.Log($"[Servidor] ¡El jugador {candidatoElegido} ha sido expulsado con {maxVotos} votos!");
            
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(candidatoElegido, out var networkClient))
            {
                PlayerState ps = networkClient.PlayerObject.GetComponent<PlayerState>();
                if (ps != null && !ps.isDead.Value)
                {
                    ps.isDead.Value = true; // El Animator matará al personaje de verdad
                }
            }
        }
    }

    // 100% Método local del servidor. Lo invoca el PlayerState del que votó.
    public void RegistrarVotoCentralizado(ulong votanteId, ulong candidatoId)
    {
        if (currentPhase.Value != GamePhase.Votacion) return;

        // Regla: Solo un voto por persona
        if (jugadoresQueVotaron.Contains(votanteId))
        {
            Debug.LogWarning($"[Servidor] El jugador {votanteId} intentó votar dos veces. Voto bloqueado.");
            return;
        }

        // Verificamos si realmente existe como jugador y si está vivo
        PlayerState psVotante = null;
        PlayerStatusEffects statusVotante = null;
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(votanteId, out var infoCliente))
        {
            psVotante = infoCliente.PlayerObject.GetComponent<PlayerState>();
            statusVotante = infoCliente.PlayerObject.GetComponent<PlayerStatusEffects>();
            if (psVotante == null || psVotante.isDead.Value) return;
        }

        // Anotamos en la lista oficial que esta persona ya no puede volver a pulsar botones
        jugadoresQueVotaron.Add(votanteId);

        // [Mecánica Sombrero de Tonto] Si el jugador tiene el sombrero, su voto se anula.
        if (statusVotante != null && statusVotante.isTonto.Value)
        {
            Debug.Log($"<color=magenta>[Servidor] El voto de {votanteId} fue anulado silenciosamente porque lleva el Sombrero de Tonto.</color>");
            return; // No sumar a conteoVotos
        }

        // Si pulso omitir, ulong.MaxValue; en caso contrario, sumamos el voto
        if (candidatoId != ulong.MaxValue)
        {
            if (!conteoVotos.ContainsKey(candidatoId)) conteoVotos[candidatoId] = 0;
            conteoVotos[candidatoId]++;
            Debug.Log($"[Servidor] Jugador {votanteId} emitió voto contra el jugador {candidatoId}.");
        }
        else
        {
            Debug.Log($"[Servidor] Jugador {votanteId} emitió una omisión.");
        }
    }

    private void AlCambiarDeFase(GamePhase faseAntigua, GamePhase faseNueva)
    {
        // 1. Lógica del Cliente (UI, Luces, Inputs, Votación)
        AlCambiarDeFase_Cliente(faseAntigua, faseNueva);

        // 2. Lógica del Servidor (Timers, Asignación de misiones)
        if (IsServer)
        {
            AlCambiarDeFase_Servidor(faseAntigua, faseNueva);
        }
    }

    private void AlCambiarDeFase_Cliente(GamePhase faseAntigua, GamePhase faseNueva)
    {
        GameplayUI gameUI = GetGameplayUI();
        if (gameUI != null) gameUI.ActualizarFase(faseNueva.ToString());

        Light sol = GetDirectionalLight();
        if (sol != null && sol.type == LightType.Directional)
        {
            if (!colorOriginalObtenido)
            {
                colorDiaOriginal = sol.color;
                colorOriginalObtenido = true;
            }

            if (faseNueva == GamePhase.Noche)
            {
                sol.intensity = 0.05f;
                sol.color = new Color(0.15f, 0.2f, 0.45f); // Luz de luna azulada
                RenderSettings.ambientIntensity = 0.15f;    // Oscurecer ambiente general
                RenderSettings.ambientLight = new Color(0.08f, 0.08f, 0.15f); // Luz ambiental nocturna fría

                // Sincronizar el cielo a un tono azul oscuro nocturno
                if (RenderSettings.skybox != null)
                {
                    Color azulNoche = new Color(0.02f, 0.04f, 0.12f);
                    if (RenderSettings.skybox.HasProperty("_SkyTint")) RenderSettings.skybox.SetColor("_SkyTint", azulNoche);
                    if (RenderSettings.skybox.HasProperty("_Tint")) RenderSettings.skybox.SetColor("_Tint", azulNoche);
                }
            }
            else if (faseNueva == GamePhase.Dia)
            {
                sol.intensity = 1.0f;
                sol.color = new Color(1.0f, 0.95f, 0.85f);  // Forzar una luz solar cálida/blanca limpia para el día
                RenderSettings.ambientIntensity = 1.0f;    // Luz de ambiente brillante de día
                RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.2f); // Luz ambiental neutral diurna

                // Sincronizar el cielo a un hermoso azul diurno (en lugar de amarillo)
                if (RenderSettings.skybox != null)
                {
                    Color azulDia = new Color(0.25f, 0.5f, 0.85f);
                    if (RenderSettings.skybox.HasProperty("_SkyTint")) RenderSettings.skybox.SetColor("_SkyTint", azulDia);
                    if (RenderSettings.skybox.HasProperty("_Tint")) RenderSettings.skybox.SetColor("_Tint", azulDia);
                }
            }
            else if (faseNueva == GamePhase.Votacion)
            {
                sol.intensity = 0.5f;
                sol.color = new Color(1.0f, 0.55f, 0.35f); // Color atardecer / anaranjado
                RenderSettings.ambientIntensity = 0.6f;    // Luz de ambiente de atardecer
                RenderSettings.ambientLight = new Color(0.18f, 0.14f, 0.12f); // Luz ambiental cálida

                // Sincronizar el cielo a un tono atardecer anaranjado
                if (RenderSettings.skybox != null)
                {
                    Color naranjaAtardecer = new Color(0.75f, 0.4f, 0.25f);
                    if (RenderSettings.skybox.HasProperty("_SkyTint")) RenderSettings.skybox.SetColor("_SkyTint", naranjaAtardecer);
                    if (RenderSettings.skybox.HasProperty("_Tint")) RenderSettings.skybox.SetColor("_Tint", naranjaAtardecer);
                }
            }
        }

        // --- Manejo del Ratón y Bloqueo de Movimiento ---
        // [Fix] Aseguramos que el EventSystem y Canvas estén activos al cambiar de fase
        var es = GetEventSystem();
        if (es != null) es.gameObject.SetActive(true);
        
        var canvasGameplay = GetGameplayUI();
        if (canvasGameplay != null) canvasGameplay.gameObject.SetActive(true);
        
        if (faseNueva == GamePhase.Votacion)
        {
            // Ya no mostramos la pantalla de votación ni bloqueamos al jugador automáticamente.
            // Ahora lo tienen que hacer yendo al altar e interactuando.
        }
        else
        {
            if (faseNueva == GamePhase.Dia || faseNueva == GamePhase.Noche)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                PlayerState ps = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerState>();
                if (ps != null && !ps.isDead.Value)
                {
                    var tpc = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<StarterAssets.ThirdPersonController>();
                    if (tpc != null) tpc.CanMove = true;

                    // Le devolvemos el secuestro del ratón al StarterAssets
                    var inputs = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<StarterAssets.StarterAssetsInputs>();
                    if (inputs != null) 
                    {
                        inputs.cursorLocked = true;
                        inputs.cursorInputForLook = true;
                    }
                }
            }

            VotingUI votingUI = GetVotingUI();
            if (votingUI != null) votingUI.OcultarPantallaVotacion();
        }
    }

    private void AlCambiarDeFase_Servidor(GamePhase faseAntigua, GamePhase faseNueva)
    {
        if (faseNueva == GamePhase.Noche)
        {
            muertesAlInicioDeLaNoche = ContarJugadoresMuertos();
        }

        // ── Sistema de tareas: al comenzar un nuevo Día (Noche→Dia), asignar misiones nuevas ──
        if (faseAntigua == GamePhase.Noche && faseNueva == GamePhase.Dia)
        {
            List<ulong> jugadoresVivos = new List<ulong>();
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                if (client.Value.PlayerObject == null) continue;
                PlayerState ps = client.Value.PlayerObject.GetComponent<PlayerState>();
                if (ps != null && !ps.isDead.Value)
                {
                    ps.tareasCompletadasHoy.Value = 0; // Reset contador del día
                    jugadoresVivos.Add(client.Key);
                }
            }

            // [NUEVO] Resetear todos los interactuables de misión de la escena para que se puedan volver a hacer hoy
            var interactuables = FindObjectsByType<Core.Environment.UniversalQuestInteractable>(FindObjectsInactive.Include);
            foreach (var inter in interactuables)
            {
                inter.ResetearMisionDiaria();
            }

            var interactuablesLegacy = FindObjectsByType<Core.Environment.InteractivableMisionUniversal>(FindObjectsInactive.Include);
            foreach (var inter in interactuablesLegacy)
            {
                inter.ResetearMisionDiaria();
            }

            Debug.Log("[Servidor] Misiones diarias reseteadas para el nuevo día.");

            AsignarMisionesBasicasADiaNuevo(jugadoresVivos);
        }

        // ── Sistema de Reloj del Servidor (Basado en Eventos / Coroutines) ──
        if (!partidaTerminada)
        {
            if (serverPhaseTimer != null) StopCoroutine(serverPhaseTimer);

            if (faseNueva == GamePhase.Dia)
            {
                if (esPrimerDia)
                {
                    Debug.Log("[Servidor] Fin del Primer Día. No hay votación hoy, cae la Noche directa.");
                    esPrimerDia = false;
                    serverPhaseTimer = StartCoroutine(RutinaRelojFase(tiempoDiaSegundos, GamePhase.Noche, null));
                }
                else
                {
                    serverPhaseTimer = StartCoroutine(RutinaRelojFase(tiempoDiaSegundos, GamePhase.Votacion, PrepararVotacion));
                }
            }
            else if (faseNueva == GamePhase.Votacion)
            {
                serverPhaseTimer = StartCoroutine(RutinaRelojFase(tiempoVotacionSegundos, GamePhase.Noche, ResolverVotacion));
            }
            else if (faseNueva == GamePhase.Noche)
            {
                serverPhaseTimer = StartCoroutine(RutinaRelojFase(tiempoNocheSegundos, GamePhase.Dia, EvaluarAmanecer));
            }
        }
    }

    public void StartGame()
    {
        if (isStarting) return; // Si ya lo pulsamos milisegundos atrás, ignoramos este doble clic.
        isStarting = true;
        Debug.Log("[GameManager] Hemos pulsado el botón. Iniciando viaje a la partida...");

        // [Regla 1] Solo el servidor/host tiene el poder de arrastrar a todo el mundo a otra pantalla.
        if (!IsServer) 
        {
            Debug.LogWarning("[GameManager] ¡Mero mortal, no puedes cambiar de escena porque NO eres el servidor!");
            return;
        }

        // Cambiamos la fase del estado de red
        currentPhase.Value = GamePhase.AsignandoRoles;
        Debug.Log("[GameManager] Fase cambiada a AsignandoRoles.");

        try
        {
            Debug.Log("[GameManager] Destruyendo clones del Lobby antes del viaje...");
            
            // [CORRECCIÓN CRÍTICA] El Prefab 'PlayerArmatureLobby' no contenía el componente PlayerLobbyPose!
            // Por ello la función FindObjectsByType<PlayerLobbyPose> jamás lo encontraba. Lo buscaremos por nombre.
            NetworkObject[] todosLosObjetosEnRed = FindObjectsByType<NetworkObject>(FindObjectsInactive.Include);
            foreach (var redObj in todosLosObjetosEnRed)
            {
                if (redObj.gameObject.name.Contains("PlayerArmatureLobby"))
                {
                    Debug.Log($"[GameManager] Exterminando clon fantasma detectado: {redObj.gameObject.name}");
                    if (redObj.IsSpawned) redObj.Despawn(true);
                    else Destroy(redObj.gameObject);
                }
            }

            Debug.Log("[GameManager] Intentando cargar Scene_Gameplay usando NetworkManager.SceneManager...");
            
            if (NetworkManager.Singleton.SceneManager == null)
            {
                Debug.LogError("[ERROR GameManager] NetworkManager.Singleton.SceneManager es nulo! ¿Está la casilla 'Enable Scene Management' activa en el NetworkManager?");
                return;
            }

            // [NUEVO] ¡Regla de Oro! Nos suscribimos para hacer el Spawn CUANDO LA ESCENA ESTÉ LISTA, no en el vacío oscuro
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;

            // Ordenamos al Servidor que cargue la escena del juego. 
            SceneEventProgressStatus status = NetworkManager.Singleton.SceneManager.LoadScene("Scene_Gameplay", LoadSceneMode.Single);
            
            if (status == SceneEventProgressStatus.Started)
            {
                Debug.Log("<color=yellow>[GameManager] Petición de carga de escena enviada correctamente a Netcode.</color>");
            }
            else
            {
                Debug.LogError($"[ERROR GameManager CRÍTICO] Netcode se negó a cargar la escena. Motivo (Status): {status}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ERROR GameManager CRÍTICO] Ocurrió un error al intentar cargar la escena: {ex.Message}");
        }
    }

    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName != "Scene_Gameplay") return;
        
        // Misión Completada. Anulamos la suscripción para evitar que se repita si mueren/reviven más adelante.
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;

        Debug.Log("<color=green>============== ¡Hemos aterrizado en la escena! Procediendo con aparición de Cuerpos =============</color>");

        var clientIds = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        if (clientIds.Count == 0) return;

        int wolfIndex = Random.Range(0, clientIds.Count);
        ulong wolfId = clientIds[wolfIndex];

        // [NUEVO] Elegir un Herrero aleatorio entre los que NO son Lobos
        List<ulong> noLobos = new List<ulong>(clientIds);
        noLobos.Remove(wolfId);
        ulong herreroId = noLobos.Count > 0 ? noLobos[Random.Range(0, noLobos.Count)] : ulong.MaxValue;

        SpawnManager spawnManager = FindAnyObjectByType<SpawnManager>();

        // [NUEVO] Recopilar y barajar las casas para asignarlas
        HouseController[] todasLasCasas = FindObjectsByType<HouseController>(FindObjectsInactive.Exclude);
        List<HouseController> casasDisponibles = new List<HouseController>(todasLasCasas);
        for (int i = 0; i < casasDisponibles.Count; i++)
        {
            HouseController temp = casasDisponibles[i];
            int r = Random.Range(i, casasDisponibles.Count);
            casasDisponibles[i] = casasDisponibles[r];
            casasDisponibles[r] = temp;
        }
        int indexCasa = 0;

        foreach (ulong clientId in clientIds)
        {
            RolAldea rolAsignado = (clientId == herreroId) ? RolAldea.Herrero : RolAldea.Ninguno;
            string assignedRoleText = (clientId == wolfId) ? "Lobo" : "Aldeano";
            if (rolAsignado != RolAldea.Ninguno) assignedRoleText += $" ({rolAsignado})";

            if (playerPrefab != null)
            {
                Transform spawnPoint = spawnManager != null ? spawnManager.GetNextSpawnPoint() : null;
                Vector3 spawnPos = spawnPoint != null ? spawnPoint.position + Vector3.up * 1.0f : new Vector3(0, 10, 0); // Lo ponemos alto para que caiga sin traspasar
                Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

                GameObject playerInstance = Instantiate(playerPrefab, spawnPos, spawnRot);
                NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
                
                if (netObj != null)
                {
                    netObj.SpawnAsPlayerObject(clientId);
                    Debug.Log($"* Un cuerpo de personaje fue inyectado para el Cliente ID {clientId}");

                    PlayerState estadoP = playerInstance.GetComponent<PlayerState>();
                    if (estadoP != null)
                    {
                        if (clientId == wolfId) estadoP.isWolf.Value = true;
                        
                        // [NUEVO] Asignar el rol de aldea al PlayerState
                        estadoP.rolAldea.Value = rolAsignado;

                        // [NUEVO] Asignar el nickname del jugador
                        string nickname = ObtenerNicknameCliente(clientId);
                        estadoP.playerName.Value = new FixedString32Bytes(nickname);

                        // [NUEVO] Asignar una casa al jugador
                        if (indexCasa < casasDisponibles.Count)
                        {
                            HouseController casaA = casasDisponibles[indexCasa];
                            casaA.HouseOwnerClientId.Value = clientId;
                            estadoP.myHouseId.Value = casaA.NetworkObjectId;
                            indexCasa++;
                            Debug.Log($"* Casa {casaA.NetworkObjectId} asignada al Cliente {clientId}");
                        }

                        ClientRpcParams enviarSoloAlDueño = new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
                        };
                        estadoP.ForzarTeletransporteClientRpc(spawnPos, spawnRot, enviarSoloAlDueño);
                    }
                }
            }

            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
            };

            RecibirRolClientRpc(assignedRoleText, rpcParams);
        }

        // Al terminar de repartir roles, el Servidor empuja el reloj hacia el DÍA número 1
        currentPhase.Value = GamePhase.Dia;
        tiempoRestanteFase = tiempoDiaSegundos;

        // ── Asignamos las Tareas Iniciales (Día 1) con un frame de retardo ──
        // [FIX] SpawnAsPlayerObject de NGO necesita al menos un frame para registrar los
        // PlayerObjects en ConnectedClients. Si llamamos ServerAssignQuest en el mismo frame,
        // GetComponent<PlayerQuestTracker> puede devolver null y las misiones no se asignan.
        if (IsServer)
        {
            StartCoroutine(AsignarMisionesConRetardo(clientIds));
        }
    }

    [ClientRpc]
    private void RecibirRolClientRpc(string rol, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"<color=cyan>¡El servidor me ha dicho en secreto que soy:</color> <color=red><b>{rol}</b></color>!");
        
        GameplayUI gameUI = GetGameplayUI();
        if (gameUI != null)
        {
            gameUI.MostrarRol($"ERES {(rol == "Lobo" ? "EL LOBO" : "UN ALDEANO")}");
        }
    }

    // --- LÓGICA DE VICTORIA CADA VEZ QUE ALGUIEN MUERE ---
    public void CheckWinConditions()
    {
        if (!IsServer || partidaTerminada) return;

        int lobosVivos = 0;
        int aldeanosVivos = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Value.PlayerObject == null) continue;
            PlayerState ps = client.Value.PlayerObject.GetComponent<PlayerState>();
            if (ps != null && !ps.isDead.Value)
            {
                if (ps.isWolf.Value) lobosVivos++;
                else aldeanosVivos++;
            }
        }

        Debug.Log($"[Juez Divino] Censo actual: {lobosVivos} Lobos vivos | {aldeanosVivos} Aldeanos vivos.");

        if (lobosVivos == 0)
        {
            FinalizarPartidaCorrutina("¡LOS ALDEANOS GANAN!");
        }
        else if (lobosVivos >= aldeanosVivos)
        {
            FinalizarPartidaCorrutina("¡LOS LOBOS GANAN!");
        }
    }

    private void FinalizarPartidaCorrutina(string mensajeGanador)
    {
        partidaTerminada = true;
        Debug.Log($"<color=yellow>[GAME OVER] {mensajeGanador}</color>");
        
        // Bloqueamos el ratón para que los vivos no intenten hacer nada raro mientras se cierra.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        AnunciarVictoriaClientRpc(mensajeGanador);
    }

    [ClientRpc]
    private void AnunciarVictoriaClientRpc(string mensajeGanador)
    {
        GameplayUI gameUI = GetGameplayUI();
        if (gameUI != null)
        {
            gameUI.MostrarVictoria(mensajeGanador);
        }

        // ¡AHORA TODOS LOS CLIENTES EJECUTAN ESTO AL MISMO TIEMPO!
        StartCoroutine(EsperaYRegresaAlMenu());
    }

    private IEnumerator EsperaYRegresaAlMenu()
    {
        // 5 Segundos Dramáticos de celebración o llanto
        yield return new WaitForSeconds(5f);

        // Limpieza Cibernética Obligatoria para reiniciar los puertos
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject); // Borramos el gestor viejo
        }
        
        Debug.Log("Saliendo de la partida...");
        
        // PRIMERO cargamos la escena nueva.
        SceneManager.LoadScene("Scene_Menu"); 

        // SEGUNDO Inmolamos este script. (Si lo hacemos antes de cargar, 
        // la Corrutina muere y el LoadScene nunca se ejecuta).
        Destroy(gameObject);
    }

    // ==========================================
    // SISTEMA DE MISIONES BÁSICAS (QuestSystem)
    // ==========================================

    /// <summary>
    /// [SERVER] Espera ACTIVAMENTE hasta que todos los PlayerObjects estén registrados en NGO.
    /// WaitForEndOfFrame no es suficiente: NGO propaga los PlayerObjects en múltiples frames de red.
    /// </summary>
    private IEnumerator AsignarMisionesConRetardo(List<ulong> jugadoresEsperados)
    {
        float tiempoLimite = 5f; // Tiempo máximo de espera en segundos
        float tiempoEsperado = 0f;

        Debug.Log($"[GameManager] Esperando que NGO registre {jugadoresEsperados.Count} PlayerObjects...");

        while (tiempoEsperado < tiempoLimite)
        {
            yield return null; // Esperar el siguiente frame de Unity
            tiempoEsperado += Time.deltaTime;

            // Contar cuántos clientes ya tienen PlayerObject válido
            int listos = 0;
            foreach (ulong id in jugadoresEsperados)
            {
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(id, out var client)
                    && client.PlayerObject != null)
                {
                    listos++;
                }
            }

            // Cuando todos estén listos, asignamos las misiones
            if (listos >= jugadoresEsperados.Count)
            {
                Debug.Log($"[GameManager] Todos los PlayerObjects listos tras {tiempoEsperado:F2}s. Asignando misiones...");
                AsignarMisionesBasicasADiaNuevo(jugadoresEsperados);
                yield break;
            }
        }

        // Timeout: asignar igualmente con lo que haya disponible
        Debug.LogWarning($"[GameManager] Timeout ({tiempoLimite}s) esperando PlayerObjects. Asignando misiones con los jugadores disponibles...");
        AsignarMisionesBasicasADiaNuevo(jugadoresEsperados);
    }

    private void AsignarMisionesBasicasADiaNuevo(List<ulong> jugadoresVivosIds)
    {
        if (!IsServer || Core.QuestSystem.QuestManager.Instance == null) return;

        // Recopilamos todos los interactuables de la escena una sola vez para pasarlos a la validación
        Core.Environment.UniversalQuestInteractable[] todosLosInteractuables =
            FindObjectsByType<Core.Environment.UniversalQuestInteractable>(FindObjectsInactive.Include);

        Core.Environment.InteractivableMisionUniversal[] todosLosInteractuablesViejos =
            FindObjectsByType<Core.Environment.InteractivableMisionUniversal>(FindObjectsInactive.Include);

        // 1. Obtener todas las misiones básicas registradas Y que tengan objetos en escena
        // [FIX] Filtrar misiones cuyos pasos no tienen ningún interactuable colocado en la escena.
        // Así el jugador nunca recibe una misión imposible de completar.
        List<Core.QuestSystem.QuestData> basicQuests = new List<Core.QuestSystem.QuestData>();
        foreach (var q in Core.QuestSystem.QuestManager.Instance.GetAllAvailableQuests())
        {
            if (q != null && q.esMisionBasica && MisionTieneInteractuablesEnEscena(q, todosLosInteractuables, todosLosInteractuablesViejos))
            {
                basicQuests.Add(q);
            }
        }

        Debug.Log($"[GameManager] Misiones básicas válidas (con objetos en escena): {basicQuests.Count}");

        if (basicQuests.Count == 0)
        {
            Debug.LogWarning("[GameManager] No hay misiones básicas completables. Verifica que los QuestZonePoint/BasicQuestInteractable estén colocados en la escena con la zona correcta.");
            return;
        }

        // 2. Diagnóstico: mostrar cuántos clientes hay y si tienen PlayerObject
        Debug.Log($"[GameManager] ConnectedClients.Count = {NetworkManager.Singleton.ConnectedClients.Count}");
        foreach (var entry in NetworkManager.Singleton.ConnectedClients)
        {
            bool tienePlayerObj = entry.Value.PlayerObject != null;
            bool tieneTracker = tienePlayerObj && entry.Value.PlayerObject.GetComponent<Core.QuestSystem.PlayerQuestTracker>() != null;
            Debug.Log($"  Cliente {entry.Key}: PlayerObject={tienePlayerObj} | Tracker={tieneTracker}");
        }

        // 3. Asignar misión a cada cliente vivo en jugadoresVivosIds
        int asignadas = 0;
        foreach (ulong clientId in jugadoresVivosIds)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var clientEntry))
            {
                Debug.LogWarning($"[GameManager] Cliente {clientId} no se encuentra en ConnectedClients.");
                continue;
            }

            if (clientEntry.PlayerObject == null)
            {
                Debug.LogWarning($"[GameManager] Cliente {clientId} no tiene PlayerObject todavía. Saltando.");
                continue;
            }

            PlayerState ps = clientEntry.PlayerObject.GetComponent<PlayerState>();
            if (ps == null)
            {
                Debug.LogWarning($"[GameManager] Cliente {clientId}: PlayerObject existe pero SIN PlayerState.");
                continue;
            }
            if (ps.isDead.Value) continue; // Muertos no reciben misiones

            Core.QuestSystem.PlayerQuestTracker tracker = clientEntry.PlayerObject.GetComponent<Core.QuestSystem.PlayerQuestTracker>();
            if (tracker == null)
            {
                Debug.LogWarning($"[GameManager] Cliente {clientId}: PlayerObject existe pero SIN PlayerQuestTracker.");
                continue;
            }

            if (!string.IsNullOrEmpty(tracker.currentQuestID.Value.ToString().TrimEnd('\0')))
            {
                Debug.Log($"[GameManager] Cliente {clientId} ya tiene misión activa: {tracker.currentQuestID.Value.ToString().TrimEnd('\0')}. No se sobreescribe.");
                continue;
            }

            tracker.ResetDailyQuests(); // Resetea las completadas hoy a 0
            string questId = GetRandomBasicQuestID();
            if (!string.IsNullOrEmpty(questId))
            {
                tracker.ServerAssignQuest(questId);
                asignadas++;
                Debug.Log($"[GameManager] ✅ Misión básica 1/2 '{questId}' asignada al jugador {clientId} (isWolf={ps.isWolf.Value})");
            }
        }

        Debug.Log($"[GameManager] Total misiones básicas asignadas este día: {asignadas}");
    }

    public string GetRandomBasicQuestID(string excludeQuestID = "")
    {
        if (Core.QuestSystem.QuestManager.Instance == null) return "";

        Core.Environment.UniversalQuestInteractable[] todosLosInteractuables =
            FindObjectsByType<Core.Environment.UniversalQuestInteractable>(FindObjectsInactive.Include);

        Core.Environment.InteractivableMisionUniversal[] todosLosInteractuablesViejos =
            FindObjectsByType<Core.Environment.InteractivableMisionUniversal>(FindObjectsInactive.Include);

        List<Core.QuestSystem.QuestData> basicQuests = new List<Core.QuestSystem.QuestData>();
        foreach (var q in Core.QuestSystem.QuestManager.Instance.GetAllAvailableQuests())
        {
            if (q != null && q.esMisionBasica && MisionTieneInteractuablesEnEscena(q, todosLosInteractuables, todosLosInteractuablesViejos))
            {
                if (string.IsNullOrEmpty(excludeQuestID) || q.questID != excludeQuestID)
                {
                    basicQuests.Add(q);
                }
            }
        }

        // Fallback: si al excluir no queda ninguna, añadimos todas de nuevo
        if (basicQuests.Count == 0 && !string.IsNullOrEmpty(excludeQuestID))
        {
            foreach (var q in Core.QuestSystem.QuestManager.Instance.GetAllAvailableQuests())
            {
                if (q != null && q.esMisionBasica && MisionTieneInteractuablesEnEscena(q, todosLosInteractuables, todosLosInteractuablesViejos))
                {
                    basicQuests.Add(q);
                }
            }
        }

        if (basicQuests.Count == 0) return "";

        int randomIndex = Random.Range(0, basicQuests.Count);
        return basicQuests[randomIndex].questID;
    }

    /// <summary>
    /// [SERVER] Verifica que cada paso de la misión tenga al menos un BasicQuestInteractable,
    /// QuestZonePoint o UniversalQuestInteractable en la escena con el MaterialType y ZoneID correctos.
    /// Si falta algún paso, la misión se descarta del sorteo para evitar misiones imposibles.
    /// </summary>
    private bool MisionTieneInteractuablesEnEscena(
        Core.QuestSystem.QuestData quest,
        Core.Environment.UniversalQuestInteractable[] todosLosInteractuables,
        Core.Environment.InteractivableMisionUniversal[] todosLosInteractuablesViejos)
    {
        if (quest.pasos == null || quest.pasos.Count == 0) return false;

        foreach (var paso in quest.pasos)
        {
            bool pasoTieneObjeto = false;

            // 1. Buscar en los UniversalQuestInteractables nuevos
            foreach (var interactable in todosLosInteractuables)
            {
                if (interactable.zonaUbicacion == paso.zonaRequerida)
                {
                    // Reglas de coincidencia permitiendo:
                    // - Coincidencia exacta de material
                    // - O que el paso requiera "Cualquiera"
                    // - O que el interactuable en el escenario esté configurado como "Cualquiera"
                    // - O que el interactuable tenga asignada explícitamente esta misión en su QuestData
                    if (interactable.materialAsignado == paso.materialRequerido ||
                        paso.materialRequerido == Core.Enums.MaterialType.Cualquiera ||
                        paso.materialRequerido == Core.Enums.MaterialType.Ninguno || // Si el paso no requiere material, sirve cualquier interactuable en la zona
                        interactable.materialAsignado == Core.Enums.MaterialType.Cualquiera ||
                        (interactable.questData != null && interactable.questData.questID == quest.questID))
                    {
                        pasoTieneObjeto = true;
                        break;
                    }
                }
            }

            // 2. Fallback: Buscar en los interactuables antiguos (InteractivableMisionUniversal)
            if (!pasoTieneObjeto)
            {
                foreach (var interactableViejo in todosLosInteractuablesViejos)
                {
                    if (interactableViejo.idMision == quest.questID)
                    {
                        pasoTieneObjeto = true;
                        break;
                    }
                }
            }

            if (!pasoTieneObjeto)
            {
                Debug.LogWarning($"[GameManager] Misión '{quest.questID}' descartada: " +
                    $"no hay ningún objeto '{paso.materialRequerido}' en zona '{paso.zonaRequerida}' en la escena.");
                return false;
            }
        }

        return true; // Todos los pasos tienen objeto en escena
    }


    // ==========================================
    // SISTEMA DE EXPULSIÓN (KICK)
    // ==========================================

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void KickPlayerServerRpc(ulong clientIdToKick, RpcParams rpcParams = default)
    {
        // Solo el servidor/Host real tiene permiso para ejecutar la patada
        if (!IsServer) return;

        // Validamos que el emisor de la petición sea el Servidor/Host
        if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId)
        {
            Debug.LogWarning($"[GameManager] Cliente {rpcParams.Receive.SenderClientId} intentó expulsar al jugador {clientIdToKick} sin ser el Host.");
            return;
        }

        // No puedes patearte a ti mismo (el Host)
        if (clientIdToKick == NetworkManager.ServerClientId) return;

        Debug.Log($"<color=red>[SERVIDOR] Expulsando al jugador {clientIdToKick} por orden del Host.</color>");
        
        // Desconectamos al cliente de la red de Netcode
        NetworkManager.Singleton.DisconnectClient(clientIdToKick);
    }
}

