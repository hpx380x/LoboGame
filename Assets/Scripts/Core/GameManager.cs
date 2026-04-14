using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core.Enums;


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
    [Tooltip("N\u00famero de tareas que se asignan a cada jugador por d\u00eda.")]
    [SerializeField] private int tareasPerJugador = 2;

    private Dictionary<ulong, int> conteoVotos = new Dictionary<ulong, int>();
    private HashSet<ulong> jugadoresQueVotaron = new HashSet<ulong>();

    private bool isStarting = false;
    private bool esPrimerDia = true;
    private bool partidaTerminada = false;

    private void Awake()
    {
        // [Regla Especial] Hacemos que la "nave" del GameManager sea indestructible
        // para que sobreviva al agujero negro del cambio de escena y llegue a la Tierra (Scene_Gameplay)
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.DestroyWithScene = false;
        }
        DontDestroyOnLoad(this.gameObject);
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

    private void ActualizarListaJugadoresServidor(ulong clientId)
    {
        // Regla 3: Guard de Servidor
        if (!IsServer) return;
        
        string nuevaLista = "";
        int conteo = 0;
        
        foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            conteo++;
            if (id == NetworkManager.Singleton.LocalClientId)
            {
                nuevaLista += $"- Jugador {id} (HOST)\n";
            }
            else
            {
                nuevaLista += $"- Jugador {id}\n";
            }
        }
        
        string cabecera = $"<color=yellow>JUGADORES EN LA SALA ({conteo}/10):</color>\n\n";
        listaJugadoresNetwork.Value = new FixedString4096Bytes(cabecera + nuevaLista);
    }

    private void Update()
    {
        // El reloj de la partida SOLO existe físicamente en el ordenador del Servidor
        if (!IsServer || partidaTerminada) return;

        if (currentPhase.Value == GamePhase.Dia || currentPhase.Value == GamePhase.Noche || currentPhase.Value == GamePhase.Votacion)
        {
            tiempoRestanteFase -= Time.deltaTime;

            if (tiempoRestanteFase <= 0f)
            {
                if (currentPhase.Value == GamePhase.Dia)
                {
                    if (esPrimerDia)
                    {
                        Debug.Log("[Servidor] Fin del Primer Día. No hay votación hoy, cae la Noche directa.");
                        esPrimerDia = false; // Quitamos el candado para los días futuros
                        currentPhase.Value = GamePhase.Noche;
                        tiempoRestanteFase = tiempoNocheSegundos;
                    }
                    else
                    {
                        currentPhase.Value = GamePhase.Votacion;
                        tiempoRestanteFase = tiempoVotacionSegundos;
                        PrepararVotacion();
                    }
                }
                else if (currentPhase.Value == GamePhase.Votacion)
                {
                    ResolverVotacion();
                    currentPhase.Value = GamePhase.Noche;
                    tiempoRestanteFase = tiempoNocheSegundos;
                }
                else if (currentPhase.Value == GamePhase.Noche)
                {
                    currentPhase.Value = GamePhase.Dia;
                    tiempoRestanteFase = tiempoDiaSegundos;
                }
            }
        }
    }

    private void PrepararVotacion()
    {
        Debug.Log("[Servidor] Iniciando Asamblea de Votación. Teletransportando jugadores vivos...");
        conteoVotos.Clear();
        jugadoresQueVotaron.Clear();

        SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();

        foreach (var clientInfo in NetworkManager.Singleton.ConnectedClients)
        {
            NetworkObject netObj = clientInfo.Value.PlayerObject;
            if (netObj != null)
            {
                PlayerState ps = netObj.GetComponent<PlayerState>();
                if (ps != null && !ps.isDead.Value)
                {
                    // Usamos la nueva función circular de puntos de reunión
                    Transform spawnDestino = spawnManager != null ? spawnManager.GetNextVotingSpawnPoint() : null;
                    if (spawnDestino != null)
                    {
                        // [CRÍTICO] Ya no empujamos el transform desde el servidor, porque su computadora local 
                        // nos negaría el movimiento la mayoría de veces debido a los pre-cálculos del motor físico.
                        // En su lugar, le emitimos una ORDEN MILITAR para que su propia computadora haga el viaje voluntariamente:
                        
                        ClientRpcParams enviarSoloAlDueño = new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientInfo.Value.ClientId } }
                        };
                        
                        ps.ForzarTeletransporteClientRpc(spawnDestino.position, spawnDestino.rotation, enviarSoloAlDueño);
                    }
                }
            }
        }
    }

    private void ResolverVotacion()
    {
        Debug.Log("[Servidor] Tiempo agotado. Resolviendo votación...");
        
        // Si nadie voto absolutamente por nadie, continuamos
        if (conteoVotos.Count == 0)
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
            Debug.Log($"[Servidor] Jugador {votanteId} emitió una omisión de voto (Skip).");
        }
    }

    private void AlCambiarDeFase(GamePhase faseAntigua, GamePhase faseNueva)
    {
        GameplayUI gameUI = FindFirstObjectByType<GameplayUI>();
        if (gameUI != null) gameUI.ActualizarFase(faseNueva.ToString());

        Light sol = FindFirstObjectByType<Light>();
        if (sol != null && sol.type == LightType.Directional)
        {
            if (faseNueva == GamePhase.Noche) sol.intensity = 0.05f;
            else if (faseNueva == GamePhase.Dia) sol.intensity = 1.0f;
            else if (faseNueva == GamePhase.Votacion) sol.intensity = 0.5f; // Atardecer de asamblea
        }

        // --- Manejo del Ratón y Bloqueo de Movimiento ---
        // [Fix] Aseguramos que el EventSystem y Canvas estén activos al cambiar de fase
        var es = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
        if (es != null) es.gameObject.SetActive(true);
        
        var canvasGameplay = FindFirstObjectByType<GameplayUI>(FindObjectsInactive.Include);
        if (canvasGameplay != null) canvasGameplay.gameObject.SetActive(true);
        if (faseNueva == GamePhase.Votacion)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                // [Fix] Uso de tipo real para evitar fallos de namespace en GetComponent
                var tpc = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<StarterAssets.ThirdPersonController>();
                if (tpc != null) tpc.enabled = false;

                // [Fix del Mouse] StarterAssets secuestra el ratón. Tenemos que indicarle a su script de Inputs que lo suelte.
                var inputs = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<StarterAssets.StarterAssetsInputs>();
                if (inputs != null) 
                {
                    inputs.cursorLocked = false;
                    inputs.cursorInputForLook = false;
                    // Forzamos el desbloqueo físico del cursor para la UI de votación
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            VotingUI votingUI = FindFirstObjectByType<VotingUI>(FindObjectsInactive.Include);
            if (votingUI != null) votingUI.MostrarPantallaVotacion();
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
                    // [Fix] Uso de tipo real
                    var tpc = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<StarterAssets.ThirdPersonController>();
                    if (tpc != null) tpc.enabled = true;

                    // Le devolvemos el secuestro del ratón al StarterAssets
                    var inputs = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<StarterAssets.StarterAssetsInputs>();
                    if (inputs != null) 
                    {
                        inputs.cursorLocked = true;
                        inputs.cursorInputForLook = true;
                    }
                }
            }

            VotingUI votingUI = FindFirstObjectByType<VotingUI>(FindObjectsInactive.Include);
            if (votingUI != null) votingUI.OcultarPantallaVotacion();
        }

        // ── Sistema de tareas: al comenzar un nuevo D\u00eda (Noche→Dia), asignar 2 tareas nuevas ──
        // Solo el servidor tiene autoridad para repartir tareas.
        if (IsServer && faseAntigua == GamePhase.Noche && faseNueva == GamePhase.Dia)
        {
            List<ulong> jugadoresVivos = new List<ulong>();
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                PlayerState ps = client.Value.PlayerObject?.GetComponent<PlayerState>();
                if (ps != null && !ps.isDead.Value)
                {
                    ps.tareasCompletadasHoy.Value = 0; // Reset contador del d\u00eda
                    jugadoresVivos.Add(client.Key);
                }
            }
            AsignarTareasATodosLosJugadores(jugadoresVivos);
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
            NetworkObject[] todosLosObjetosEnRed = FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

        SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();

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

        // ── Asignamos las Tareas Iniciales (Día 1) ──
        if (IsServer)
        {
            AsignarTareasATodosLosJugadores(clientIds);
        }

        // Al terminar de repartir roles y tareas, el Servidor empuja el reloj hacia el DÍA número 1
        currentPhase.Value = GamePhase.Dia;
        tiempoRestanteFase = tiempoDiaSegundos;
    }

    [ClientRpc]
    private void RecibirRolClientRpc(string rol, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"<color=cyan>¡El servidor me ha dicho en secreto que soy:</color> <color=red><b>{rol}</b></color>!");
        
        GameplayUI gameUI = FindFirstObjectByType<GameplayUI>();
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
        GameplayUI gameUI = FindFirstObjectByType<GameplayUI>();
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
    // SISTEMA DE TAREAS (V1.3)
    // ==========================================

    [Tooltip("La lista en memoria de todos los TaskPoint de la escena (solo servidor).")]
    private List<TaskPoint> todosLosTaskPoints = new List<TaskPoint>();

    private void AsignarTareasATodosLosJugadores(List<ulong> jugadoresVivosIds)
    {
        if (!IsServer) return;

        // 1. Recolectar o actualizar la lista de todas las tareas del mapa
        TaskPoint[] puntosFisicos = FindObjectsByType<TaskPoint>(FindObjectsSortMode.None);
        todosLosTaskPoints = new List<TaskPoint>(puntosFisicos);

        if (todosLosTaskPoints.Count == 0)
        {
            Debug.LogWarning("[Server] No se encontraron TaskPoints en la escena. Nadie recibirá tareas.");
            return;
        }

        Debug.Log($"[Server] Repartiendo {tareasPerJugador} tareas por jugador (Total disponibles: {todosLosTaskPoints.Count}).");

        foreach (ulong clientId in jugadoresVivosIds)
        {
            // Verificación de seguridad
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var clientInfo)) continue;
            PlayerState ps = clientInfo.PlayerObject?.GetComponent<PlayerState>();
            if (ps == null || ps.isDead.Value) continue;

            // 2. Barajar la lista de tareas para este jugador específico
            List<TaskPoint> tareasBarajadas = new List<TaskPoint>(todosLosTaskPoints);
            // Simple Fisher-Yates shuffle
            for (int i = tareasBarajadas.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                (tareasBarajadas[i], tareasBarajadas[r]) = (tareasBarajadas[r], tareasBarajadas[i]);
            }

            // 3. Seleccionar las primeras N tareas
            int cantidadAAsignar = Mathf.Min(tareasPerJugador, tareasBarajadas.Count);
            List<string> jsonInfos = new List<string>();

            for (int i = 0; i < cantidadAAsignar; i++)
            {
                TaskPoint tp = tareasBarajadas[i];
                TaskInfo info = new TaskInfo(tp.taskId, tp.nombreTarea);
                jsonInfos.Add(JsonUtility.ToJson(info));
            }

            // 4. Empaquetar el array y enviárselo secretamente SOLO al dueño
            string jsonArray = "[" + string.Join(",", jsonInfos) + "]";

            ClientRpcParams parametrosPrivados = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
            };

            RecibirAsignacionTareasClientRpc(jsonArray, ps.isWolf.Value, parametrosPrivados);
        }
    }

    [ClientRpc]
    private void RecibirAsignacionTareasClientRpc(string tareasJsonArray, bool esLobo, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"<color=cyan>[Red]</color> El servidor me acaba de entregar mis tareas de hoy: {tareasJsonArray}");

        // 1. Deserializar el JSON trampa de Unity (Unity no soporta arrays puros bien usando JsonUtility, necesitamos un wrapper o un truco. Usaremos un parseo basico por simplicidad si se empacó manual)
        // Para simplificar, asumimos que sabemos parsearlo o usamos un array Wrapper (aquí usaremos un array simple deserializando objeto por objeto).
        // Como JsonUtility es malo con arrays raiz, parseémoslo cortando strings.
        
        // --- PARSEO RUDIMENTARIO ---
        List<TaskInfo> listaParseada = new List<TaskInfo>();
        tareasJsonArray = tareasJsonArray.Trim('[', ']'); // "["{...}","{...}"]" => "{...}","{...}"
        
        // Separamos por la cadena "," (incluyendo comillas si las hay). Lo más seguro es usar un wrapper real, pero para el prototipo servirá.
        // Mejor si mandamos el array como strings individuales... pero vamos a solucionarlo usando un struct Wrapper interno.
        
        // Hack rapido:
        string[] objetosJson = tareasJsonArray.Split(new string[] { "},{" }, System.StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < objetosJson.Length; i++)
        {
            string objStr = objetosJson[i];
            if (!objStr.StartsWith("{")) objStr = "{" + objStr;
            if (!objStr.EndsWith("}")) objStr = objStr + "}";
            
            TaskInfo tInfo = JsonUtility.FromJson<TaskInfo>(objStr);
            if (tInfo != null && !string.IsNullOrEmpty(tInfo.taskId))
            {
                listaParseada.Add(tInfo);
            }
        }

        // 2. Avisarle a nuestro HUD
        GameplayUI gameUI = FindFirstObjectByType<GameplayUI>();
        if (gameUI != null)
        {
            gameUI.ActualizarListaTareas(listaParseada, esLobo);
        }

        // 3. Avisarle a las "estaciones físicas" (TaskPoints) en la escena
        // Para que se enciendan/apaguen localmente (Brillo dorado, etc.)
        TaskPoint[] todosLosPuntosL = FindObjectsByType<TaskPoint>(FindObjectsSortMode.None);
        foreach (TaskPoint tp in todosLosPuntosL)
        {
            bool meTocaAmi = listaParseada.Exists(t => t.taskId == tp.taskId);
            tp.MarcarComoAsignada(meTocaAmi);
        }
    }
    // ==========================================
    // SISTEMA DE EXPULSIÓN (KICK)
    // ==========================================

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void KickPlayerServerRpc(ulong clientIdToKick)
    {
        // Solo el servidor/Host real tiene permiso para ejecutar la patada
        if (!IsServer) return;

        // No puedes patearte a ti mismo (el Host)
        if (clientIdToKick == NetworkManager.Singleton.LocalClientId) return;

        Debug.Log($"<color=red>[SERVIOR] Expulsando al jugador {clientIdToKick} por orden del Host.</color>");
        
        // Desconectamos al cliente de la red de Netcode
        NetworkManager.Singleton.DisconnectClient(clientIdToKick);
    }
}
