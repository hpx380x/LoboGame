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
    [Tooltip("El recuadro blanco donde los amigos escriben el código para unirse")]
    [SerializeField] private TMP_InputField joinCodeInput;

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
    // [Regla 4] Evitamos NetworkManager.Singleton
    [SerializeField] private NetworkManager networkManager;

    private async void Start()
    {
        Debug.Log("[LobbyUI] Iniciando Sistema de Nube Mundial...");
        
        // Desboqueamos forzosamente el ratón
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (hostButton != null) hostButton.onClick.AddListener(OnHostButtonClicked);
        if (clientButton != null) clientButton.onClick.AddListener(OnClientButtonClicked);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (leaveButton != null) leaveButton.onClick.AddListener(LeaveRoom);
        
        if (settingsButton != null) settingsButton.onClick.AddListener(MostrarSettingsPanel);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(MostrarMainMenu);
        
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
        }

        // --- CONEXIÓN A UNITY CLOUD ---
        try
        {
            await UnityServices.InitializeAsync();
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
        Debug.Log("<color=green>[LobbyUI] Pidiendo servidor gratuito a Unity Relay...</color>");
        
        try
        {
            // 1. Pedimos sala secreta para 10 cazadores máximo (+1 que es el anfitrión)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(10);
            
            // 2. Extraemos el código de 6 letras como el "Among Us"
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"<color=yellow>¡CÓDIGO DE SALA CREADO OBJETIVO: {joinCode}</color>");

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
                else if (endpoint.ConnectionType == "udp" && string.IsNullOrEmpty(hostIP)) // Fallback preventivo
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

            // 4. Arrancamos Primero el Servidor real en el internet
            bool started = networkManager.StartHost();
            if(!started) Debug.LogError("NetworkManager ignoró el START HOST");

            // 4. ¡AHORA SÍ! Compartimos el código a través del GameManager (Porque el servidor ya nació oficialmente)
            if (gameManager != null)
            {
                gameManager.EstablecerCodigoSalaSincronizado(joinCode);
            }
            
            // Pasamos a la pantalla de Sala
            MostrarRoomPanel(true);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay Error] El servidor falló al crearse: {e.Message}");
        }
    }

    private async void OnClientButtonClicked()
    {
        if (joinCodeInput == null)
        {
            Debug.LogError("[LobbyUI] Error: Falta asignar el InputField en el Inspector.");
            return;
        }

        // Filtramos TODO lo que no sean letras y números (elimina saltos de línea invisibles, espacios, etc)
        string typedCode = System.Text.RegularExpressions.Regex.Replace(joinCodeInput.text, "[^a-zA-Z0-9]", "").ToUpper();

        if (string.IsNullOrEmpty(typedCode) || typedCode.Length < 6)
        {
            Debug.LogWarning($"[LobbyUI] Tienes que escribir un código de 6 letras completo para unirte. Has escrito: '{typedCode}'");
            return;
        }


        Debug.Log($"<color=blue>[LobbyUI] Intentando asaltar la sala de código: {typedCode}...</color>");

        try
        {
            // 1. Validamos código en Unity Server
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(typedCode);


            // 2. Extraemos IPs nativas para el Cliente sin constructores conflictivos
            // 2. Extraemos IPs nativas para el Cliente asegurando endpoints
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
                else if (endpoint.ConnectionType == "udp" && string.IsNullOrEmpty(clientIP)) // Fallback preventivo
                {
                    clientIP = endpoint.Host;
                    clientPort = (ushort)endpoint.Port;
                    isSecure = false;
                }
            }
            
            Debug.Log($"<color=blue>[Relay] Configurando IP del Cliente: {clientIP}:{clientPort} (Seguro: {isSecure})</color>");

            networkManager.GetComponent<UnityTransport>().SetClientRelayData(
                clientIP, clientPort, joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData, isSecure
            );

            // 3. Entramos como Cliente pacífico
            bool success = networkManager.StartClient();
            
            if (success)
            {
                Debug.Log($"<color=green>[LobbyUI] ¡Conexión aceptada por el transportador!</color>");
                MostrarRoomPanel(false); // Falso porque somos Clientes
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

    private void OnStartGameButtonClicked()
    {
        if (gameManager != null)
        {
            gameManager.StartGame();
        }
    }

    private void LeaveRoom()
    {
        if (networkManager != null)
        {
            networkManager.Shutdown(); // Corta la conexión actual limpiamente
        }
        VolverAlLobby();
    }

    private void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
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
        if (roomPanel != null) roomPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        
        if (joinCodeText != null) joinCodeText.text = "";
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void MostrarRoomPanel(bool isHost)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        
        if (startGameButton != null) startGameButton.gameObject.SetActive(isHost);
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
        MostrarMainMenu();

        // Ocultar la Interfaz del Juego si estaba abierta erróneamente
        GameUI gameUI = FindFirstObjectByType<GameUI>(FindObjectsInactive.Include);
        if (gameUI != null) gameUI.gameObject.SetActive(false);

        // 4. Asegurarnos de tener el ratón de vuelta para poder dar click a "Start Client" o "Start Host"
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
