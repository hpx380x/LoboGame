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

        // OnNetworkSpawn ensures we initialize once the object is ready on the network
        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                // Enlaza la UI local del cliente con este jugador específico
                _localGameplayUI = Object.FindFirstObjectByType<GameplayUI>();
                
                // Conseguimos el estado del jugador para saber si está vivo o muerto
                _playerState = GetComponent<PlayerState>();
                if (_playerState != null)
                {
                    _playerState.isDead.OnValueChanged += OnDeathStateChanged;
                }

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

        // OnScroll is automatically called by PlayerInput if set to "Send Messages"
        public void OnScroll(InputValue value)
        {
            Debug.Log($"[PergaminoLog] Input Recibido ('Q'). IsOwner: {IsOwner} | isPressed: {value.isPressed}");

            // [Robustez] Solo bloqueamos si el NetworkManagerestá activo y REALMENTE no somos el dueño.
            // Si estamos en modo de prueba local, dejamos que pase.
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsOwner) return;

            // ¡BLOQUEO DE MUERTE! Si el jugador está muerto, se le prohíbe abrir el pergamino.
            if (_playerState != null && _playerState.isDead.Value)
            {
                Debug.Log("[PergaminoLog] Jugador muerto, no se le permite leer.");
                return;
            }

            // Only trigger on PRESS (not release). This is the "One-Shot" fix.
            if (value.isPressed)
            {
                ExecuteToggle();
            }
        }

        private void ExecuteToggle()
        {
            _isReading = !_isReading;
            Debug.Log($"[PergaminoLog] Ejecutando Toggle! Estado local _isReading pasa a: {_isReading}");

            // --- INTERACCIÓN CON EL HUD LOCAL ---
            if (_localGameplayUI == null) 
            {
                _localGameplayUI = Object.FindFirstObjectByType<GameplayUI>();
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

            Debug.Log($"[ScrollController] Toggle UI Scroll HUD! State: {_isReading}");
        }
    }
}
