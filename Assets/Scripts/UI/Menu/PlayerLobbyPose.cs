using UnityEngine;
using StarterAssets;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Automatiza la configuración del personaje para que aparezca sentado en el Lobby.
/// Bloqueando el input y las físicas para que no se mueva lo más mínimo.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerLobbyPose : MonoBehaviour
{
    [Header("Configuración Animator")]
    [Tooltip("El nombre exacto de la capa en el Animator que controla la animación de sentado")]
    [SerializeField] private string sitLayerName = "Lobby Layer";
    [SerializeField] private float sitWeight = 1.0f;

    private Animator _animator;
    private int _sitLayerIndex;
    private bool _hasIsSittingParam;
    private bool _hasGroundedParam;
    private bool _hasFreeFallParam;
    private bool _hasSpeedParam;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        
        if (_animator != null)
        {
            _sitLayerIndex = _animator.GetLayerIndex(sitLayerName);

            foreach (AnimatorControllerParameter param in _animator.parameters)
            {
                if (param.name == "isSitting") _hasIsSittingParam = true;
                if (param.name == "Grounded") _hasGroundedParam = true;
                if (param.name == "FreeFall") _hasFreeFallParam = true;
                if (param.name == "Speed") _hasSpeedParam = true;
            }
        }
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "Scene_Menu") return;

        if (_animator != null && _sitLayerIndex == -1)
        {
            Debug.LogWarning($"[Pose] ATENCIÓN: No existe la capa '{sitLayerName}' en el Animator de {gameObject.name}.");
        }
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != "Scene_Menu" || _animator == null) return;

        if (_sitLayerIndex != -1)
        {
            _animator.SetLayerWeight(_sitLayerIndex, sitWeight);
        }

        if (_hasGroundedParam) _animator.SetBool("Grounded", true);
        if (_hasFreeFallParam) _animator.SetBool("FreeFall", false);
        if (_hasSpeedParam) _animator.SetFloat("Speed", 0f);

        if (_hasIsSittingParam) _animator.SetBool("isSitting", true);
    }
}
