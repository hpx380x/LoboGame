using UnityEngine;
using StarterAssets;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Automatiza la configuración del personaje para que aparezca sentado en el Lobby.
/// Bloquea input y físicas para que no se mueva lo más mínimo.
///
/// CORRECCIONES APLICADAS:
/// [FIX #1] Eliminado Update() — los parámetros del Animator se setean UNA SOLA VEZ en Start().
/// [FIX #4] Nombre de escena cacheado en bool _estaEnLobby para evitar string comparison por frame.
/// [FIX #6] CharacterController y PlayerInput bloqueados para impedir movimiento accidental.
/// [FIX #8] Nombres de parámetros del Animator como constantes para evitar typos silenciosos.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerLobbyPose : MonoBehaviour
{
    // ─── Constantes de parámetros del Animator ────────────────────────────────
    // [FIX #8] Centralizar los nombres evita typos silenciosos en el Inspector y en el código.

    private const string PARAM_IS_SITTING = "isSitting";
    private const string PARAM_GROUNDED   = "Grounded";
    private const string PARAM_FREE_FALL  = "FreeFall";
    private const string PARAM_SPEED      = "Speed";

    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Configuración Animator")]
    [Tooltip("El nombre exacto de la capa en el Animator que controla la animación de sentado.")]
    [SerializeField] private string sitLayerName = "Lobby Layer";
    [SerializeField] [Range(0f, 1f)] private float sitWeight = 1.0f;

    // ─── Privadas ─────────────────────────────────────────────────────────────

    private Animator    _animator;
    private int         _sitLayerIndex = -1;

    // [FIX #4] Comprobado en Start ya que solo se ejecuta una vez al inicializar el objeto.
    private bool _estaEnLobby = false;

    // Flags de parámetros disponibles (detectados en Awake para no crashear si faltan).
    private bool _hasIsSittingParam;
    private bool _hasGroundedParam;
    private bool _hasFreeFallParam;
    private bool _hasSpeedParam;

    // ─── Ciclo de vida ────────────────────────────────────────────────────────

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        if (_animator == null) return;

        _sitLayerIndex = _animator.GetLayerIndex(sitLayerName);

        // [FIX #8] Detección de parámetros usando las constantes.
        foreach (AnimatorControllerParameter param in _animator.parameters)
        {
            if (param.name == PARAM_IS_SITTING) _hasIsSittingParam = true;
            if (param.name == PARAM_GROUNDED)   _hasGroundedParam  = true;
            if (param.name == PARAM_FREE_FALL)  _hasFreeFallParam  = true;
            if (param.name == PARAM_SPEED)      _hasSpeedParam     = true;
        }
    }

    private void Start()
    {
        // Comprobar la escena activa real en el momento del Start
        _estaEnLobby = SceneManager.GetActiveScene().name == "Scene_Menu";
        if (!_estaEnLobby)
        {
            // [FIX] Si no estamos en el lobby, forzamos que el peso de la capa de sentado sea 0.
            // Esto asegura que en Scene_Gameplay el personaje esté parado y use sus animaciones de movimiento.
            if (_animator != null && _sitLayerIndex != -1)
            {
                _animator.SetLayerWeight(_sitLayerIndex, 0f);
            }
            return;
        }

        // ── Validaciones ──────────────────────────────────────────────────────

        if (_animator == null)
        {
            Debug.LogWarning($"[PlayerLobbyPose] {gameObject.name}: No se encontró Animator.");
            return;
        }

        if (_sitLayerIndex == -1)
        {
            Debug.LogWarning($"[PlayerLobbyPose] {gameObject.name}: La capa '{sitLayerName}' " +
                             $"no existe en el Animator. El personaje no se sentará correctamente.");
        }

        // ── [FIX #1] Setear el Animator UNA SOLA VEZ en Start (no en Update) ──

        if (_sitLayerIndex != -1)
            _animator.SetLayerWeight(_sitLayerIndex, sitWeight);

        if (_hasGroundedParam)   _animator.SetBool (PARAM_GROUNDED,   true);
        if (_hasFreeFallParam)   _animator.SetBool (PARAM_FREE_FALL,  false);
        if (_hasSpeedParam)      _animator.SetFloat(PARAM_SPEED,      0f);
        if (_hasIsSittingParam)  _animator.SetBool (PARAM_IS_SITTING, true);

        // ── [FIX #6] Bloquear CharacterController para impedir movimiento físico ──
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            Debug.Log($"[PlayerLobbyPose] {gameObject.name}: CharacterController desactivado para el Lobby.");
        }

        // ── [FIX #6] Bloquear PlayerInput para que no procese ninguna acción ──
        // Usamos currentActionMap.Disable() siguiendo la regla #3 de arquitectura:
        // NO desactivar el componente PlayerInput, solo el mapa de acciones activo.
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null && playerInput.currentActionMap != null)
        {
            playerInput.currentActionMap.Disable();
            Debug.Log($"[PlayerLobbyPose] {gameObject.name}: ActionMap '{playerInput.currentActionMap.name}' desactivado para el Lobby.");
        }

        // ── Bloquear inputs de StarterAssets (cursor y movimiento) ────────────
        StarterAssetsInputs sai = GetComponent<StarterAssetsInputs>();
        if (sai != null)
        {
            sai.move              = Vector2.zero;
            sai.look              = Vector2.zero;
            sai.jump              = false;
            sai.sprint            = false;
            sai.cursorLocked      = false;
            sai.cursorInputForLook = false;
        }
    }

    // [FIX #1] Update() ELIMINADO.
    // Los valores del Animator son estáticos durante el Lobby — no hay razón para seterarlos cada frame.
    // Si otro sistema los modifica inesperadamente, eso es un bug en ese sistema, no aquí.
}
