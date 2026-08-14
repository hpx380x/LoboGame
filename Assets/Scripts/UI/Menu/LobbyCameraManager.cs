using System.Collections;
using UnityEngine;

/// <summary>
/// Director de Cámara del Lobby.
/// Gestiona la transición entre el Menú Principal (Pergamino) y el seguimiento del jugador.
/// Resuelve conflictos con cámaras internas de los prefabs (como CameraFPSLobby).
/// </summary>
public class LobbyCameraManager : MonoBehaviour
{
    [Header("Referencias de Cámara y HUD")]
    [Tooltip("Arrastra aquí la Main Camera de la escena")]
    [SerializeField] private Camera _camPrincipal;

    [Header("Ancla Menú (posición fija cinematográfica)")]
    [Tooltip("Transform vacío que marca DÓNDE y HACIA DÓNDE mira la cámara en el Menú Principal")]
    [SerializeField] private Transform anclaMenu;

    [Header("Modo Seguimiento de Personaje")]
    [Tooltip("Offset de posición respecto al personaje seguido")]
    [SerializeField] private Vector3 offsetCamara = new Vector3(0f, 2.2f, -3.5f);

    [Tooltip("Altura del punto donde la cámara mira")]
    [SerializeField] private float alturaObjetivo = 1.5f;

    [Header("Configuración de Transición")]
    [SerializeField] private float velocidadTransicion = 2.0f;
    [SerializeField] private float velocidadSeguimiento = 5.0f;

    // ─── Estado interno ───────────────────────────────────
    private Coroutine _transicionActiva;
    private Transform _objetivoSeguimiento = null;
    private bool _modoSeguimiento = false;

    private void Awake()
    {
        // Buscamos la cámara principal si no está asignada en el inspector
        if (_camPrincipal == null) _camPrincipal = Camera.main;
        if (_camPrincipal == null) _camPrincipal = FindAnyObjectByType<Camera>();
    }

    private void Start()
    {
        // Estado inicial: Menú
        ActualizarPosicionMenu();
    }

    /// <summary>
    /// Colocal la cámara en el pergamino y anula cualquier cámara intrusa.
    /// </summary>
    public void ActualizarPosicionMenu()
    {
        if (anclaMenu != null && _camPrincipal != null)
        {
            _modoSeguimiento = false;
            _objetivoSeguimiento = null;

            // Aseguramos que la Main Camera esté activa y habilitada
            _camPrincipal.gameObject.SetActive(true);
            _camPrincipal.enabled = true;

            // Posicionamos la cámara en el ancla del menú
            _camPrincipal.transform.SetPositionAndRotation(anclaMenu.position, anclaMenu.rotation);

            // Apagamos cámaras conflictivas (CameraFPSLobby, Camara_Estudio, etc.)
            GestionarConflictosCamara(true);

            Debug.Log("[LobbyCameraManager] Vista de Menú activada. Superposiciones eliminadas.");
        }
    }

    /// <summary>
    /// Desactiva cámaras de jugador o de estudio mientras estamos en el menú.
    /// </summary>
    private void GestionarConflictosCamara(bool modoMenu)
    {
        Camera[] todasLasCams = Resources.FindObjectsOfTypeAll<Camera>();
        foreach (Camera cam in todasLasCams)
        {
            if (cam == _camPrincipal) continue;

            // Filtramos solo cámaras FPS de jugador (Camara_Estudio se gestiona manualmente)
            if (cam.name.Contains("FPS") || cam.transform.root.name.Contains("Player"))
            {
                // Solo actuamos sobre objetos de escena activos o que podamos controlar
                if (cam.gameObject.scene.isLoaded)
                {
                    cam.gameObject.SetActive(!modoMenu);
                    
                    // Gestionamos AudioListener
                    AudioListener listener = cam.GetComponent<AudioListener>();
                    if (listener != null) listener.enabled = !modoMenu;
                }
            }
        }

        // Listener principal siempre on en el menú
        if (modoMenu && _camPrincipal != null)
        {
             AudioListener mainListener = _camPrincipal.GetComponent<AudioListener>();
             if (mainListener != null) mainListener.enabled = true;
        }
    }


    private void LateUpdate()
    {
        if (!_modoSeguimiento || _objetivoSeguimiento == null || _camPrincipal == null) return;

        Vector3 posDeseada = _objetivoSeguimiento.TransformPoint(offsetCamara);
        Vector3 puntoMirada = _objetivoSeguimiento.position + Vector3.up * alturaObjetivo;

        _camPrincipal.transform.position = Vector3.Lerp(_camPrincipal.transform.position, posDeseada, Time.deltaTime * velocidadSeguimiento);
        Quaternion rotDeseada = Quaternion.LookRotation(puntoMirada - _camPrincipal.transform.position);
        _camPrincipal.transform.rotation = Quaternion.Slerp(_camPrincipal.transform.rotation, rotDeseada, Time.deltaTime * velocidadSeguimiento);
    }

    public void SeguirPersonaje(Transform personaje)
    {
        if (personaje == null) return;

        _objetivoSeguimiento = personaje;
        if (_transicionActiva != null) StopCoroutine(_transicionActiva);
        
        _modoSeguimiento = true;

        // Al empezar a seguir al personaje, permitimos sus cámaras si las tiene
        GestionarConflictosCamara(false);

        Debug.Log($"[LobbyCameraManager] Siguiendo a {personaje.name}. Cámaras de jugador permitidas.");
    }

    public void ActivarVistaMenu()
    {
        ActualizarPosicionMenu(); // Reutilizamos la lógica de limpieza
    }

    private void IniciarTransicionFija(Transform destino)
    {
        if (_camPrincipal == null) return;
        if (_transicionActiva != null) StopCoroutine(_transicionActiva);
        _transicionActiva = StartCoroutine(LerpHaciaAncla(destino));
    }

    private IEnumerator LerpHaciaAncla(Transform destino)
    {
        Vector3 posInicial = _camPrincipal.transform.position;
        Quaternion rotInicial = _camPrincipal.transform.rotation;
        float progreso = 0f;

        while (progreso < 1f)
        {
            progreso += Time.deltaTime * velocidadTransicion;
            float curva = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progreso));
            _camPrincipal.transform.position = Vector3.Lerp(posInicial, destino.position, curva);
            _camPrincipal.transform.rotation = Quaternion.Slerp(rotInicial, destino.rotation, curva);
            yield return null;
        }

        _camPrincipal.transform.SetPositionAndRotation(destino.position, destino.rotation);
        _transicionActiva = null;
    }
}

