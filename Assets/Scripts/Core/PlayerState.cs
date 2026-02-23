using Unity.Netcode;
using UnityEngine;
using StarterAssets;

public enum TipoObjeto 
{ 
    Ninguno, 
    Antorcha, 
    Pocion, 
    Daga 
}

public class PlayerState : NetworkBehaviour
{
    [Header("Identidad y Estado (Server-Auth)")]
    // [Regla de Netcode] Solo el servidor puede modificar estas variables, pero todos las pueden leer.
    public NetworkVariable<bool> isWolf = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    [Header("Inventario Autoritativo")]
    public NetworkVariable<TipoObjeto> objetoEnMano = new NetworkVariable<TipoObjeto>(
        TipoObjeto.Ninguno, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    [Header("Modelos 3D Visuales (Hijos de la Mano)")]
    [Tooltip("El modelo de la Antorcha (con su luz o partículas)")]
    [SerializeField] private GameObject modeloAntorcha;
    [Tooltip("El modelo de la Poción (que cura o da velocidad)")]
    [SerializeField] private GameObject modeloPocion;
    [Tooltip("El modelo del cuchillo letal")]
    [SerializeField] private GameObject modeloDaga;

    // Referencias a los componentes de movimiento de tu personaje de StarterAssets
    private CharacterController characterController;
    private ThirdPersonController thirdPersonController;
    private Animator animator;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        // Nos suscribimos matemáticamente: Si el servidor decreta mi muerte, mi juego reaccionará localmente.
        isDead.OnValueChanged += OnDeathStateChanged;
        
        // [Objetivo 3] Suscripción a cambios de inventario
        objetoEnMano.OnValueChanged += OnObjetoCambiado;
        
        // Comprobación de seguridad al nacer (por si un cliente se une tarde y el cuerpo ya era un cadáver)
        if (isDead.Value)
        {
            ApplyDeathPhysics();
        }

        // Estado inicial del objeto (para el que se une tarde)
        ActualizarVisualizacionObjeto(objetoEnMano.Value);
        
        // Si somos nosotros mismos, le decimos a nuestro recuadro en pantalla que arranque con lo que tenemos puesto
        if (IsOwner)
        {
            GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
            if (ui != null)
            {
                ui.ActualizarInventario(objetoEnMano.Value.ToString());
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        isDead.OnValueChanged -= OnDeathStateChanged;
        objetoEnMano.OnValueChanged -= OnObjetoCambiado;
        base.OnNetworkDespawn();
    }

    private void OnDeathStateChanged(bool estadoAnterior, bool estadoNuevo)
    {
        // Si acabamos de morir de verdad
        if (estadoNuevo == true && estadoAnterior == false)
        {
            ApplyDeathPhysics();
        }
    }

    private void ApplyDeathPhysics()
    {
        Debug.Log($"[Resolución de Combate] Reproduciendo animación de Muerte para el jugador {OwnerClientId}.");

        // [NUEVO] Le avisamos al Juez para que compruebe si la partida terminó con esta muerte
        if (IsServer)
        {
            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null) gm.CheckWinConditions();
        }

        // Desactivamos el control del motor de colisión para que sea un objeto estático
        if (characterController != null) characterController.enabled = false;
        
        // Desactivamos el script principal de movimiento (ya no puede correr ni saltar)
        if (thirdPersonController != null) thirdPersonController.enabled = false;

        // [NUEVO] ¡Ya no apagamos el Animador! Le ordenamos que dispare su animación de muerte.
        if (animator != null)
        {
            animator.SetTrigger("Muerte");
            
            // Opcional: Asegurarnos de que el jugador no siga "corriendo" visualmente en la muerte
            animator.SetFloat("Speed", 0f);
            animator.SetFloat("MotionSpeed", 0f);
        }

        // Ya no rotamos forzosamente 90 grados ni tocamos el Transform, la animación se encargará de tirarlo.
    }

    [Rpc(SendTo.Server)] // Mandado desde tu personaje (Tiene permiso Owner 100% garantizado)
    public void EnviarMiVotoServerRpc(ulong candidatoId)
    {
        // Esto solo lo lee el Servidor cuando recibe tu voto
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.RegistrarVotoCentralizado(OwnerClientId, candidatoId);
        }
    }

