using UnityEngine;
using StarterAssets;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections;

namespace StarterAssets
{
    // Inherit from NetworkBehaviour for NGO compatibility
    public class ScrollController : NetworkBehaviour
    {
        private bool _isReading;
        private GameplayUI _localGameplayUI;
        private PlayerState _playerState;
        private float _spawnTime;

        private void Awake()
        {
            _spawnTime = Time.time;
        }

        private void OnEnable()
        {
            _spawnTime = Time.time;
        }

        // OnNetworkSpawn ensures we initialize once the object is ready on the network
        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                // Enlaza la UI local del cliente con este jugador específico
                _localGameplayUI = Object.FindAnyObjectByType<GameplayUI>();
                
                // Conseguimos el estado del jugador para saber si está vivo o muerto
                _playerState = GetComponent<PlayerState>();
                if (_playerState != null)
                {
                    _playerState.isDead.OnValueChanged += OnDeathStateChanged;
                }

                // [GARANTÍA DE SPAWN] Asegurar que el movimiento nazca desbloqueado y el pergamino cerrado
                _isReading = false;
                if (TryGetComponent(out ThirdPersonController tpc)) tpc.CanMove = true;
                if (_localGameplayUI != null) _localGameplayUI.ToggleScroll(false);

                Debug.Log($"[PergaminoLog] Jugador Local {_localGameplayUI != null} | Enlazado al GameplayUI.");
            }

            Debug.Log($"[ScrollController] Local de {gameObject.name} inicializado. IsOwner: {IsOwner}");
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner && _playerState != null)
            {
                _playerState.isDead.OnValueChanged -= OnDeathStateChanged;
            }
        }

        private void OnDeathStateChanged(bool estadoAnterior, bool estadoNuevo)
        {
            // Si nos acaban de matar, forzamos el cierre del pergamino y reiniciamos el estado local.
            if (estadoNuevo == true)
            {
                Debug.Log("[PergaminoLog] Muerte detectada. Forzando cierre de HUD y estado _isReading = false.");
                _isReading = false;
                if (_localGameplayUI != null) _localGameplayUI.ToggleScroll(false);
            }
        }

        public bool IsReading => _isReading;

        private void Update()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsOwner) return;

            // Detección limpia a través del nuevo Input System
            if (TryGetComponent(out StarterAssetsInputs inputs) && inputs.scroll)
            {
                inputs.scroll = false; // Consumimos el input para que no se dispare múltiples veces
                ToggleReading();
            }
        }

        private void ToggleReading()
        {
            // Si el pergamino ya está abierto, siempre permitimos cerrarlo
            if (_isReading)
            {
                ExecuteToggle();
                return;
            }

            // Evitamos abrir el pergamino si los controles del jugador están bloqueados (minijuegos, aturdimiento, votación, etc.)
            if (TryGetComponent(out StarterAssetsInputs inputs) && inputs.isInputLocked)
            {
                Debug.Log("[PergaminoLog] Controles bloqueados, ignorando apertura de pergamino.");
                return;
            }

            // ¡BLOQUEO DE MUERTE! Si el jugador está muerto, se le prohíbe abrir el pergamino.
            if (_playerState != null && _playerState.isDead.Value)
            {
                Debug.Log("[PergaminoLog] Jugador muerto, no se le permite leer.");
                return;
            }

            ExecuteToggle();
        }

        private void ExecuteToggle()
        {
            _isReading = !_isReading;
            Debug.Log($"[PergaminoLog] Ejecutando Toggle! Estado local _isReading pasa a: {_isReading}");

            // --- INTERACCIÓN CON EL HUD LOCAL ---
            if (_localGameplayUI == null) 
            {
                _localGameplayUI = Object.FindAnyObjectByType<GameplayUI>();
                Debug.Log("[PergaminoLog] Buscando GameplayUI de emergencia...");
            }

            if (_localGameplayUI != null)
            {
                Debug.Log($"[PergaminoLog] Enviando la orden al Canvas GameplayUI...");
                _localGameplayUI.ToggleScroll(_isReading); 
            }
            else if (_localGameplayUI == null)
            {
                Debug.LogError($"[PergaminoLog] ¡ERROR! No se encontró GameplayUI para abrir el Canvas.");
            }

            // --- MANEJO DE CURSOR E INPUT DE MOUSE/TECLADO ---
            if (TryGetComponent(out StarterAssetsInputs inputs))
            {
                if (!_isReading)
                {
                    // Aseguramos que siga bloqueado por si acaso
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    inputs.cursorInputForLook = true;
                }
            }

            Debug.Log($"[ScrollController] Toggle UI Scroll HUD! State: {_isReading}");
        }
    }
}

