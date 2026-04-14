using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla la cámara en primera persona en el lobby de forma simple y libre de fallos de huesos.
/// </summary>
public class LobbyHeadLook : NetworkBehaviour
{
    [Header("Referencias (Asignar Manualmente)")]
    [Tooltip("La cámara en primera persona que está dentro de la cabeza")]
    [SerializeField] private Camera fpCamera;
    [Tooltip("Opcional: El hueso de la cabeza solo para que otros jugadores te vean moverlo")]
    [SerializeField] private Transform headBone;

    [Header("Ajustes de Visión")]
    [SerializeField] private float mouseSensitivity = 0.2f;
    [SerializeField] private Vector2 verticalLookLimits = new Vector2(-50f, 60f); 
    [SerializeField] private Vector2 horizontalLookLimits = new Vector2(-80f, 80f);

    // Network (Sincroniza para que otros te vean mover la cabeza)
    private NetworkVariable<Vector2> _netHeadAngles = new NetworkVariable<Vector2>(
        Vector2.zero, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner
    );

    private float _pitch = 0f;
    private float _yaw = 0f;
    private Quaternion _initialHeadRot;

    public override void OnNetworkSpawn()
    {
        if (SceneManager.GetActiveScene().name != "Scene_Menu")
        {
            enabled = false; 
            if (fpCamera != null) fpCamera.enabled = false;
            return;
        }

        if (headBone != null)
        {
            _initialHeadRot = headBone.localRotation;
        }

        if (fpCamera != null)
        {
            // Asegurarnos de que el GameObject esté encendido si estaba apagado en el prefab
            fpCamera.gameObject.SetActive(IsOwner);
            fpCamera.enabled = IsOwner;
            if (fpCamera.TryGetComponent(out AudioListener listener)) listener.enabled = IsOwner;
        }

        if (IsOwner)
        {
            // [FIX] Desactivamos SOLO el componente Camera de la MainCamera.
            Camera[] todasLasCams = Resources.FindObjectsOfTypeAll<Camera>();
            foreach (Camera cam in todasLasCams)
            {
                if (cam != fpCamera && cam.CompareTag("MainCamera"))
                {
                    cam.enabled = false; // Solo el componente
                }
            }

            // Apagar TODOS los AudioListeners extra de golpe para evitar SPAM extremo en consola que oculte los prints
            AudioListener[] todosLosListeners = Resources.FindObjectsOfTypeAll<AudioListener>();
            foreach (AudioListener listener in todosLosListeners)
            {
                if (fpCamera != null && listener.gameObject == fpCamera.gameObject) 
                    continue; // No apagar nuestro propio listener
                
                listener.enabled = false;
            }

            // Bloqueamos el ratón al nacer para el Free-Look inmersivo
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (SceneManager.GetActiveScene().name != "Scene_Menu") return;
        if (fpCamera == null) return;

        // [NUEVO] Eliminamos el chequeo de click derecho. Ahora girar es SIEMPRE libre.
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector2 delta = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();

            _yaw += delta.x * mouseSensitivity;
            _yaw = Mathf.Clamp(_yaw, horizontalLookLimits.x, horizontalLookLimits.y);

            _pitch -= delta.y * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, verticalLookLimits.x, verticalLookLimits.y);
            
            _netHeadAngles.Value = new Vector2(_pitch, _yaw);
        }
    }

    private void LateUpdate()
    {
        if (IsOwner)
        {
            if (fpCamera != null)
            {
                // CLAVE: Tomamos SOLO el eje Y del cuerpo (hacia dónde mira horizontalmente el personaje).
                float bodyYaw = transform.eulerAngles.y;
                fpCamera.transform.rotation = Quaternion.Euler(_pitch, bodyYaw + _yaw, 0);
            }
        }
        else
        {
            if (headBone != null)
            {
                float netPitch = _netHeadAngles.Value.x;
                float netYaw = _netHeadAngles.Value.y;
                headBone.localRotation = _initialHeadRot * Quaternion.Euler(netPitch, netYaw, 0);
            }
        }
    }

    private void OnDisable()
    {
        if (IsOwner)
        {
            // [FIX] Re-activamos el componente Camera de la Main Camera (no el GO).
            Camera[] todasLasCams = Resources.FindObjectsOfTypeAll<Camera>();
            foreach (Camera cam in todasLasCams)
            {
                if (cam.CompareTag("MainCamera"))
                {
                    cam.enabled = true;
                    if (cam.TryGetComponent(out AudioListener listener))
                        listener.enabled = true;
                    break;
                }
            }

            if (SceneManager.GetActiveScene().name == "Scene_Menu")
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
