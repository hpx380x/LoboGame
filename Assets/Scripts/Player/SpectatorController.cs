using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

/// <summary>
/// CONTROLADOR DE ESPECTADOR (LOCAL-ONLY)
/// Este script se activa automáticamente en el jugador local cuando muere.
/// Permite volar libremente o seguir a otros jugadores vivos.
/// </summary>
public class SpectatorController : MonoBehaviour
{
    [Header("Configuración de Vuelo")]
    public float flySpeed = 15f;
    public float turboMultiplier = 2.5f;
    public float lookSensitivity = 1.0f;
    public float smoothTime = 0.12f;

    [Header("Estado")]
    [SerializeField] private bool isFreeCam = true;
    [SerializeField] private PlayerState currentTarget;

    private CinemachineVirtualCamera _vcam;
    private List<PlayerState> _livingPlayers = new List<PlayerState>();
    private PlayerState[] _allPlayersCache;
    private int _playerIndex = -1;
    private Vector3 _currentVelocity;
    private Vector2 _lookRotation;
    private float _currentSpeedScale = 1.0f;

    private GameplayUI _ui;

    private void Awake()
    {
        // El componente empieza desactivado, PlayerState lo habilitará al morir
        enabled = false;
    }

    private void OnEnable()
    {
        Debug.Log("[Espectador] Modo Espectador ACTIVO.");
        
        _vcam = Object.FindAnyObjectByType<CinemachineVirtualCamera>();
        _ui = Object.FindAnyObjectByType<GameplayUI>();

        if (_vcam == null)
        {
            Debug.LogWarning("[Espectador] No se encontró CinemachineVirtualCamera.");
            return;
        }

        // Sincronizar rotación inicial con la cámara actual
        _lookRotation = new Vector2(_vcam.transform.eulerAngles.y, _vcam.transform.eulerAngles.x);
        
        // Cachear jugadores y suscribirse a cambios de estado de muerte
        _allPlayersCache = Object.FindObjectsByType<PlayerState>(FindObjectsInactive.Exclude);
        if (_allPlayersCache != null)
        {
            foreach (var ps in _allPlayersCache)
            {
                if (ps != null && ps.isDead != null)
                {
                    ps.isDead.OnValueChanged += OnPlayerDeadChanged;
                }
            }
        }
        UpdateLivingPlayersList();

        // Empezar en modo libre por defecto
        SetFreeCam(true);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        if (_allPlayersCache != null)
        {
            foreach (var ps in _allPlayersCache)
            {
                if (ps != null && ps.isDead != null)
                {
                    ps.isDead.OnValueChanged -= OnPlayerDeadChanged;
                }
            }
        }

        if (_ui != null) _ui.ActualizarEspectador(null, false);
        Debug.Log("[Espectador] Modo Espectador DESACTIVADO.");
    }

    private void OnPlayerDeadChanged(bool oldValue, bool newValue)
    {
        UpdateLivingPlayersList();
        if (!isFreeCam && currentTarget != null && currentTarget.isDead.Value)
        {
            CyclePlayers(1);
        }
    }

    private void Update()
    {
        // Alternar a Vuelo Libre con ESPACIO
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SetFreeCam(true);
        }

        // Ciclar con CLICKS
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) CyclePlayers(1);
            else if (Mouse.current.rightButton.wasPressedThisFrame) CyclePlayers(-1);
        }

        if (isFreeCam) HandleFreeFlight();
        else HandleFollowing();
    }

    private void SetFreeCam(bool free)
    {
        isFreeCam = free;
        if (free)
        {
            currentTarget = null;
            if (_vcam != null)
            {
                _vcam.Follow = null;
                _vcam.LookAt = null;
            }
            if (_ui != null) _ui.ActualizarEspectador(null, true);
        }
    }

    private void CyclePlayers(int direction)
    {
        UpdateLivingPlayersList();

        if (_livingPlayers.Count == 0)
        {
            SetFreeCam(true);
            return;
        }

        isFreeCam = false;
        _playerIndex = (_playerIndex + direction + _livingPlayers.Count) % _livingPlayers.Count;
        currentTarget = _livingPlayers[_playerIndex];

        if (_vcam != null && currentTarget != null)
        {
            Transform targetRoot = currentTarget.transform.Find("PlayerCameraRoot");
            if (targetRoot == null) targetRoot = currentTarget.transform;

            _vcam.Follow = targetRoot;
            _vcam.LookAt = targetRoot;
            
            if (_ui != null) _ui.ActualizarEspectador(currentTarget.gameObject.name, true);
        }
    }

    private void UpdateLivingPlayersList()
    {
        _livingPlayers.Clear();
        if (_allPlayersCache == null) return;
        
        foreach (var ps in _allPlayersCache)
        {
            // No nos seguimos a nosotros mismos (estamos muertos) ni a otros muertos
            if (ps != null && ps.isDead != null && !ps.isDead.Value)
            {
                _livingPlayers.Add(ps);
            }
        }
    }

    private void HandleFreeFlight()
    {
        if (_vcam == null) return;

        // Rotación
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * 0.1f * lookSensitivity;
        _lookRotation.x += mouseDelta.x;
        _lookRotation.y -= mouseDelta.y;
        _lookRotation.y = Mathf.Clamp(_lookRotation.y, -89f, 89f);
        _vcam.transform.rotation = Quaternion.Euler(_lookRotation.y, _lookRotation.x, 0);

        // Movimiento WASD + Q/E
        Vector3 inputDir = Vector3.zero;
        if (Keyboard.current.wKey.isPressed) inputDir += _vcam.transform.forward;
        if (Keyboard.current.sKey.isPressed) inputDir -= _vcam.transform.forward;
        if (Keyboard.current.aKey.isPressed) inputDir -= _vcam.transform.right;
        if (Keyboard.current.dKey.isPressed) inputDir += _vcam.transform.right;
        if (Keyboard.current.eKey.isPressed) inputDir += Vector3.up;
        if (Keyboard.current.qKey.isPressed) inputDir -= Vector3.up;

        float currentSpeed = flySpeed;
        if (Keyboard.current.leftShiftKey.isPressed) currentSpeed *= turboMultiplier;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll != 0) _currentSpeedScale = Mathf.Clamp(_currentSpeedScale + (scroll * 0.001f), 0.1f, 5f);
        currentSpeed *= _currentSpeedScale;

        Vector3 targetPos = _vcam.transform.position + inputDir * currentSpeed * Time.deltaTime;
        _vcam.transform.position = Vector3.SmoothDamp(_vcam.transform.position, targetPos, ref _currentVelocity, smoothTime);
    }

    private void HandleFollowing()
    {
        if (currentTarget == null || currentTarget.isDead.Value)
        {
            CyclePlayers(1);
        }
    }
}

