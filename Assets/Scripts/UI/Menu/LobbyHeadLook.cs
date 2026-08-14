using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
 
/// <summary>
/// Cámara/cabeza en el lobby. El propietario mira con el ratón; los demás reciben
/// los ángulos por red; los aldeanos de escena (sin red) simulan mirada idle.
///
/// CORRECCIONES CLAVE (por qué la cabeza no se movía):
/// - [DefaultExecutionOrder] muy alto: garantiza que este LateUpdate corre DESPUÉS
///   de cualquier otro script del lobby que toque el Animator/huesos.
/// - Detección de hueso robusta: primero Humanoid (Animator.GetBoneTransform),
///   luego coincidencia flexible por nombre (Contains "head"/"neck"), con error claro.
/// - El movimiento se aplica SOBRE la pose animada de la cabeza cada frame
///   (additivo), así no pelea con la animación de "sentado" del Lobby Layer.
/// - Logs de diagnóstico en OnNetworkSpawn/Start para saber por qué no entra el input.
/// </summary>
[DefaultExecutionOrder(10000)]
public class LobbyHeadLook : NetworkBehaviour
{
    private const string LOBBY_SCENE = "Scene_Menu";
 
    [Header("Referencias (Asignar Manualmente)")]
    [Tooltip("La cámara en primera persona que está dentro de la cabeza")]
    [SerializeField] private Camera fpCamera;
    [Tooltip("Opcional: El hueso de la cabeza para que otros jugadores te vean moverla")]
    [SerializeField] private Transform headBone;
 
    [Header("Ajustes de Visión")]
    [SerializeField] private float mouseSensitivity = 0.2f;
    [SerializeField] private Vector2 verticalLookLimits = new Vector2(-50f, 60f);
    [SerializeField] private Vector2 horizontalLookLimits = new Vector2(-80f, 80f);
 
    [Header("Desfase de Orientación")]
    [Tooltip("Ángulo de desfase para alinear la cámara con el frente del modelo")]
    [SerializeField] private float yawOffset = 180f;
 
    public enum EjeRotacion { X, Y, Z }
 
    [Header("Mapeo de Ejes (Synty Models)")]
    [SerializeField] private EjeRotacion ejePitch = EjeRotacion.X;
    [SerializeField] private EjeRotacion ejeYaw = EjeRotacion.Y;
 
    [Header("Corrección de Hueso (Modelos 3D)")]
    [SerializeField] private bool invertirPitchCabeza = false;
    [SerializeField] private bool invertirYawCabeza = false;
 
    [Header("Base de Rotación")]
    [Tooltip("Si está en true, la rotación se aplica sobre la pose inicial del hueso (ignora la animación de la cabeza). Si está en false, se aplica de forma aditiva sobre la animación del frame actual.")]
    [SerializeField] private bool usarPoseInicialComoBase = true;
 
    [Header("Diagnóstico")]
    [Tooltip("Pon esto en true para ver en consola por qué no se mueve la cabeza")]
    [SerializeField] private bool debugLogs = true;
 
