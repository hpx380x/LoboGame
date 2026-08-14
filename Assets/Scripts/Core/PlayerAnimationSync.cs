using Unity.Netcode;
using UnityEngine;

public class PlayerAnimationSync : NetworkBehaviour
{
    private Animator _animator;
    
    // [Regla 2] Usamos NetworkVariable para estados continuos (Velocidad, Toco el Suelo, etc)
    // Sólo el Servidor tiene derecho a escribirlas por defecto (Regla 1 Server Auth).
    private NetworkVariable<float> netSpeed = new NetworkVariable<float>(0f);
    private NetworkVariable<bool> netGrounded = new NetworkVariable<bool>(true);
    private NetworkVariable<bool> netJump = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> netFreeFall = new NetworkVariable<bool>(false);
    private NetworkVariable<float> netMotionSpeed = new NetworkVariable<float>(0f);
    private NetworkVariable<bool> netIsInteracting = new NetworkVariable<bool>(false);
    private NetworkVariable<int> netInteractionType = new NetworkVariable<int>(0);

    // Identificadores de las animaciones del Starter Assets
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;
    private int _animIDIsInteracting;
    private int _animIDInteractionType;

    private float _lastSpeedSync;
    private float _timeSinceLastSync = 0f;
    private const float SYNC_COOLDOWN = 0.1f;

    private Quests.PlayerTransformation _transformation;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        _animIDIsInteracting = Animator.StringToHash("IsInteracting");
        _animIDInteractionType = Animator.StringToHash("InteractionType");
    }

    private void Start()
    {
        _transformation = GetComponent<Quests.PlayerTransformation>();
        if (_transformation != null)
        {
            _transformation.OnAnimatorChanged += HandleAnimatorChanged;
            UpdateAnimatorReference();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (_transformation != null)
        {
            _transformation.OnAnimatorChanged -= HandleAnimatorChanged;
        }
    }

    private void HandleAnimatorChanged(Animator newAnimator)
    {
        _animator = newAnimator;
        Debug.Log($"[PlayerAnimationSync] Referencia de Animator de red actualizada a: {(newAnimator != null ? newAnimator.gameObject.name : "NULO")}");
    }

    private void UpdateAnimatorReference()
    {
        // 1. Buscar en los hijos activos con controlador asignado
        foreach (var anim in GetComponentsInChildren<Animator>())
        {
            if (anim.gameObject != this.gameObject && anim.isActiveAndEnabled && anim.runtimeAnimatorController != null)
            {
                _animator = anim;
                return;
            }
        }
        // 2. Si no hay, usar el de la raíz
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // [NGO Guard] No procesamos red hasta que el objeto esté spawneado y el manager esté activo.
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || !IsSpawned) return; 
        
        if (_animator == null) return;

        if (IsOwner)
        {
            _timeSinceLastSync += Time.deltaTime;

            // Lógica LOCAL: Recojo lo que mi mando (o teclado) calculó para mis animaciones.
            float currentSpeed = _animator.GetFloat(_animIDSpeed);
            bool currentGrounded = _animator.GetBool(_animIDGrounded);
            bool currentJump = _animator.GetBool(_animIDJump);
            bool currentFreeFall = _animator.GetBool(_animIDFreeFall);
            float currentMotionSpeed = _animator.GetFloat(_animIDMotionSpeed);
            bool currentIsInteracting = _animator.GetBool(_animIDIsInteracting);
            int currentInteractionType = _animator.GetInteger(_animIDInteractionType);
            
            // Separar cambios discretos (críticos) de cambios de velocidad continua
            bool criticalChange = netGrounded.Value != currentGrounded || 
                                 netJump.Value != currentJump || 
                                 netFreeFall.Value != currentFreeFall ||
                                 netIsInteracting.Value != currentIsInteracting ||
                                 netInteractionType.Value != currentInteractionType;

            bool speedChange = Mathf.Abs(currentSpeed - _lastSpeedSync) > 0.05f;

            // Para no saturar el servidor, solo avisamos cuando realmente hay un cambio visible.
            // Los cambios de velocidad se rate-limitan a 10 Hz (SYNC_COOLDOWN), pero saltos y estados de interacción se envían al instante.
            if (criticalChange || (speedChange && _timeSinceLastSync >= SYNC_COOLDOWN))
            {
                // [Regla 1] El dueño del personaje pide al Servidor actualizar estos estados a todo el mundo.
                SubmitAnimationsServerRpc(currentSpeed, currentGrounded, currentJump, currentFreeFall, currentMotionSpeed, currentIsInteracting, currentInteractionType);
                _lastSpeedSync = currentSpeed;
                _timeSinceLastSync = 0f;
            }
        }
        else
        {
            // Lógica CLON: Si no es mi personaje, leo la red y aplico la animación a su esqueleto.
            _animator.SetFloat(_animIDSpeed, netSpeed.Value);
            _animator.SetBool(_animIDGrounded, netGrounded.Value);
            _animator.SetBool(_animIDJump, netJump.Value);
            _animator.SetBool(_animIDFreeFall, netFreeFall.Value);
            _animator.SetFloat(_animIDMotionSpeed, netMotionSpeed.Value);
            _animator.SetBool(_animIDIsInteracting, netIsInteracting.Value);
            _animator.SetInteger(_animIDInteractionType, netInteractionType.Value);
        }
    }

    [ServerRpc]
    private void SubmitAnimationsServerRpc(float speed, bool grounded, bool jump, bool freeFall, float motionSpeed, bool isInteracting, int interactionType)
    {
        // Lógica SERVIDOR: Valida y actualiza universalmente el estado visual.
        netSpeed.Value = speed;
        netGrounded.Value = grounded;
        netJump.Value = jump;
        netFreeFall.Value = freeFall;
        netMotionSpeed.Value = motionSpeed;
        netIsInteracting.Value = isInteracting;
        netInteractionType.Value = interactionType;
    }
}
