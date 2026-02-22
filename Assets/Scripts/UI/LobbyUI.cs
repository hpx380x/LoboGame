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
    [Header("UI Elements")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    [Header("Relay System (Nube)")]
    [Tooltip("El recuadro blanco donde los amigos escriben el código para unirse")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [Tooltip("El texto donde aparecerá el código en mayúsculas para que el Host se lo dicte a sus amigos")]
    [SerializeField] private TextMeshProUGUI joinCodeText;

    [Header("Network Management")]
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

            // 3. Pintamos el texto para que la gente en la sala lo vea
            if (joinCodeText != null) joinCodeText.text = $"CÓDIGO SECRETO: {joinCode}";

            // 4. Inyectamos los datos de Relay en el motor de Netcode (Con el formato de 1 solo parámetro nuevo)
            // 4. Inyectamos los datos sin usar RelayServerData para evadir el bug de constructores
            // 4. Inyectamos los datos asegurando endpoints
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

            // 5. Arrancamos el Host real en el internet e inyectamos el botón verde
            bool started = networkManager.StartHost();
            if(!started) Debug.LogError("NetworkManager ignoró el START HOST");
            
            // Ocultamos botones de Host/Client para no recargar (Pero no el GameObject para conservar el ID a la vista)
            hostButton.gameObject.SetActive(false);
            if (clientButton != null) clientButton.gameObject.SetActive(false);
            if (joinCodeInput != null) joinCodeInput.gameObject.SetActive(false);

            GameUI gameUI = FindFirstObjectByType<GameUI>(FindObjectsInactive.Include);
            if (gameUI != null) gameUI.gameObject.SetActive(true);
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
                gameObject.SetActive(false);
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

    private void OnDestroy()
    {
        // Limpiamos el evento si este GameObject se destruye
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

    private void VolverAlLobby()
    {
        // 1. Mostrar de nuevo TODO el Canvas del Lobby
        gameObject.SetActive(true);
        
        // 2. Reactivar los botones y el campo de texto interno (que ocultamos al entrar)
        if (hostButton != null) hostButton.gameObject.SetActive(true);
        if (clientButton != null) clientButton.gameObject.SetActive(true);
        if (joinCodeInput != null) joinCodeInput.gameObject.SetActive(true);

        // Limpiamos el texto que decía el código antiguo
        if (joinCodeText != null) joinCodeText.text = "";

        // 3. Ocultar la Interfaz del Juego si estaba abierta
        GameUI gameUI = FindFirstObjectByType<GameUI>(FindObjectsInactive.Include);
        if (gameUI != null) gameUI.gameObject.SetActive(false);

        // 4. Asegurarnos de tener el ratón de vuelta para poder dar click a "Start Client" o "Start Host"
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
