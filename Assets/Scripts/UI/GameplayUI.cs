using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// HUD de gameplay. Gestiona:
/// - Texto de rol (revelación al inicio)
/// - Lista de tareas dinámica con estados (Disponible / EnProgreso / Completada / Fallada)
/// - Lock de tarea activa (solo una a la vez)
/// - Barra de progreso de minijuegos
/// - Mensajes de proximidad
/// - Inventario en mano
/// </summary>
public class GameplayUI : MonoBehaviour
{
    // ─── Referencias Inspector ────────────────────────────────────────────────

    [Header("Presentación de Rol")]
    [SerializeField] private TextMeshProUGUI textoRol;

    [Header("Fase del día")]
    [SerializeField] private TextMeshProUGUI textoFase;

    [Header("Panel de Tareas")]
    [SerializeField] private GameObject taskPanel;
    [SerializeField] private TextMeshProUGUI taskText;

    [Header("Mensaje de Proximidad")]
    [Tooltip("Texto pequeño en pantalla que avisa qué hacer cerca de una tarea.")]
    [SerializeField] private TextMeshProUGUI textoProximidad;

    [Header("Inventario")]
    [SerializeField] private TextMeshProUGUI textoInventario;
    [SerializeField] private TextMeshProUGUI textoMonedas;
    [SerializeField] private UnityEngine.UI.Image fondoInventario;

    [Header("Barra de Progreso")]
    [SerializeField] private GameObject contenedorBarra;
    [SerializeField] private UnityEngine.UI.Slider barraProgreso;

    [Header("Pergamino Animación (3D)")]
    [SerializeField] private GameObject pergaminoHudRoot;
    [SerializeField] private Animator scroll3DAnimator;

    [Header("Espectador")]
    [SerializeField] private GameObject panelEspectador;
    [SerializeField] private TextMeshProUGUI textoNombreEspectador;
    
    private bool isScrollOpen = false;

    // ─── Estado interno ───────────────────────────────────────────────────────

    // Lista de tareas del día actual (privada, se recibe del servidor)
    private List<TaskInfo> tareasActuales = new List<TaskInfo>();

    // ID de la tarea en la que el jugador está bloqueado (null = libre)
    private string tareaActivaId = null;

    private bool esLobo = false;
    private Coroutine corutinaProximidad;
    private Coroutine corutinaRol;

    private void Awake()
    {
        if (pergaminoHudRoot != null)
        {
            pergaminoHudRoot.SetActive(false);
        }

        if (panelEspectador != null)
        {
            panelEspectador.SetActive(false);
        }
    }

    public void ToggleScroll(bool forceState)
    {
        isScrollOpen = forceState;
        Debug.Log($"[PergaminoLog-UI] Recibiendo ToggleScroll de Cliente. Estado deseado: {forceState}");
        
        if (pergaminoHudRoot != null)
        {
            if (isScrollOpen) 
            {
                pergaminoHudRoot.SetActive(true);
                Debug.Log("[PergaminoLog-UI] Activando contenedor Raíz del pergamino en la UI.");
            }
            
            if (scroll3DAnimator != null)
            {
                // Disparamos la animación del modelo 3D
                scroll3DAnimator.SetBool("isOpen", isScrollOpen);
                Debug.Log($"[PergaminoLog-UI] Disparando flecha del Animator a isOpen={isScrollOpen}");
            }
            else
            {
                Debug.LogWarning("[PergaminoLog-UI] ¡Falta Animator! No se asignó scroll3DAnimator en el Inspector de GameplayUI.");
            }

            // Si cerramos, desactivamos tras un breve delay 
            if (!isScrollOpen) 
            {
                Debug.Log("[PergaminoLog-UI] Cerrando... Iniciando corutina para apagar la malla en 0.5s.");
                StartCoroutine(OcultarScrollRutina());
            }
        }
        else
        {
            Debug.LogError("[PergaminoLog-UI] ¡ERROR CRÍTICO! pergaminoHudRoot es nulo. ¡Asígnalo en el Inspector!");
        }
    }

    private IEnumerator OcultarScrollRutina()
    {
        // Esperamos a que termine la animación de cierre antes de apagar el objeto
        yield return new WaitForSeconds(0.5f);
        if (!isScrollOpen && pergaminoHudRoot != null)
            pergaminoHudRoot.SetActive(false);
    }

    // ─── API: Rol ─────────────────────────────────────────────────────────────

