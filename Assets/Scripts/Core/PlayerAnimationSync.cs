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

    // Identificadores de las animaciones del Starter Assets
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    private float _lastSpeedSync;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void Update()
    {
        // [NGO Guard] No procesamos red hasta que el objeto esté spawneado y el manager esté activo.
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || !IsSpawned) return; 
        
        if (_animator == null) return;

        if (IsOwner)
        {
            // Lógica LOCAL: Recojo lo que mi mando (o teclado) calculó para mis animaciones.
            float currentSpeed = _animator.GetFloat(_animIDSpeed);
            bool currentGrounded = _animator.GetBool(_animIDGrounded);
            bool currentJump = _animator.GetBool(_animIDJump);
            bool currentFreeFall = _animator.GetBool(_animIDFreeFall);
            float currentMotionSpeed = _animator.GetFloat(_animIDMotionSpeed);
            
            // Para no saturar el servidor, solo avisamos cuando realmente hay un cambio visible.
            if (Mathf.Abs(currentSpeed - _lastSpeedSync) > 0.05f || 
                netGrounded.Value != currentGrounded || 
                netJump.Value != currentJump || 
                netFreeFall.Value != currentFreeFall)
            {
                // [Regla 1] El dueño del personaje pide al Servidor actualizar estos estados a todo el mundo.
                SubmitAnimationsServerRpc(currentSpeed, currentGrounded, currentJump, currentFreeFall, currentMotionSpeed);
                _lastSpeedSync = currentSpeed;
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
        }
    }

    [ServerRpc]
    private void SubmitAnimationsServerRpc(float speed, bool grounded, bool jump, bool freeFall, float motionSpeed)
    {
        // Lógica SERVIDOR: Valida y actualiza universalmente el estado visual.
        netSpeed.Value = speed;
        netGrounded.Value = grounded;
        netJump.Value = jump;
        netFreeFall.Value = freeFall;
        netMotionSpeed.Value = motionSpeed;
    }
}