    // --- MÓDULO 7: INVENTARIO AUTORITATIVO ---

    // [Objetivo 2] Método que el cliente invoca para pedirle permiso al Servidor de coger un objeto.
    [Rpc(SendTo.Server)]
    public void RecogerObjetoServerRpc(TipoObjeto nuevoObjeto)
    {
        // 1. Guard de Autoridad Suprema: Si el jugador está muerto, el servidor simplemente ignora el intento de trampa.
        if (isDead.Value)
        {
            Debug.LogWarning($"[Seguridad Server] El fantasma {OwnerClientId} intentó recoger un objeto ({nuevoObjeto}). Acción Denegada.");
            return;
        }

        // 2. Modificación del estado real en el servidor. Todos los clientes serán notificados por la NetworkVariable.
        objetoEnMano.Value = nuevoObjeto;
        Debug.Log($"[Server] El jugador {OwnerClientId} ha recogido exitosamente: {nuevoObjeto}");
    }

    // [Objetivo 3] Callback disparado en las computadoras de TODO el mundo cuando el servidor cambia la variable.
    private void OnObjetoCambiado(TipoObjeto viejo, TipoObjeto nuevo)
    {
        ActualizarVisualizacionObjeto(nuevo);

        // [Nuevo HUD] Si somos el jugador de esta PC, actualizamos nuestro recuadro visual del inventario
        if (IsOwner)
        {
            GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
            if (ui != null)
            {
                ui.ActualizarInventario(nuevo.ToString());
            }
        }
    }

    // Lógica pura de visualización local
    private void ActualizarVisualizacionObjeto(TipoObjeto obj)
    {
        // Primero, apagamos todo por seguridad
        if (modeloAntorcha != null) modeloAntorcha.SetActive(false);
        if (modeloPocion != null) modeloPocion.SetActive(false);
        if (modeloDaga != null) modeloDaga.SetActive(false);

        // Encendemos solo el que diga el Servidor que tenemos
        switch (obj)
        {
            case TipoObjeto.Antorcha:
                if (modeloAntorcha != null) modeloAntorcha.SetActive(true);
                break;
            case TipoObjeto.Pocion:
                if (modeloPocion != null) modeloPocion.SetActive(true);
                break;
            case TipoObjeto.Daga:
                if (modeloDaga != null) modeloDaga.SetActive(true);
                break;
            case TipoObjeto.Ninguno:
                // No hacemos nada, ya hemos apagado todo
                break;
        }
    }

    // --- SISTEMA DE TELETRANSPORTE ANTI-BUGS DE RED ---
    
    // [Objetivo] El servidor nos ordena movernos a nosotros (el dueño local) para evitar conflictos con el CharacterController
    [ClientRpc]
    public void ForzarTeletransporteClientRpc(Vector3 nuevaPosicion, Quaternion nuevaRotacion, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"<color=cyan>[Red]</color> El servidor me ordena viajar a la asamblea: {nuevaPosicion}");
        
        // 1. Apagamos el motor físico para que no rechace el viaje por "chocar" con el aire
        if (characterController != null) characterController.enabled = false;
        if (thirdPersonController != null) thirdPersonController.enabled = false;

        // 2. Nos movemos en la realidad local del cliente (lo cual se sincronizará hacia el servidor)
        // [Parche de Altura] Elevamos medio metro para evitar que el CharacterController inicie clavado en la malla del suelo y se caiga.
        transform.position = nuevaPosicion + Vector3.up * 1.5f; // Mayor altura para evitar traspasar el suelo
        transform.rotation = nuevaRotacion;
        
        // FORZAMOS LA SINCRONIZACIÓN FÍSICA de Unity antes de volver a encender el motor
        Physics.SyncTransforms();

        // 3. Encendemos los motores de nuevo
        if (characterController != null) characterController.enabled = true;
        if (thirdPersonController != null) thirdPersonController.enabled = true;
    }
}
