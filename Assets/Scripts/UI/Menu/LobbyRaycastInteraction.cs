using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

/// <summary>
/// Procesa el Raycast desde la mirada para detectar jugadores (X para expulsar)
/// y la hoguera (E para empezar) con un HUD inmersivo.
/// </summary>
public class LobbyRaycastInteraction : NetworkBehaviour
{
    [Header("Ajustes de Interacción")]
    [SerializeField] private float reachDistance = 10f;
    [SerializeField] private LayerMask interactionLayer;
    [SerializeField] private float holdTimeRequired = 1.5f;

    [Header("Referencias UI")]
    [SerializeField] private LobbyInteractionUI interactionHUD;

    private Camera _cam;
    private float _currentHoldTime = 0f;
    private ulong? _targetClientId = null;
    private bool _isLookingAtStartPoint = false;

    private GameManager _gameManager;

    private void Start()
    {
        // [FIX] Búsqueda en cascada: fpCamera hija → Camera.main → tag MainCamera
        // Gemini Flash rompía esto al desactivar el GameObject de la Main Camera.
        _cam = GetComponentInChildren<Camera>();
        if (_cam == null) _cam = Camera.main;
        if (_cam == null)
        {
            Camera[] allCams = Resources.FindObjectsOfTypeAll<Camera>();
            foreach (Camera c in allCams)
            {
                if (c.CompareTag("MainCamera") && c.enabled)
                {
                    _cam = c;
                    break;
                }
            }
        }

        _gameManager = FindAnyObjectByType<GameManager>();

        if (interactionHUD == null)
            interactionHUD = FindAnyObjectByType<LobbyInteractionUI>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // [FIX] Re-búsqueda dinámica por si el jugador spawneó después del Start
        if (_cam == null)
        {
            _cam = GetComponentInChildren<Camera>();
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return; // Sin cámara, esperamos al próximo frame
        }

        // [MEJORA] Búsqueda robusta del HUD si es null (incluyendo inactivos)
        if (interactionHUD == null)
        {
            interactionHUD = Object.FindAnyObjectByType<LobbyInteractionUI>(FindObjectsInactive.Include);
            if (interactionHUD != null)
            {
                interactionHUD.gameObject.SetActive(true);
            }
        }

        if (interactionHUD == null) return;

        // [NUEVO] Manejar Salida (Disponible para TODOS)
        if (Keyboard.current.escapeKey.isPressed)
        {
            ManejarHoldSalida();
            return; // Bloquea otras interacciones mientras intentas salir
        }
        else if (_currentHoldTime > 0 && !Keyboard.current.xKey.isPressed && !Keyboard.current.eKey.isPressed)
        {
            // Reset si soltamos ESC y no hay otras entradas
            _currentHoldTime = 0;
            interactionHUD.SetProgress(0);
            interactionHUD.HidePrompt();
        }

        // [MEJORA] Si la capa es 0 (nada), ponemos una por defecto
        if (interactionLayer.value == 0)
        {
            interactionLayer = LayerMask.GetMask("Default", "Player", "Interactable");
        }

        // El Raycast de nombres y detección lo ejecutan TODOS para ver el HUD inmersivo
        ManejarRaycast();

        // Solo el Host puede procesar las acciones de gestión (Expulsar/Empezar)
        if (IsServer)
        {
            ManejarEntradaHost();
        }
    }

    private void ManejarHoldSalida()
    {
        _currentHoldTime += Time.deltaTime;
        interactionHUD.ShowPrompt("MANTÉN [ESC] PARA ABANDONAR", "SALIENDO AL MENÚ...");
        interactionHUD.SetProgress(_currentHoldTime / holdTimeRequired);

        if (_currentHoldTime >= holdTimeRequired)
        {
            EjecutarSalida();
            _currentHoldTime = 0f;
        }
    }

    private void EjecutarSalida()
    {
        // 1. Ocultar HUD inmediatamente para que no se quede pegado en pantalla
        if (interactionHUD != null)
        {
            interactionHUD.SetProgress(0);
            interactionHUD.HidePrompt();
        }

        LobbyUI lobbyUI = Object.FindAnyObjectByType<LobbyUI>();
        if (lobbyUI != null)
        {
            Debug.Log("[LobbyInteraction] Saliendo de la sala vía ESC...");
            lobbyUI.LeaveRoom();
        }
        else
        {
            Debug.LogWarning("[LobbyInteraction] No se encontró el componente LobbyUI para salir.");
        }
    }

    private void ManejarRaycast()
    {
        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        bool foundTarget = false;
        
        if (Physics.Raycast(ray, out hit, reachDistance, interactionLayer))
        {
            // 1. ¿Es un jugador?
            NetworkObject netObj = hit.collider.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId)
            {
                if (_targetClientId != netObj.OwnerClientId)
                {
                    _targetClientId = netObj.OwnerClientId;
                    _currentHoldTime = 0f;
                }
                
                // [LÓGICA DIFERENCIADA]
                string prompt = IsServer ? "MANTÉN [X] PARA EXPULSAR" : "MIRANDO A";
                string info = $"JUGADOR {netObj.OwnerClientId}";
                
                interactionHUD.ShowPrompt(prompt, info);
                _isLookingAtStartPoint = false;
                foundTarget = true;
            }
            // 2. ¿Es la hoguera?
            bool isCampfire = hit.collider.name.Contains("Fire") || 
                             hit.collider.name.Contains("hoguera") ||
                             hit.collider.name.Contains("Campfire") ||
                             (hit.collider.transform.parent != null && hit.collider.transform.parent.name.ToLower().Contains("hoguera"));

            if (isCampfire)
            {
                // [LÓGICA DIFERENCIADA]
                string prompt = IsServer ? "MANTÉN [E] PARA EMPEZAR" : "ESPERANDO AL LÍDER...";
                
                interactionHUD.ShowPrompt(prompt, "HOGUERA");
                _isLookingAtStartPoint = true;
                _targetClientId = null;
                foundTarget = true;
            }
        }

        if (!foundTarget)
        {
            if (_targetClientId != null || _isLookingAtStartPoint)
            {
                _targetClientId = null;
                _isLookingAtStartPoint = false;
                _currentHoldTime = 0f;
                interactionHUD.HidePrompt();
            }
        }
    }

    private void ManejarEntradaHost()
    {
        bool isHoldingKick = _targetClientId.HasValue && Keyboard.current.xKey.isPressed;
        bool isHoldingStart = _isLookingAtStartPoint && Keyboard.current.eKey.isPressed;

        if (isHoldingKick || isHoldingStart)
        {
            _currentHoldTime += Time.deltaTime;
            interactionHUD.SetProgress(_currentHoldTime / holdTimeRequired);

            if (_currentHoldTime >= holdTimeRequired)
            {
                if (isHoldingKick) EjecutarKick();
                else if (isHoldingStart) EjecutarStart();
                
                _currentHoldTime = 0f;
            }
        }
        else if (!Keyboard.current.escapeKey.isPressed)
        {
            if (_currentHoldTime > 0)
            {
                _currentHoldTime = 0f;
                interactionHUD.SetProgress(0);
            }
        }
    }

    private void EjecutarKick()
    {
        if (_targetClientId.HasValue && _gameManager != null)
        {
            _gameManager.KickPlayerServerRpc(_targetClientId.Value);
            interactionHUD.HidePrompt();
            _targetClientId = null;
        }
    }

    private void EjecutarStart()
    {
        if (_gameManager != null)
        {
            _gameManager.StartGame();
            interactionHUD.HidePrompt();
        }
    }
}