    public void MostrarRol(string rol)
    {
        if (textoRol == null) return;
        textoRol.text = rol;
        textoRol.gameObject.SetActive(true);
        if (corutinaRol != null) StopCoroutine(corutinaRol);
        corutinaRol = StartCoroutine(OcultarRolRutina());
    }

    private IEnumerator OcultarRolRutina()
    {
        yield return new WaitForSeconds(3f);
        if (textoRol != null) textoRol.gameObject.SetActive(false);
    }

    // ─── API: Lista de Tareas ─────────────────────────────────────────────────

    /// <summary>
    /// Llamado por la ClientRpc del GameManager cuando el servidor asigna las tareas del día.
    /// Recibe 2 tareas disponibles; el jugador elige cuál empezar primero.
    /// El lobo también recibe tareas reales (para camuflarse).
    /// </summary>
    public void ActualizarListaTareas(List<TaskInfo> tareas, bool jugadorEsLobo)
    {
        tareasActuales = tareas;
        tareaActivaId  = null;   // Liberar cualquier bloqueo anterior
        esLobo         = jugadorEsLobo;

        if (taskPanel != null) taskPanel.SetActive(true);
        RedibujarListaTareas();
    }

    /// <summary>Devuelve si una tarea ya está terminada (completada O fallada).</summary>
    public bool TareaEstaTerminada(string taskId)
    {
        TaskInfo t = tareasActuales.Find(x => x.taskId == taskId);
        return t != null && t.EstaTerminada;
    }

    // ─── API: Bloqueo de tarea activa ─────────────────────────────────────────

    /// <summary>El TaskPoint llama esto al empezar un minijuego. Bloquea otras tareas.</summary>
    public void SetTareaActiva(string taskId)
    {
        tareaActivaId = taskId;

        TaskInfo t = tareasActuales.Find(x => x.taskId == taskId);
        if (t != null) t.estado = EstadoTarea.EnProgreso;

        RedibujarListaTareas();
    }

    /// <summary>El ID de la tarea actualmente bloqueada (null si ninguna).</summary>
    public string GetTareaActivaId() => tareaActivaId;

    /// <summary>
    /// El jugador completó la tarea activa.
    /// Marca como Completada y avanza automáticamente a la siguiente Disponible.
    /// </summary>
    public void CompletarTareaActiva(string taskId)
    {
        TaskInfo t = tareasActuales.Find(x => x.taskId == taskId);
        if (t != null) t.estado = EstadoTarea.Completada;

        tareaActivaId = null;
        RedibujarListaTareas();

        MostrarMensajeTarea($"<color=green>✓ '{t?.nombreTarea}' completada.</color>", 3f);

        AvanzarSiguienteTareaDisponible();
    }

    /// <summary>
    /// El jugador falló la tarea activa (se alejó en progreso).
    /// Marca como Fallada y avanza a la siguiente Disponible, si la hay.
    /// </summary>
    public void FallarTareaActiva(string taskId, string motivo)
    {
        TaskInfo t = tareasActuales.Find(x => x.taskId == taskId);
        if (t != null) t.estado = EstadoTarea.Fallada;

        tareaActivaId = null;
        RedibujarListaTareas();

        MostrarMensajeTarea($"<color=red>✗ {motivo}</color>", 3f);

        AvanzarSiguienteTareaDisponible();
    }

    // ─── Lógica interna de avance ─────────────────────────────────────────────

    /// <summary>
    /// Tras completar/fallar, busca la próxima tarea Disponible y la resalta en amarillo.
    /// Si no queda ninguna, muestra el mensaje de fin de turno.
    /// </summary>
    private void AvanzarSiguienteTareaDisponible()
    {
        TaskInfo siguiente = tareasActuales.Find(x => x.EstaDisponible);
        if (siguiente != null)
        {
            // Resaltar la siguiente en la lista – el TaskPoint ya tiene el tint dorado por su propia lógica.
            MostrarMensajeTarea($"<color=yellow>Siguiente tarea: {siguiente.nombreTarea}</color>", 3f);
        }
        else
        {
            // Sin tareas restantes para este turno
            bool alguienCompletada = tareasActuales.Exists(x => x.estado == EstadoTarea.Completada);
            string mensajeFinal = alguienCompletada
                ? "<color=green>¡Turno completado! Buen trabajo.</color>"
                : "<color=orange>Sin tareas completadas. Mañana tendrás nuevas tareas.</color>";
            MostrarMensajeTarea(mensajeFinal, 5f);
        }
    }

    // ─── Dibujar lista ────────────────────────────────────────────────────────

