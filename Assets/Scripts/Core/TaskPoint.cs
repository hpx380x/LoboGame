using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Core.Enums;

// ─── Enums ────────────────────────────────────────────────────────────────────
public enum TipoInteraccion
{
    Instantanea,        // Pulsa E una vez (o automático al pisar)
    MantenerBoton,      // Mantener E durante X segundos (ej: afilar daga)
    PulsarRepetidamente,// Golpear E muchas veces (ej: martillar)
    MinijuegoUI         // Ejecuta un Canvas UI interactivo (Requiere prefab)
}

/// <summary>
/// Punto de tarea en el mundo. Cada estación tiene un ID único, nombre visible en HUD
/// y tipo de minijuego. El servidor asigna cuáles corresponden a cada jugador;
/// el cliente solo puede interactuar con las suyas.
///
/// NUEVO FLUJO:
///   1. El servidor asigna 2 tareas al jugador (pool de elección).
///   2. Al pisar una estación disponible → queda "EnProgreso" (bloqueado).
///   3. Si se aleja en mitad del progreso → FALLO → avanza a la siguiente disponible.
///   4. Si completa → avanza a la siguiente disponible.
/// </summary>
public class TaskPoint : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Identificación")]
    [Tooltip("ID único. Se genera automáticamente por ruta en jerarquía si está vacío.")]
    public string taskId = "";
    [Tooltip("Nombre visible en el HUD del jugador.")]
    public string nombreTarea = "Tarea sin nombre";

    [Header("Recompensa")]
    [Tooltip("Objeto que recibirá el jugador al completar la tarea directamente (Ruta de Riesgo).")]
    public TipoObjeto objetoRecompensa = TipoObjeto.Ninguno;
    [Tooltip("Cantidad de monedas que gana el jugador al completar la tarea (Ruta Segura).")]
    public int monedasRecompensa = 1;

    [Header("Minijuego")]
    public TipoInteraccion tipoDeMinijuego = TipoInteraccion.MantenerBoton;
    [Tooltip("Segundos a mantener pulsado (MantenerBoton) o toques necesarios (PulsarRepetidamente).")]
    public float objetivoMinijuego = 5f;
    [Tooltip("Para Instantanea: ¿requiere pulsar E o se activa solo al pisar?")]
    public bool requierePulsarBoton = true;
    [Tooltip("Prefab del minijuego 2D a spawnear (si el tipo es MinijuegoUI).")]
    public GameObject prefabMinijuegoUI;

    [Header("Collider")]
    [Tooltip("Margen extra (metros) que se suma al tamaño visual al auto-ajustar el BoxCollider.")]
    public float margenCollider = 0.2f;

    // Visual (Opcional): Objeto que se activa cuando esta tarea es del jugador local (luz, part\u00edcula...).
    public GameObject indicadorAsignada;

    // ─── Estado interno ───────────────────────────────────────────────────────

    // SERVER: ¿esta tarea fue asignada al jugador local de esta máquina?
    private bool esAsignadaAlJugadorLocal   = false;
    // ¿El jugador local está físicamente dentro del trigger?
    private bool jugadorLocalCerca          = false;
    // ¿Hay un minijuego corriendo ahora mismo?
    private bool minijuegoEnProgreso        = false;
    private MinigameBase minijuegoInstanciado = null;
    private float progresoActual            = 0f;
    
    // [Memoria de Minijuegos] Para que no se reseteen al cerrar el panel
    [HideInInspector] public bool minigameInitialized = false;
    [HideInInspector] public int[] runeState = new int[3];
    [HideInInspector] public int[] runeTarget = new int[3];

    // [Memoria Tapiz]
    [System.Serializable]
    public struct TapestryConnectionData
    {
        public int phase; // Índice de columna (0, 1, 2...)
        public int fromIdx;
        public int toIdx;
        public string colorHex;
    }
    [HideInInspector] public System.Collections.Generic.List<TapestryConnectionData> tapestryState = new System.Collections.Generic.List<TapestryConnectionData>();
    [HideInInspector] public int tapestryLevel = 0; // 0 a 2 (para completar 3 niveles)
    
    // [Memoria Puzzle 3x3]
    [HideInInspector] public int[] slidingPuzzleState = new int[9];

    // Referencia al PlayerState local
    private PlayerState jugadorLocalState;
    private Collider miCollider;

    // Renderers para tint visual
    private MeshRenderer[] renderersPropios;
    private Color[] coloresOriginales;

    // ─── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-generar ID por ruta en jerarquía (único dentro de la escena)
        if (string.IsNullOrEmpty(taskId))
            taskId = GenerarRutaJerarquica();

        miCollider = GetComponent<Collider>();

        // Guardar colores base de todos los MeshRenderers hijos
        renderersPropios = GetComponentsInChildren<MeshRenderer>();
        coloresOriginales = new Color[renderersPropios.Length];
        for (int i = 0; i < renderersPropios.Length; i++)
        {
            Material m = renderersPropios[i].sharedMaterial;
            if (m == null) continue;
            coloresOriginales[i] = m.HasProperty("_BaseColor")
                ? m.GetColor("_BaseColor")
                : m.color;
        }
    }

    private void Start()
    {
        AjustarBoxCollider();   // Fix automático del BoxCollider al mesh
        AplicarTintNoAsignada();// Por defecto: gris sutil (no asignada)
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Llamado por la ClientRpc del GameManager para marcar si esta estación
    /// pertenece al pool de tareas del jugador local.
    /// </summary>
    public void MarcarComoAsignada(bool asignada)
    {
        esAsignadaAlJugadorLocal = asignada;
        if (indicadorAsignada != null) indicadorAsignada.SetActive(asignada);

        if (asignada) AplicarTintDisponible();
        else          AplicarTintNoAsignada();
    }

    // ─── Triggers de colisión ─────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (Time.timeSinceLevelLoad < 2f) return;   // Ignora la carga inicial
        if (other.isTrigger) return;                 // Solo cuerpos sólidos

        NetworkObject netObj = other.GetComponent<NetworkObject>()
                            ?? other.GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        PlayerState ps = netObj.GetComponent<PlayerState>();
        if (ps == null || ps.isDead.Value) return;

        jugadorLocalState = ps;
        GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();

        // ── 1. ¿No está asignada a este jugador? ─────────────────────────────
        if (!esAsignadaAlJugadorLocal)
        {
            if (ui != null) ui.MostrarMensajeTarea("Esta no es tu tarea.", 2f);
            return;
        }

        // ── 2. ¿Hay otra tarea YA en progreso? ───────────────────────────────
        if (ui != null && ui.GetTareaActivaId() != null && ui.GetTareaActivaId() != taskId)
        {
            ui.MostrarMensajeTarea("Ya tienes una tarea activa. ¡Termínala primero!", 2f);
            jugadorLocalState = null;
            return;
        }

        // ── 3. ¿Ya la completé antes? ────────────────────────────────────────
        if (ui != null && ui.TareaEstaTerminada(taskId))
        {
            ui.MostrarMensajeTarea("Esta tarea ya está completada.", 2f);
            jugadorLocalState = null;
            return;
        }

        // ── 4. ¡Entramos! Bloquear esta tarea como activa ─────────────────────
        if (!jugadorLocalCerca)
        {
            jugadorLocalCerca = true;
            if (ui != null)
            {
                ui.SetTareaActiva(taskId); // Bloqueo: ninguna otra puede activarse
                ui.MostrarMensajeTarea(
                    requierePulsarBoton && tipoDeMinijuego == TipoInteraccion.Instantanea
                        ? $"[E] {nombreTarea}"
                        : $"Iniciando: {nombreTarea}",
                    0f);
            }

            // Para Instantanea sin botón → arranca ya
            if (!requierePulsarBoton && tipoDeMinijuego == TipoInteraccion.Instantanea)
                IntentarIniciarInstantanea();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.isTrigger) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>()
                            ?? other.GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        // Si nos fuimos mientras había un minijuego en curso → Solo cancelamos el progreso
        if (minijuegoEnProgreso)
        {
            Debug.Log($"<color=yellow>[Tarea '{nombreTarea}']</color> El jugador se alejó. Progreso reiniciado.");
            GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
            if (ui != null) ui.MostrarMensajeTarea("Te alejaste. Progreso reiniciado.", 2f);
        }

        ResetearEstadoLocal();
    }

    // ─── Update: bucle del minijuego ──────────────────────────────────────────

    private void Update()
    {
        if (!jugadorLocalCerca || jugadorLocalState == null) return;

        // Anti-bug: ClosestPoint para detectar salidas silenciosas
        if (miCollider != null)
        {
            Vector3 puntoCercano = miCollider.ClosestPoint(jugadorLocalState.transform.position);
            // Umbral reducido: 0.5 m (el anterior 1.5 m era demasiado permisivo)
            if (Vector3.Distance(jugadorLocalState.transform.position, puntoCercano) > 0.5f)
            {
                if (minijuegoEnProgreso)
                {
                    GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
                    if (ui != null) ui.MostrarMensajeTarea("Te alejaste. Progreso reiniciado.", 2f);
                }
                ResetearEstadoLocal();
                return;
            }
        }

        if (Keyboard.current == null) return;

        switch (tipoDeMinijuego)
        {
            case TipoInteraccion.Instantanea:
                if (requierePulsarBoton && !minijuegoEnProgreso
                    && Keyboard.current.eKey.wasPressedThisFrame)
                    IntentarIniciarInstantanea();
                break;

            case TipoInteraccion.MantenerBoton:
                TickMantener();
                break;

            case TipoInteraccion.PulsarRepetidamente:
                TickMartillar();
                break;
                
            case TipoInteraccion.MinijuegoUI:
                if (!minijuegoEnProgreso && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    IntentarIniciarMinijuegoUI();
                }
                break;
        }
    }

    // ─── Tipos de minijuego ───────────────────────────────────────────────────

    private void IntentarIniciarInstantanea()
    {
        if (jugadorLocalState == null || jugadorLocalState.isDead.Value) return;
        if (minijuegoEnProgreso) return;
        minijuegoEnProgreso = true;
        StartCoroutine(MinijuegoInstantaneo());
    }

    private System.Collections.IEnumerator MinijuegoInstantaneo()
    {
        // Barra de carga de 1 segundo para dar feedback
        GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
        if (ui != null) ui.MostrarBarraProgreso(true);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            if (ui != null) ui.ActualizarBarraProgreso(t, 1f);
            yield return null;
        }

        CompletarMinijuego();
    }

    private void IntentarIniciarMinijuegoUI()
    {
        if (jugadorLocalState == null || jugadorLocalState.isDead.Value) return;
        if (minijuegoEnProgreso) return;
        
        if (prefabMinijuegoUI == null)
        {
            Debug.LogError($"[TaskPoint] Falta asignar el PREFAB del minijuego UI en la tarea {nombreTarea}");
            return;
        }

        minijuegoEnProgreso = true;
        
        // Instanciamos el Canvas del minijuego
        GameObject minijuegoGO = Instantiate(prefabMinijuegoUI);
        minijuegoInstanciado = minijuegoGO.GetComponent<MinigameBase>();
        
        if (minijuegoInstanciado != null)
        {
            minijuegoInstanciado.SetupMinigame(this);
        }
        else
        {
            Debug.LogError("El prefab no tiene ningún script que herede de MinigameBase");
        }
    }

    public void MinijuegoResueltoPorUI()
    {
        CompletarMinijuego();
    }

    public void MinijuegoFalladoPorUI(string motivo)
    {
        FallarTarea(motivo);
    }

    private void TickMantener()
    {
        GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();

        if (Keyboard.current.eKey.isPressed)
        {
            if (!minijuegoEnProgreso)
            {
                minijuegoEnProgreso = true;
                if (ui != null) ui.MostrarBarraProgreso(true);
            }

            progresoActual += Time.deltaTime;
            if (ui != null) ui.ActualizarBarraProgreso(progresoActual, objetivoMinijuego);

            if (progresoActual >= objetivoMinijuego)
                CompletarMinijuego();
        }
        else if (minijuegoEnProgreso)
        {
            // Soltar E → reinicia progreso pero NO falla (sigue bloqueado)
            progresoActual = 0f;
            if (ui != null)
            {
                ui.MostrarBarraProgreso(false);
                ui.MostrarMensajeTarea("Soltaste. Vuelve a mantener [E].", 1.5f);
            }
            // NO ponemos minijuegoEnProgreso = false para que siga bloqueado
            // pero sí bajamos la bandera para que la barra se reinicie limpia
            minijuegoEnProgreso = false;
        }
    }

    private void TickMartillar()
    {
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();

        if (!minijuegoEnProgreso)
        {
            minijuegoEnProgreso = true;
            if (ui != null) ui.MostrarBarraProgreso(true);
        }

        progresoActual += 1f;
        if (ui != null) ui.ActualizarBarraProgreso(progresoActual, objetivoMinijuego);

        if (progresoActual >= objetivoMinijuego)
            CompletarMinijuego();
    }

    // ─── Resolución ───────────────────────────────────────────────────────────

    private void CompletarMinijuego()
    {
        if (jugadorLocalState == null || jugadorLocalState.isDead.Value) return;

        Debug.Log($"<color=green>[Tarea '{nombreTarea}']</color> ¡COMPLETADA!");
        progresoActual = 0f;
        minijuegoEnProgreso = false;

        GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
        if (ui != null)
        {
            ui.MostrarBarraProgreso(false);
            ui.CompletarTareaActiva(taskId);   // Avanza automáticamente a la siguiente
        }

        // Indicar al servidor que completamos una tarea (para el conteo diario)
        jugadorLocalState.NotificarTareaCompletadaServerRpc();

        // Dar las recompensas de Econom\u00eda H\u00edbrida
        PlayerInventory inv = jugadorLocalState.GetComponent<PlayerInventory>();
        if (inv != null)
        {
            if (monedasRecompensa > 0)
            {
                inv.GanarMonedasServerRpc(monedasRecompensa);
            }
            if (objetoRecompensa != TipoObjeto.Ninguno)
            {
                inv.RecogerObjetoServerRpc(objetoRecompensa);
            }
        }

        // Visual: turnar a verde/completada
        AplicarTintCompletada();
        if (indicadorAsignada != null) indicadorAsignada.SetActive(false);

        ResetearEstadoLocal();
    }

    private void FallarTarea(string motivo)
    {
        progresoActual = 0f;
        minijuegoEnProgreso = false;

        GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
        if (ui != null)
        {
            ui.MostrarBarraProgreso(false);
            ui.FallarTareaActiva(taskId, motivo);  // Avanza a la siguiente disponible
        }

        // Visual: tachar / atenuar
        AplicarTintFallada();
        if (indicadorAsignada != null) indicadorAsignada.SetActive(false);
    }

    private void ResetearEstadoLocal()
    {
        if (minijuegoInstanciado != null)
        {
            minijuegoInstanciado.ForzarCierreDesdeExterno();
            minijuegoInstanciado = null;
        }

        jugadorLocalCerca   = false;
        jugadorLocalState   = null;
        minijuegoEnProgreso = false;
        progresoActual      = 0f;

        GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
        if (ui != null)
        {
            ui.MostrarBarraProgreso(false);
            ui.MostrarMensajeTarea("", 0f); // Limpiar mensaje de proximidad
        }
    }

    // ─── BoxCollider Auto-Fix ─────────────────────────────────────────────────

    /// <summary>
    /// Calcula bounds combinados de todos los MeshRenderers hijo y ajusta el
    /// BoxCollider para que coincida exactamente con el volumen visual + margen.
    /// Arregla el bug de "puedo hacer tareas fuera del cuadrado".
    /// </summary>
    private void AjustarBoxCollider()
    {
        BoxCollider box = miCollider as BoxCollider;
        if (box == null) return;

        MeshRenderer[] meshes = GetComponentsInChildren<MeshRenderer>();
        if (meshes.Length == 0) return;

        // Calcular bounds combinados en WORLD space
        Bounds wb = meshes[0].bounds;
        for (int i = 1; i < meshes.Length; i++)
            wb.Encapsulate(meshes[i].bounds);

        // Convertir centro a LOCAL space
        Vector3 localCenter = transform.InverseTransformPoint(wb.center);

        // Escalar el tamaño: dividir por lossyScale para compensar el scale del GO
        Vector3 ls = transform.lossyScale;
        Vector3 localSize = new Vector3(
            wb.size.x / Mathf.Max(0.001f, Mathf.Abs(ls.x)),
            wb.size.y / Mathf.Max(0.001f, Mathf.Abs(ls.y)),
            wb.size.z / Mathf.Max(0.001f, Mathf.Abs(ls.z))
        );

        box.center = localCenter;
        box.size   = localSize + Vector3.one * margenCollider;

        Debug.Log($"[TaskPoint '{nombreTarea}'] BoxCollider auto-ajustado → size:{box.size}, center:{box.center}");
    }

    // ─── Visual ───────────────────────────────────────────────────────────────

    private void AplicarTintDisponible()  => AplicarTint(new Color(1f,   0.85f, 0.2f,  1f)); // Dorado
    private void AplicarTintNoAsignada() => AplicarTint(new Color(0.55f, 0.55f, 0.55f, 1f)); // Gris
    private void AplicarTintCompletada() => AplicarTint(new Color(0.25f, 0.75f, 0.35f, 1f)); // Verde
    private void AplicarTintFallada()    => AplicarTint(new Color(0.75f, 0.25f, 0.25f, 1f)); // Rojo

    private void AplicarTint(Color color)
    {
        if (renderersPropios == null) return;
        foreach (var r in renderersPropios)
        {
            if (r == null) continue;
            // Instanciar material en runtime para no modificar el asset original
            Material mat = r.material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
        }
    }

    // ─── Utils ────────────────────────────────────────────────────────────────

    private string GenerarRutaJerarquica()
    {
        string path = gameObject.name;
        Transform t = transform.parent;
        while (t != null) { path = t.name + "/" + path; t = t.parent; }
        return path;
    }
}