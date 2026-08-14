using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Core.QuestSystem;

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
    
    [Header("Minijuego de Timing")]
    [SerializeField] private GameObject contenedorTiming;
    [SerializeField] private UnityEngine.UI.Slider sliderTiming;
    [SerializeField] private RectTransform zonaVerdeRect;

    private bool isScrollOpen = false;

    // ─── Estado interno ───────────────────────────────────────────────────────

    private bool esLobo = false;
    private Coroutine corutinaProximidad;
    private Coroutine corutinaRol;

    // --- Referencias al Jugador Local ---
    private PlayerInventory localInventory;
    private PlayerQuestTracker localQuestTracker;

    public void VincularJugadorLocal(GameObject player)
    {
        // Limpiamos suscripciones viejas por si acaso
        DesvincularJugadorLocal();

        localInventory = player.GetComponent<PlayerInventory>();
        localQuestTracker = player.GetComponent<Core.QuestSystem.PlayerQuestTracker>();

        // [FIX] Leer el rol directamente desde PlayerState para no depender del orden
        // de llegada de MostrarRol() vs VincularJugadorLocal().
        // MostrarRol() puede dispararse ANTES de que el tracker esté listo,
        // causando que el dibujo inicial no muestre la misión.
        PlayerState ps = player.GetComponent<PlayerState>();
        if (ps != null)
        {
            esLobo = ps.isWolf.Value;
        }

        if (localQuestTracker != null)
        {
            localQuestTracker.OnQuestUpdated += OnQuestUpdated;
            localQuestTracker.OnQuestCompleted += OnQuestCompleted;

            // Si ya hay una misión asignada ANTES de que la UI existiera (ej. misión asignada
            // en el mismo frame del spawn), la cargamos manualmente para no perder el evento.
            string questIDActual = localQuestTracker.currentQuestID.Value.ToString();
            if (!string.IsNullOrEmpty(questIDActual) && Core.QuestSystem.QuestManager.Instance != null)
            {
                localQuestTracker.ForzarActualizacionUI();
            }
        }

        if (localInventory != null)
        {
            localInventory.materiales.OnValueChanged += OnMaterialesCambiados;
        }

        // [FIX] Forzar redibujo DESPUÉS de vincular el tracker y leer el rol correcto.
        // Esto garantiza que tanto la misión como el objetivo del lobo aparezcan juntos.
        RedibujarListaTareas();
    }

    private void DesvincularJugadorLocal()
    {
        if (localQuestTracker != null)
        {
            localQuestTracker.OnQuestUpdated -= OnQuestUpdated;
            localQuestTracker.OnQuestCompleted -= OnQuestCompleted;
        }
        if (localInventory != null)
        {
            localInventory.materiales.OnValueChanged -= OnMaterialesCambiados;
        }
    }

    private void OnQuestUpdated(Core.QuestSystem.QuestData quest)
    {
        RedibujarListaTareas();
    }

    private void OnQuestCompleted()
    {
        RedibujarListaTareas();
    }

    private void OnMaterialesCambiados(PlayerInventory.MaterialesMision anterior, PlayerInventory.MaterialesMision nuevo)
    {
        RedibujarListaTareas();
    }

    private void OnDestroy()
    {
        DesvincularJugadorLocal();
    }

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

        InicializarTimingBarDinamica();
    }

    public void ToggleScroll(bool forceState)
    {
        // Si no hay pergamino en el HUD (fue eliminado), ignorar silenciosamente
        if (pergaminoHudRoot == null)
        {
            isScrollOpen = false;
            return;
        }

        isScrollOpen = forceState;
        Debug.Log($"[PergaminoLog-UI] Recibiendo ToggleScroll de Cliente. Estado deseado: {forceState}");
        
        if (isScrollOpen) 
        {
            pergaminoHudRoot.SetActive(true);
            Debug.Log("[PergaminoLog-UI] Activando contenedor Raíz del pergamino en la UI.");
        }
        
        if (scroll3DAnimator != null)
        {
            scroll3DAnimator.SetBool("isOpen", isScrollOpen);
            Debug.Log($"[PergaminoLog-UI] Disparando flecha del Animator a isOpen={isScrollOpen}");
        }

        // Si cerramos, desactivamos tras un breve delay 
        if (!isScrollOpen) 
        {
            StartCoroutine(OcultarScrollRutina());
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

        // [NUEVO] Actualizar la variable interna esLobo ya que ahora no dependemos de las tareas antiguas
        esLobo = rol.Contains("LOBO");
        RedibujarListaTareas();

        if (corutinaRol != null) StopCoroutine(corutinaRol);
        corutinaRol = StartCoroutine(OcultarRolRutina());
    }

    private IEnumerator OcultarRolRutina()
    {
        yield return new WaitForSeconds(3f);
        if (textoRol != null) textoRol.gameObject.SetActive(false);
    }


    // ─── Dibujar lista ────────────────────────────────────────────────────────

    private void RedibujarListaTareas()
    {
        if (taskText == null) return;

        var sb = new System.Text.StringBuilder();

        // 1. MISIÓN ACTIVA (Sistema QuestSystem)
        if (localQuestTracker != null && !string.IsNullOrEmpty(localQuestTracker.currentQuestID.Value.ToString().TrimEnd('\0')))
        {
            if (esLobo)
                sb.AppendLine("\n<color=red><s>SIMULAR MISIÓN:</s></color>");
            else
                sb.AppendLine("\n<color=orange>MISIÓN ACTIVA:</color>");
                
            string questActual = localQuestTracker.currentQuestID.Value.ToString().TrimEnd('\0');
            Core.QuestSystem.QuestData activeQuest = Core.QuestSystem.QuestManager.Instance?.GetQuestByID(questActual);
            if (activeQuest != null)
            {
                sb.AppendLine($"<b>{activeQuest.nombreMision}</b>");
                int currentStep = localQuestTracker.currentStepIndex.Value;
                for (int i = 0; i < activeQuest.pasos.Count; i++)
                {
                    var paso = activeQuest.pasos[i];
                    if (i < currentStep)
                    {
                        sb.AppendLine($"  <color=green>✓ <s>{paso.descripcion}</s></color>");
                    }
                    else if (i == currentStep)
                    {
                        sb.AppendLine($"  <color=yellow>⟳ {paso.descripcion} ({paso.zonaRequerida})</color>");
                    }
                    else
                    {
                        sb.AppendLine($"  [ ] {paso.descripcion}");
                    }
                }
            }
        }

        // 3. MATERIALES RECOGIDOS
        if (!esLobo && localInventory != null)
        {
            bool tieneMateriales = false;
            PlayerInventory.MaterialesMision mats = localInventory.materiales.Value;
            
            // Recorrer los posibles materiales y pintar los que tengan cantidad > 0
            foreach (Core.Enums.MaterialType type in System.Enum.GetValues(typeof(Core.Enums.MaterialType)))
            {
                if (type == Core.Enums.MaterialType.Ninguno || type == Core.Enums.MaterialType.Cualquiera) continue;
                
                int count = mats.GetCount(type);
                if (count > 0)
                {
                    if (!tieneMateriales)
                    {
                        sb.AppendLine("\n<color=#00FFCC>MATERIALES RECOGIDOS:</color>");
                        tieneMateriales = true;
                    }
                    sb.AppendLine($"  • {type}: {count}");
                }
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

    // ─── Minijuego de Timing (Afilar) ─────────────────────────────────────────

    private void InicializarTimingBarDinamica()
    {
        if (contenedorTiming != null) return;

        // Buscar el Canvas principal
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Crear contenedor principal de la barra de timing
        GameObject goCont = new GameObject("ContenedorTimingBar", typeof(RectTransform));
        goCont.transform.SetParent(canvas.transform, false);
        contenedorTiming = goCont;

        RectTransform rtCont = goCont.GetComponent<RectTransform>();
        rtCont.anchorMin = new Vector2(0.5f, 0.28f); // Abajo, arriba del prompt
        rtCont.anchorMax = new Vector2(0.5f, 0.28f);
        rtCont.pivot = new Vector2(0.5f, 0.5f);
        rtCont.sizeDelta = new Vector2(300f, 25f);

        // Añadir fondo semi-transparente negro (Glassmorphism)
        var imgBg = goCont.AddComponent<UnityEngine.UI.Image>();
        imgBg.color = new Color(0f, 0f, 0f, 0.7f);

        // Crear la zona verde
        GameObject goGreen = new GameObject("ZonaVerde", typeof(RectTransform));
        goGreen.transform.SetParent(goCont.transform, false);
        zonaVerdeRect = goGreen.GetComponent<RectTransform>();
        var imgGreen = goGreen.AddComponent<UnityEngine.UI.Image>();
        imgGreen.color = new Color(0.2f, 0.8f, 0.3f, 0.85f); // Verde

        // Crear la barra slider
        GameObject goSlider = new GameObject("SliderTiming", typeof(RectTransform), typeof(UnityEngine.UI.Slider));
        goSlider.transform.SetParent(goCont.transform, false);
        sliderTiming = goSlider.GetComponent<UnityEngine.UI.Slider>();
        
        RectTransform rtSlider = goSlider.GetComponent<RectTransform>();
        rtSlider.anchorMin = Vector2.zero;
        rtSlider.anchorMax = Vector2.one;
        rtSlider.sizeDelta = Vector2.zero;

        // Crear el puntero (el palo vertical en medio)
        GameObject goPointer = new GameObject("Puntero", typeof(RectTransform));
        goPointer.transform.SetParent(goSlider.transform, false);
        var imgPointer = goPointer.AddComponent<UnityEngine.UI.Image>();
        imgPointer.color = Color.white;

        RectTransform rtPointer = goPointer.GetComponent<RectTransform>();
        rtPointer.anchorMin = new Vector2(0.5f, 0f);
        rtPointer.anchorMax = new Vector2(0.5f, 1f);
        rtPointer.sizeDelta = new Vector2(6f, 10f); // Palo vertical blanco

        sliderTiming.targetGraphic = imgBg;
        sliderTiming.handleRect = rtPointer;
        sliderTiming.minValue = 0f;
        sliderTiming.maxValue = 1f;
        sliderTiming.value = 0f;
        sliderTiming.interactable = false;

        // Ocultar por defecto
        contenedorTiming.SetActive(false);
    }

    public void MostrarTimingBar(bool mostrar, float targetMin, float targetMax)
    {
        InicializarTimingBarDinamica();

        if (contenedorTiming != null)
        {
            contenedorTiming.SetActive(mostrar);
        }

        if (mostrar && zonaVerdeRect != null)
        {
            // Ajustar los anclajes de la zona verde
            zonaVerdeRect.anchorMin = new Vector2(targetMin, 0f);
            zonaVerdeRect.anchorMax = new Vector2(targetMax, 1f);
            zonaVerdeRect.offsetMin = Vector2.zero;
            zonaVerdeRect.offsetMax = Vector2.zero;
        }
    }

    public void ActualizarTimingBar(float valor)
    {
        if (sliderTiming != null)
        {
            sliderTiming.value = valor;
        }
    }
}
