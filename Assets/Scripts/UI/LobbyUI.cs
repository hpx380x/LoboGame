using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;

public class LobbyUI : MonoBehaviour
{
    [Header("Paneles (Screens)")]
    [Tooltip("Panel principal con los botones de Host, Client y Quit")]
    [SerializeField] private GameObject mainMenuPanel;
    [Tooltip("Panel de la sala de espera (Lobby) con la lista de jugadores")]
    [SerializeField] private GameObject roomPanel;
    [Tooltip("Panel con las opciones gráficas y configuración")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Main Menu Elements")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [Tooltip("Botón para cerrar el juego")]
    [SerializeField] private Button quitButton;
    [Tooltip("Botón para abrir el panel de Ajustes")]
    [SerializeField] private Button settingsButton;
    [Tooltip("Si se marca, la partida no saldrá en el Navegador de Servidores")]
    [SerializeField] private Toggle privateGameToggle;
    [Tooltip("Campo para escribir tu nombre antes de entrar")]
    [SerializeField] private TMP_InputField nicknameInputField;

    [Header("Join Panel & Server Browser")]
    [SerializeField] private GameObject joinPanel;
    [Tooltip("El recuadro blanco donde los amigos escriben el código para unirse a partidas privadas")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button joinWithCodeButton;
    [SerializeField] private Button refreshListaButton;
    [SerializeField] private Button joinPanelBackButton;
    [SerializeField] private Transform serverListContent;
    [SerializeField] private GameObject serverEntryPrefab;

    [Header("Room Elements")]
    [Tooltip("El texto donde aparecerá el código en mayúsculas para que el Host se lo dicte a sus amigos")]
    [SerializeField] private TextMeshProUGUI joinCodeText;
    [Tooltip("Texto grande de UI donde pintaremos quién entró a la sala")]
    [SerializeField] private TextMeshProUGUI playerListText;
    [Tooltip("Botón que solo verá el Host para viajar a la escena de Gameplay")]
    [SerializeField] private Button startGameButton;
    [Tooltip("Botón para desconectarse y volver al menú principal")]
    [SerializeField] private Button leaveButton;

    [Header("Settings Elements")]
    [Tooltip("Botón para cerrar el menú de ajustes y volver al menú principal")]
    [SerializeField] private Button settingsBackButton;

    [Header("Sistemas Inyectados")]
    [Tooltip("Gestor de partida inyectado para escuchar eventos sin usar Singletons")]
    [SerializeField] private GameManager gameManager;
    // [Regla 4] Evitamos NetworkManager.Singleton. 
    // [Fix] Oculto en el inspector para evitar el crash de Odin (TypeLoadException)
    [HideInInspector]
    [SerializeField] private NetworkManager networkManager;

    [Header("Lobby Atmosférico")]
    [SerializeField] private LobbyCameraManager cameraManager;
    [Tooltip("Gestor de spawn del personaje local en la sala de espera")]
    [SerializeField] private LobbyPlayerSpawner lobbyPlayerSpawner;

    private Lobby _hostLobby;
    private float _heartbeatTimer;

    private void Awake()
    {
        if (networkManager == null)
            networkManager = FindAnyObjectByType<NetworkManager>();
    }

    private void Update()
    {
        ManejarLobbyHeartbeat();
    }

    private async void ManejarLobbyHeartbeat()
    {
        if (_hostLobby != null)
        {
            _heartbeatTimer -= Time.deltaTime;
            if (_heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 15f;
                _heartbeatTimer = heartbeatTimerMax;

                try
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(_hostLobby.Id);
                }
                catch (LobbyServiceException e)
                {
                    Debug.Log($"[Lobby Heartbeat Error]: {e}");
                }
            }
        }
    }

    private async void Start()
    {
        Debug.Log("[LobbyUI] Iniciando Sistema de Nube Mundial...");
        
        // Desboqueamos forzosamente el ratón
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Cargar nickname guardado
        string currentNick = GetSavedNickname();
        if (nicknameInputField != null)
        {
            nicknameInputField.text = currentNick;
            nicknameInputField.onValueChanged.AddListener((val) => {
                string nameToSave = val.Trim();
                if (nameToSave.Length > 25) nameToSave = nameToSave.Substring(0, 25);
                PlayerPrefs.SetString("PlayerNickname", nameToSave);
                PlayerPrefs.Save();
            });
        }

        if (hostButton != null) hostButton.onClick.AddListener(OnHostButtonClicked);
        if (clientButton != null) clientButton.onClick.AddListener(MostrarJoinPanel);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (leaveButton != null) leaveButton.onClick.AddListener(LeaveRoom);
        
        if (settingsButton != null) settingsButton.onClick.AddListener(MostrarSettingsPanel);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(MostrarMainMenu);

        if (joinWithCodeButton != null) joinWithCodeButton.onClick.AddListener(OnJoinWithCodeClicked);
        if (refreshListaButton != null) refreshListaButton.onClick.AddListener(RefreshServerList);
        if (joinPanelBackButton != null) joinPanelBackButton.onClick.AddListener(MostrarMainMenu);
        
        // 1. Ocultar el botón al inicio, solo se muestra cuando eres Host confirmado
        if (startGameButton != null) 
        {
            startGameButton.gameObject.SetActive(false);
            startGameButton.onClick.AddListener(OnStartGameButtonClicked);
        }

        // Estado inicial de la UI
        MostrarMainMenu();

        // Suscripción de UI a eventos de datos puros (Regla 5: Desacoplamiento)
        if (gameManager != null)
        {
            gameManager.OnListaJugadoresModificada += ActualizarTextoJugadores;
            gameManager.OnCodigoSalaModificado += ActualizarTextoCodigo;
        }

        // Nos suscribimos de manera permanente al evento de desconexión
        if (networkManager != null)
        {
            networkManager.OnClientDisconnectCallback += OnClientDisconnect;

            // [FIX] Habilitar la validación de conexión para evitar que se autospawnee el PlayerArmature en el Lobby
            networkManager.NetworkConfig.ConnectionApproval = true;
            networkManager.ConnectionApprovalCallback = ApprovalCheck;
        }

        // --- CONEXIÓN A UNITY CLOUD (SOPORTE PARRELSYNC) ---
        try
        {
            InitializationOptions options = new InitializationOptions();
#if UNITY_EDITOR
            // Si usamos ParrelSync, creamos un perfil para cada clon o colapsará la red
            if (ParrelSync.ClonesManager.IsClone())
            {
                string customProfile = "Clone_" + ParrelSync.ClonesManager.GetArgument();
                options.SetProfile(customProfile);
            }
#endif
            await UnityServices.InitializeAsync(options);

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Conectado a la Nube Secreta de Unity con ID: {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ERROR NUBE] No hay internet o las credenciales fallan: {e.Message}");
        }
    }

    private async void OnHostButtonClicked()
    {
        // Evitar que el usuario pulse el botón varias veces seguidas muy rápido (Double Click spam)
        if (hostButton != null) hostButton.interactable = false;

        Debug.Log("<color=green>[LobbyUI] Pidiendo servidor gratuito a Unity Relay...</color>");
        
        try
        {
            // 1. Pedimos sala secreta para 10 cazadores máximo (+1 que es el anfitrión)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(10);
            
            // 2. Extraemos el código de 6 letras de Relay
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"<color=yellow>¡CÓDIGO DE SALA CREADO OBJETIVO: {joinCode}</color>");

            // 2.5 Crear el Lobby visible en Unity Services (Navegador)
            bool isPrivate = privateGameToggle != null ? privateGameToggle.isOn : false;
            string lobbyName = "Mundo de " + AuthenticationService.Instance.PlayerId.Substring(0, 5);
            
            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Data = new Dictionary<string, DataObject>
                {
                    { "JoinCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                }
            };

            _hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 10, lobbyOptions);
            Debug.Log($"<color=yellow>[Lobby] Sala Púbica/Privada registrada en la Nube: {_hostLobby.Name} (Privado: {isPrivate})</color>");

            // 3. Extraemos IPs nativas
            string hostIP = "";
            ushort hostPort = 0;
            bool isSecure = false;

            foreach (var endpoint in allocation.ServerEndpoints)
            {
                if (endpoint.ConnectionType == "dtls")
                {
                    hostIP = endpoint.Host;
                    hostPort = (ushort)endpoint.Port;
                    isSecure = endpoint.Secure;
                    break;
                }
                else if (endpoint.ConnectionType == "udp" && string.IsNullOrEmpty(hostIP)) 
                {
                    hostIP = endpoint.Host;
                    hostPort = (ushort)endpoint.Port;
                    isSecure = false;
                }
            }
            
            Debug.Log($"<color=blue>[Relay] Configurando IP del Host: {hostIP}:{hostPort} (Seguro: {isSecure})</color>");

            networkManager.GetComponent<UnityTransport>().SetHostRelayData(
                hostIP, hostPort, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, isSecure
            );

            // [NUEVO] Registrar payload de nickname para el Host
            string nick = GetSavedNickname();
            networkManager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(nick);

            // 4. Arrancamos Primero el Servidor real en el internet
            // Evitamos el error rojo de "Cannot start host while an instance is already running"
            if (!networkManager.IsListening)
            {
                bool started = networkManager.StartHost();
                if(!started) Debug.LogError("NetworkManager ignoró el START HOST. Puede que la IP o el puerto estén bloqueados.");
            }
            else
            {
                Debug.LogWarning("[LobbyUI] El NetworkManager ya estaba encendido. Reanudando host...");
            }

            // Compartimos el código a través del GameManager
            if (gameManager != null)
            {
                gameManager.EstablecerCodigoSalaSincronizado(joinCode);
            }
            
            // Pasamos a la pantalla de Sala
            MostrarRoomPanel(true);

            // Devolvemos la posibilidad de clickear el botón por si hay que salir y volver a crear sala luego
            if (hostButton != null) hostButton.interactable = true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay Error] El servidor falló al crearse: {e.Message}");
            if (hostButton != null) hostButton.interactable = true; // Liberarlo si hubo error
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby Error] Error creando la sala en la nube: {e.Message}");
            if (hostButton != null) hostButton.interactable = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Error Inesperado] Se interrumpió la creación del Host: {e.Message}");
            if (hostButton != null) hostButton.interactable = true;
        }
    }

    private void OnJoinWithCodeClicked()
    {
        if (joinCodeInput == null) return;
        string typedCode = System.Text.RegularExpressions.Regex.Replace(joinCodeInput.text, "[^a-zA-Z0-9]", "").ToUpper();
        if (string.IsNullOrEmpty(typedCode) || typedCode.Length < 6)
        {
            Debug.LogWarning("[LobbyUI] Código inválido. Escribe un código válido de 6 letras.");
            return;
        }
        ConectarClienteARelay(typedCode);
    }

    private async void JoinLobbyTarget(Lobby targetLobby)
    {
        try
        {
            Debug.Log($"[Lobby] Uniéndose a sala {targetLobby.Name}...");
            Lobby lobbyUnido = await LobbyService.Instance.JoinLobbyByIdAsync(targetLobby.Id);
            
            // Extraer el JoinCode secreto que metimos en CreateLobbyAsync
            string codeFromLobby = lobbyUnido.Data["JoinCode"].Value;
            Debug.Log($"[Lobby] ¡Sala conectada! Código extraído: {codeFromLobby}. Lanzando Relay...");

            ConectarClienteARelay(codeFromLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby Error] Error al unirse a la sala listada: {e}");
        }
    }

    private async void ConectarClienteARelay(string typedCode)
    {
        Debug.Log($"<color=blue>[LobbyUI] Intentando asaltar la sala con código Relay: {typedCode}...</color>");

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(typedCode);

            string clientIP = "";
            ushort clientPort = 0;
            bool isSecure = false;

            foreach (var endpoint in joinAllocation.ServerEndpoints)
            {
                if (endpoint.ConnectionType == "dtls")
                {
                    clientIP = endpoint.Host;
                    clientPort = (ushort)endpoint.Port;
                    isSecure = endpoint.Secure;
                    break;
                }
                else if (endpoint.ConnectionType == "udp" && string.IsNullOrEmpty(clientIP)) 
                {
                    clientIP = endpoint.Host;
                    clientPort = (ushort)endpoint.Port;
                    isSecure = false;
                }
            }
            
            networkManager.GetComponent<UnityTransport>().SetClientRelayData(
                clientIP, clientPort, joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData, isSecure
            );

            // [NUEVO] Registrar payload de nickname para el Cliente
            string nick = GetSavedNickname();
            networkManager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(nick);

            bool success = networkManager.StartClient();
            
            if (success)
            {
                Debug.Log($"<color=green>[LobbyUI] ¡Conexión aceptada por el transportador!</color>");
                MostrarRoomPanel(false); 
            }
            else
            {
                Debug.LogError("[LobbyUI] Relay aceptó el código pero la red del NetworkManager rechazó la entrada.");
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay Error] Código inventado o servidor cerrado: {e.Message}");
        }
    }

    private async void RefreshServerList()
    {
        if (serverListContent == null || serverEntryPrefab == null) return;

        // 1. Limpiar lista antigua
        foreach (Transform child in serverListContent)
        {
            Destroy(child.gameObject);
        }

        try
        {
            // 2. Opciones de búsqueda (No mostrar vacíos ni llenos ni privados)
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse lobbies = await LobbyService.Instance.QueryLobbiesAsync(options);
            Debug.Log($"[LobbyUI] Encontradas {lobbies.Results.Count} salas públicas.");

            // 3. Crear instancias de UI
            foreach (Lobby lobby in lobbies.Results)
            {
                GameObject entryObj = Instantiate(serverEntryPrefab, serverListContent);
                LobbyEntryUI entryScript = entryObj.GetComponent<LobbyEntryUI>();
                if (entryScript != null)
                {
                    entryScript.Inicializar(lobby, JoinLobbyTarget);
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby Error] Error al buscar partidas: {e}");
        }
    }

    private async void OnStartGameButtonClicked()
    {
        if (gameManager != null)
        {
            // Opcional: Cerrar la sala de Lobby Cloud para que ya nadie se pueda unir durante la partida
            if (_hostLobby != null)
            {
                try {
                    await LobbyService.Instance.DeleteLobbyAsync(_hostLobby.Id);
                    _hostLobby = null;
                } catch { }
            }

            gameManager.StartGame();
        }
    }

    public async void LeaveRoom()
    {
        // Destruimos el personaje de lobby local antes de desconectarnos
        if (lobbyPlayerSpawner != null) lobbyPlayerSpawner.DestruirPersonajeLocal();

        if (networkManager != null)
        {
            networkManager.Shutdown(); // Corta la conexión actual limpiamente
        }

        // Si éramos el Host, borramos el Lobby en la nube
        if (_hostLobby != null)
        {
            try {
                await LobbyService.Instance.DeleteLobbyAsync(_hostLobby.Id);
                _hostLobby = null;
            } catch { } // Ignoramos fallos al borrar (ej si se crasheó internet)
        }

        VolverAlLobby();
    }

    private async void QuitGame()
    {
        Debug.Log("Saliendo del juego...");

        // Desconectarse limpiamente si se cierra la app siendo Host.
        if (_hostLobby != null)
        {
            try {
                await LobbyService.Instance.DeleteLobbyAsync(_hostLobby.Id);
            } catch { }
        }

        Application.Quit();
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnDestroy()
    {
        // Limpiamos los eventos si este GameObject se destruye
        if (gameManager != null)
        {
            gameManager.OnListaJugadoresModificada -= ActualizarTextoJugadores;
            gameManager.OnCodigoSalaModificado -= ActualizarTextoCodigo;
        }

        if (networkManager != null)
        {
            networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        // Si la ID es 0, normalmente significa que el servidor cerró.
        // Si es nuestra propia ID, significa que nos caímos nosotros.
        if (clientId == 0 || clientId == networkManager.LocalClientId)
        {
            Debug.Log("<color=orange>[LobbyUI] Se perdió la conexión con el Host. Restaurando el lobby...</color>");
            VolverAlLobby();
        }
    }

    private void ActualizarTextoJugadores(string nuevaLista)
    {
        if (playerListText != null)
        {
            playerListText.text = nuevaLista;
        }
    }

    private void ActualizarTextoCodigo(string nuevoCodigo)
    {
        if (joinCodeText != null)
        {
            joinCodeText.text = string.IsNullOrEmpty(nuevoCodigo) ? "" : $"CÓDIGO SECRETO: {nuevoCodigo}";
        }
    }

    private void MostrarMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (joinPanel != null) joinPanel.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        
        if (joinCodeText != null) joinCodeText.text = "";
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);

        // [Lobby Pro] Volver a la vista cinematográfica del Aldeano
        if (cameraManager != null) cameraManager.ActivarVistaMenu();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void MostrarJoinPanel()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (joinPanel != null) joinPanel.SetActive(true);
        if (roomPanel != null) roomPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        RefreshServerList(); // Automáticamente refrescar al abrir el panel
    }

    private void MostrarRoomPanel(bool isHost)
    {
        // 1. Intercambio de paneles UI
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (joinPanel != null)     joinPanel.SetActive(false);
        if (roomPanel != null)     roomPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // 2. Solo el Host ve el botón de empezar partida
        if (startGameButton != null) startGameButton.gameObject.SetActive(isHost);

        // 3. Activar el HUD de interacción del lobby (barra de progreso circular, prompts)
        LobbyInteractionUI interactionHUD = Object.FindAnyObjectByType<LobbyInteractionUI>(FindObjectsInactive.Include);
        if (interactionHUD != null)
        {
            interactionHUD.gameObject.SetActive(true);
            interactionHUD.HidePrompt(); // Empezamos limpio, sin prompts activos
            Debug.Log("[LobbyUI] LobbyInteractionUI activada al entrar en sala.");
        }
        else
        {
            Debug.LogWarning("[LobbyUI] No se encontró LobbyInteractionUI en la escena. ¿Está en la jerarquía?");
        }

        // 4. Bloqueamos el cursor: en la sala el jugador mira con el ratón (modo inmersivo)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log($"[LobbyUI] Panel de Sala activado. isHost={isHost}");
    }


    private void MostrarSettingsPanel()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    private void VolverAlLobby()
    {
        gameObject.SetActive(true); // Nos aseguramos de revivir si la base estaba desactivada

        // 1. Destruimos el personaje de lobby por si acaso (doble seguridad)
        if (lobbyPlayerSpawner != null) lobbyPlayerSpawner.DestruirPersonajeLocal();

        // 2. Restauramos la UI del menú
        MostrarMainMenu();

        // 3. Ocultamos el HUD de interacción si existe en la escena
        LobbyInteractionUI interactionHUD = Object.FindAnyObjectByType<LobbyInteractionUI>(FindObjectsInactive.Include);
        if (interactionHUD != null) interactionHUD.HidePrompt();

        // 4. Asegurarnos de tener el ratón de vuelta para poder dar click a "Start Client" o "Start Host"
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = false; // Desactivar la creación automática del Player Object de Netcode en el Lobby
        response.Pending = false;

        string nickname = "Jugador";
        if (request.Payload != null && request.Payload.Length > 0)
        {
            try
            {
                nickname = System.Text.Encoding.UTF8.GetString(request.Payload);
                nickname = nickname.Trim();
                if (nickname.Length > 25) nickname = nickname.Substring(0, 25);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[LobbyUI] Error al decodificar nickname del payload: {ex.Message}");
            }
        }
        else
        {
            if (request.ClientNetworkId == NetworkManager.ServerClientId)
            {
                nickname = GetSavedNickname();
            }
            else
            {
                nickname = $"Jugador_{request.ClientNetworkId}";
            }
        }

        if (gameManager != null)
        {
            gameManager.RegistrarNicknameCliente(request.ClientNetworkId, nickname);
        }
    }

    private string GetSavedNickname()
    {
        if (nicknameInputField != null && !string.IsNullOrEmpty(nicknameInputField.text))
        {
            string cleanName = nicknameInputField.text.Trim();
            if (cleanName.Length > 25) cleanName = cleanName.Substring(0, 25);
            PlayerPrefs.SetString("PlayerNickname", cleanName);
            PlayerPrefs.Save();
            return cleanName;
        }

        string savedName = PlayerPrefs.GetString("PlayerNickname", "");
        if (!string.IsNullOrEmpty(savedName))
        {
            return savedName;
        }

        // Si no hay nada, generamos uno por defecto aleatorio
        int rand = Random.Range(100, 999);
        string defaultName = $"Jugador_{rand}";
        PlayerPrefs.SetString("PlayerNickname", defaultName);
        PlayerPrefs.Save();
        return defaultName;
    }
}