    private void RedibujarListaTareas()
    {
        if (taskText == null) return;

        var sb = new System.Text.StringBuilder();

        if (esLobo)
            sb.AppendLine("<color=red><s>SIMULAR TAREAS:</s></color>");
        else
            sb.AppendLine("<color=yellow>MIS TAREAS HOY:</color>");

        foreach (var t in tareasActuales)
        {
            switch (t.estado)
            {
                case EstadoTarea.Disponible:
                    sb.AppendLine($"  ☐ {t.nombreTarea}");
                    break;
                case EstadoTarea.EnProgreso:
                    sb.AppendLine($"  <color=yellow>⟳ {t.nombreTarea} (en progreso...)</color>");
                    break;
                case EstadoTarea.Completada:
                    sb.AppendLine($"  <color=green>✓ <s>{t.nombreTarea}</s></color>");
                    break;
                case EstadoTarea.Fallada:
                    sb.AppendLine($"  <color=red>✗ <s>{t.nombreTarea}</s></color>");
                    break;
            }
        }

        if (esLobo)
            sb.AppendLine("\n<color=red><b>OBJETIVO REAL:\n¡Caza a los aldeanos antes\nde que terminen sus tareas!</b></color>");

        taskText.text = sb.ToString();
    }

    // ─── Mensaje de proximidad ────────────────────────────────────────────────

    /// <summary>
    /// Muestra un mensaje al jugador cerca de una estación.
    /// duracion = 0 → permanente hasta que se llame otra vez con string vacío.
    /// </summary>
    public void MostrarMensajeTarea(string mensaje, float duracion)
    {
        if (textoProximidad == null) return;

        if (corutinaProximidad != null) StopCoroutine(corutinaProximidad);

        textoProximidad.text = mensaje;
        textoProximidad.gameObject.SetActive(!string.IsNullOrEmpty(mensaje));

        if (duracion > 0f && !string.IsNullOrEmpty(mensaje))
            corutinaProximidad = StartCoroutine(OcultarTextoRutina(textoProximidad, duracion));
    }

    private IEnumerator OcultarTextoRutina(TextMeshProUGUI texto, float segundos)
    {
        yield return new WaitForSeconds(segundos);
        if (texto != null) texto.gameObject.SetActive(false);
    }

    // ─── Fase del día ─────────────────────────────────────────────────────────

    public void ActualizarFase(string fase)
    {
        if (textoFase != null)
            textoFase.text = "Fase: " + fase.ToUpper();
    }

    // ─── Inventario ───────────────────────────────────────────────────────────

    public void ActualizarInventario(string nombreObjeto)
    {
        if (textoInventario == null) return;

        if (nombreObjeto == "Ninguno")
        {
            textoInventario.text = "Mano Vacía";
            if (fondoInventario != null) fondoInventario.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        }
        else
        {
            textoInventario.text = nombreObjeto;
            if (fondoInventario != null) fondoInventario.color = new Color(1f, 0.8f, 0f, 0.6f);
        }
    }

    public void ActualizarMonedas(int cantidad)
    {
        if (textoMonedas != null)
        {
            textoMonedas.text = $"{cantidad} Oro";
        }
    }

    // ─── Barra de progreso ────────────────────────────────────────────────────

    public void MostrarBarraProgreso(bool mostrar)
    {
        if (contenedorBarra == null) return;
        contenedorBarra.SetActive(mostrar);
        if (mostrar && barraProgreso != null) barraProgreso.value = 0f;
    }

    public void ActualizarBarraProgreso(float actual, float maximo)
    {
        if (barraProgreso != null && maximo > 0f)
            barraProgreso.value = actual / maximo;
    }

    // ─── Victoria ─────────────────────────────────────────────────────────────

    public void MostrarVictoria(string mensajeVictoria)
    {
        if (textoRol == null) return;
        textoRol.text = mensajeVictoria;
        textoRol.transform.localScale = Vector3.one * 1.5f;
        textoRol.gameObject.SetActive(true);
    }

    public void ActualizarEspectador(string nombre, bool activo)
    {
        if (panelEspectador != null) panelEspectador.SetActive(activo);
        
        if (textoNombreEspectador != null)
        {
            if (string.IsNullOrEmpty(nombre))
            {
                textoNombreEspectador.text = "Modo: <color=#00eaff>Cámara Libre</color>";
            }
            else
            {
                textoNombreEspectador.text = $"Viendo a: <color=#FFD700>{nombre}</color>";
            }
        }
    }
}
