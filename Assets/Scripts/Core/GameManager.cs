using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Reloj del Servidor")]
    private float tiempoRestanteFase = 0f;
    [Tooltip("Duración en segundos de la fase de Día")]
    [SerializeField] private float tiempoDiaSegundos = 60f;
    [Tooltip("Duración en segundos de la fase de Noche (Asesinatos)")]
    [SerializeField] private float tiempoNocheSegundos = 30f;
    [Tooltip("Duración en segundos de la Asamblea de Votación")]
    [SerializeField] private float tiempoVotacionSegundos = 30f;
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
        
        // Disparo inicial por si el cliente cargó la escena unos segundos tarde
        AlCambiarDeFase(currentPhase.Value, currentPhase.Value);

        // [Nuevo] Nos preparamos para el peor caso: que el Host apague su PC a mitad de partida
        if (IsClient)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += EnDesconexionInesperada;
        }
    }

    public override void OnNetworkDespawn()
    {
        currentPhase.OnValueChanged -= AlCambiarDeFase;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= EnDesconexionInesperada;
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
                    // Desactivar el CharacterController para dejarlo teletransportar libremente
                    CharacterController cc = netObj.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;

                    Transform spawnDestino = spawnManager != null ? spawnManager.GetNextSpawnPoint() : null;
                    if (spawnDestino != null)
                    {
                        netObj.transform.position = spawnDestino.position;
                        netObj.transform.rotation = spawnDestino.rotation;
                    }

                    if (cc != null) cc.enabled = true;
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
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(votanteId, out var infoCliente))
        {
            PlayerState ps = infoCliente.PlayerObject.GetComponent<PlayerState>();
            if (ps == null || ps.isDead.Value) return;
        }

        // Anotamos en la lista oficial que esta persona ya no puede volver a pulsar botones
        jugadoresQueVotaron.Add(votanteId);

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
        if (faseNueva == GamePhase.Votacion)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                MonoBehaviour tpc = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent("ThirdPersonController") as MonoBehaviour;
                if (tpc != null) tpc.enabled = false;

                // [Fix del Mouse] StarterAssets secuestra el ratón. Tenemos que indicarle a su script de Inputs que lo suelte.
                MonoBehaviour inputs = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent("StarterAssetsInputs") as MonoBehaviour;
                if (inputs != null) 
                {
                    // Usamos reflexión simple o campos si están públicos (cursorLocked, cursorInputForLook)
                    var type = inputs.GetType();
                    var cursorLockedProp = type.GetField("cursorLocked");
                    var cursorInputProp = type.GetField("cursorInputForLook");
                    if (cursorLockedProp != null) cursorLockedProp.SetValue(inputs, false);
                    if (cursorInputProp != null) cursorInputProp.SetValue(inputs, false);
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
                    MonoBehaviour tpc = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent("ThirdPersonController") as MonoBehaviour;
                    if (tpc != null) tpc.enabled = true;

                    // Le devolvemos el secuestro del ratón al StarterAssets
                    MonoBehaviour inputs = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent("StarterAssetsInputs") as MonoBehaviour;
                    if (inputs != null) 
                    {
                        var type = inputs.GetType();
                        var cursorLockedProp = type.GetField("cursorLocked");
                        var cursorInputProp = type.GetField("cursorInputForLook");
                        if (cursorLockedProp != null) cursorLockedProp.SetValue(inputs, true);
                        if (cursorInputProp != null) cursorInputProp.SetValue(inputs, true);
                    }
                }
            }

            VotingUI votingUI = FindFirstObjectByType<VotingUI>(FindObjectsInactive.Include);
            if (votingUI != null) votingUI.OcultarPantallaVotacion();
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
        SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();

        foreach (ulong clientId in clientIds)
        {
            string assignedRole = (clientId == wolfId) ? "Lobo" : "Aldeano";

            if (playerPrefab != null)
            {
                Transform spawnPoint = spawnManager != null ? spawnManager.GetNextSpawnPoint() : null;
                Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : new Vector3(0, 10, 0); // Lo ponemos alto para que caiga si falta
                Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

                GameObject playerInstance = Instantiate(playerPrefab, spawnPos, spawnRot);
                NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
                
                if (netObj != null)
                {
                    netObj.SpawnAsPlayerObject(clientId);
                    Debug.Log($"* Un cuerpo de personaje fue inyectado para el Cliente ID {clientId}");

                    // [V0.3: ADN Autorizado] Le asignamos al PlayerState si este sujeto es Lobo.
                    PlayerState estadoP = playerInstance.GetComponent<PlayerState>();
                    if (estadoP != null && clientId == wolfId)
                    {
                        estadoP.isWolf.Value = true;
                    }
                }
            }

            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
            };

            RecibirRolClientRpc(assignedRole, rpcParams);
        }

        // Al terminar de repartir roles, el Servidor empuja el reloj hacia el DÍA número 1
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
}