    // Sincroniza para que otros te vean mover la cabeza
    private NetworkVariable<Vector2> _netHeadAngles = new NetworkVariable<Vector2>(
        Vector2.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
 
    private float _pitch = 0f;
    private float _yaw = 0f;
    private Quaternion _initialHeadRot = Quaternion.identity;
    private float _lastLoggedPitch = -999f;
    private float _lastLoggedYaw = -999f;
 
    [Header("Simulación de Mirada (Aldeanos Idle)")]
    [SerializeField] private bool simularMiradaIdle = true;
    private float _simTargetPitch, _simTargetYaw, _simCurrentPitch, _simCurrentYaw, _simTimer, _simSpeed = 2f;
 
    private void Start()
    {
        ResolverHeadBone();
        if (headBone != null)
        {
            _initialHeadRot = headBone.localRotation;
            Debug.Log($"[LobbyHeadLook] {name}: Start - Hueso '{headBone.name}' configurado. Rotación inicial: {_initialHeadRot.eulerAngles}. UsarPoseInicialComoBase={usarPoseInicialComoBase}");
        }
        else
        {
            Debug.LogError($"[LobbyHeadLook] {name}: Start - NO se pudo resolver el hueso de la cabeza (headBone es NULL).");
        }
    }
 
    private void ResolverHeadBone()
    {
        if (headBone != null) return;
 
        // 1) Mejor opción: avatar Humanoid con jerarquía NO optimizada
        var animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            if (headBone == null) headBone = animator.GetBoneTransform(HumanBodyBones.Neck);
            if (headBone != null && debugLogs)
                Debug.Log($"[LobbyHeadLook] {name}: hueso Humanoid '{headBone.name}' detectado.");
        }
 
        // 2) Fallback: coincidencia flexible por nombre
        if (headBone == null)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLower();
                if (n.Contains("head") || n.Contains("neck")) { headBone = t; break; }
            }
            if (headBone != null && debugLogs)
                Debug.Log($"[LobbyHeadLook] {name}: hueso por nombre '{headBone.name}' detectado.");
        }
 
        if (headBone == null)
            Debug.LogError($"[LobbyHeadLook] {name}: NO se encontró el hueso de la cabeza. " +
                           $"Asigna 'headBone' a mano en el Inspector. Si el modelo es Humanoid con " +
                           $"'Optimize Game Objects' activado, los huesos no existen como Transform: " +
                           $"desactívalo o expón la cabeza en 'Extra Transforms to Expose'.");
    }
 
    public override void OnNetworkSpawn()
    {
        if (SceneManager.GetActiveScene().name != LOBBY_SCENE)
        {
            enabled = false; // fuera del lobby no hacemos nada
            return;
        }
 
        if (debugLogs)
            Debug.Log($"[LobbyHeadLook] {name}: OnNetworkSpawn IsOwner={IsOwner} IsSpawned={IsSpawned} headBone={(headBone ? headBone.name : "NULL")}");
 
        if (fpCamera != null)
        {
            fpCamera.gameObject.SetActive(IsOwner);
            fpCamera.enabled = IsOwner;
            if (fpCamera.TryGetComponent(out AudioListener listener)) listener.enabled = IsOwner;
        }
 
        if (IsOwner)
        {
            foreach (Camera cam in Resources.FindObjectsOfTypeAll<Camera>())
                if (cam != fpCamera && cam.CompareTag("MainCamera")) cam.enabled = false;
 
            foreach (AudioListener l in Resources.FindObjectsOfTypeAll<AudioListener>())
                if (!(fpCamera != null && l.gameObject == fpCamera.gameObject)) l.enabled = false;
 
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
 
    private void Update()
    {
        // Caso 1: objeto de red controlado por el jugador local
        if (IsSpawned && IsOwner)
        {
            if (SceneManager.GetActiveScene().name != LOBBY_SCENE) return;
 
            var mouse = Mouse.current;
            if (mouse == null)
            {
                if (debugLogs && Time.frameCount % 300 == 0)
                    Debug.LogWarning($"[LobbyHeadLook] {name}: Mouse.current es null.");
                return;
            }
 
            Vector2 delta = mouse.delta.ReadValue();
            if (delta.sqrMagnitude > 0.0001f)
            {
                _yaw = Mathf.Clamp(_yaw + delta.x * mouseSensitivity, horizontalLookLimits.x, horizontalLookLimits.y);
                _pitch = Mathf.Clamp(_pitch - delta.y * mouseSensitivity, verticalLookLimits.x, verticalLookLimits.y);
                _netHeadAngles.Value = new Vector2(_pitch, _yaw);
                
                if (debugLogs)
                {
                    Debug.Log($"[LobbyHeadLook] {name} (Local Owner): Movimiento ratón detectado. delta={delta}, pitch={_pitch}, yaw={_yaw}");
                }
            }
            return;
        }
 
        // Caso 2: aldeanos de escena estáticos (no spawned por red)
        if (!IsSpawned && simularMiradaIdle)
        {
            _simTimer -= Time.deltaTime;
            if (_simTimer <= 0f)
            {
                _simTargetYaw = Random.Range(-40f, 40f);
                _simTargetPitch = Random.Range(-12f, 15f);
                _simTimer = Random.Range(3f, 7f);
                _simSpeed = Random.Range(0.8f, 2f);
            }
            _simCurrentYaw = Mathf.MoveTowards(_simCurrentYaw, _simTargetYaw, Time.deltaTime * _simSpeed * 15f);
            _simCurrentPitch = Mathf.MoveTowards(_simCurrentPitch, _simTargetPitch, Time.deltaTime * _simSpeed * 10f);
        }
    }
 
    private void LateUpdate()
    {
        // 1. Hueso de la cabeza PRIMERO.
        //    Si la fpCamera es hija del hueso, moverlo arrastra la cámara; por eso
        //    fijamos la rotación de la cámara DESPUÉS (paso 2) para que mande el ratón.
        if (headBone != null)
        {
            float finalPitch, finalYaw;
            if (IsSpawned)
            {
                if (IsOwner) { finalPitch = _pitch; finalYaw = _yaw; }
                else { finalPitch = _netHeadAngles.Value.x; finalYaw = _netHeadAngles.Value.y; }
            }
            else { finalPitch = _simCurrentPitch; finalYaw = _simCurrentYaw; }
 
            if (invertirPitchCabeza) finalPitch = -finalPitch;
            if (invertirYawCabeza) finalYaw = -finalYaw;
 
            Vector3 offsetEuler = Vector3.zero;
            SetAxis(ref offsetEuler, ejePitch, finalPitch);
            SetAxis(ref offsetEuler, ejeYaw, finalYaw);
 
            // Si usarPoseInicialComoBase es true, ignoramos los movimientos animados de la cabeza en el clip
            // y aplicamos la rotación desde la pose de sentado capturada al inicio.
            string source = IsSpawned ? (IsOwner ? "Owner Local" : "Net Client") : "Simulated Idle";
            Quaternion targetRot;
            if (usarPoseInicialComoBase)
            {
                targetRot = _initialHeadRot * Quaternion.Euler(offsetEuler);
            }
            else
            {
                targetRot = headBone.localRotation * Quaternion.Euler(offsetEuler);
            }
            
            headBone.localRotation = targetRot;
 
            if (debugLogs && (Mathf.Abs(finalPitch - _lastLoggedPitch) > 0.05f || Mathf.Abs(finalYaw - _lastLoggedYaw) > 0.05f))
            {
                _lastLoggedPitch = finalPitch;
                _lastLoggedYaw = finalYaw;
                Debug.Log($"[LobbyHeadLook] {name} ({source}): Hueso '{headBone.name}' asignado -> Rotación Local: {targetRot.eulerAngles} (offset Pitch: {finalPitch}, Yaw: {finalYaw}), usarPoseInicialComoBase={usarPoseInicialComoBase}");
            }
        }
 
        // 2. Cámara en primera persona (solo dueño), fijada al FINAL para que el
        //    movimiento del ratón mande sobre la rotación, sin que la arrastre el
        //    hueso de la cabeza ni la animación de sentado.
        if (IsSpawned && IsOwner && fpCamera != null)
        {
            float bodyYaw = transform.eulerAngles.y;
            fpCamera.transform.rotation = Quaternion.Euler(_pitch, bodyYaw + yawOffset + _yaw, 0f);
        }
    }
 
    private static void SetAxis(ref Vector3 euler, EjeRotacion eje, float valor)
    {
        if (eje == EjeRotacion.X) euler.x = valor;
        else if (eje == EjeRotacion.Y) euler.y = valor;
        else euler.z = valor;
    }
 
    private void OnDisable()
    {
        if (!IsOwner) return;
 
        foreach (Camera cam in Resources.FindObjectsOfTypeAll<Camera>())
        {
            if (cam.CompareTag("MainCamera"))
            {
                cam.enabled = true;
                if (cam.TryGetComponent(out AudioListener listener)) listener.enabled = true;
                break;
            }
        }
 
        if (SceneManager.GetActiveScene().name == LOBBY_SCENE)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
