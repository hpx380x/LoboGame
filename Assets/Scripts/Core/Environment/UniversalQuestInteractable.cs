using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Core.Enums;
using Core.QuestSystem;
using UI.Minigames;

namespace Core.Environment
{

    /// <summary>
    /// Interactuable modular que sustituye a múltiples scripts específicos.
    /// Lee sus acciones y visuales dinámicamente desde QuestData.
    /// AHORA CON SOPORTE DE RED: Bloqueo Diario y Barra de Progreso.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class UniversalQuestInteractable : NetworkBehaviour
    {
        [Header("Modo de Interacción")]
        [Tooltip("Acción Pura: Solo da Oro. Recolección: Da items físicos en la mochila.")]
        public InteractionMode modoInteraccion = InteractionMode.AccionPura;

        [Tooltip("Cómo interactúa el jugador con este objeto.")]
        public MecanicaInteraccion mecanica = MecanicaInteraccion.MantenerPulsado;

        [Header("Configuración Aporrear")]
        public int pulsacionesRequeridas = 4;
        
        [Header("Configuración Transportar")]
        [Tooltip("Si este objeto es una caja para recoger, actívalo. Si es el Punto de Entrega, ignora esto y usa el modo 'PuntoEntrega' arriba.")]
        public bool esCajaRecogible = false;
        
        [Header("Múltiples Etapas (Para Mantener)")]
        [Tooltip("Número de veces que hay que interactuar (ej. regar 3 veces) para completarla.")]
        public int interaccionesRequeridas = 1;

        [Header("Visual Seguro")]
        public string nombreVisualManual = "";

        [Header("UI de Progreso")]
        [Tooltip("Barra de progreso de relleno (Image.fillAmount)")]
        public UnityEngine.UI.Image barraProgresoUI;

        [Header("Datos de la Misión")]
        [Tooltip("La configuración Data-Driven de la interacción (opcional si se usa material)")]
        public QuestData questData;

        [Header("Configuración Opcional")]
        public string customUIMessage = "Pulsa [E] para interactuar";

        [Header("Sistema de Materiales (Solo Recolección)")]
        public Core.Enums.MaterialType materialAsignado = Core.Enums.MaterialType.Ninguno;
        public int cantidadMaterialOtorgado = 0;
        
        [Header("Retrocompatibilidad (Ignorado si hay QuestData)")]
        public Core.Enums.ZoneID zonaUbicacion = Core.Enums.ZoneID.Desconocida;
        public int tipoAnimacionRecogida = 0;
        public string toolVisualNameLegacy = "";

        [Header("HUD Flotante 3D")]
        [Tooltip("Componente HUD en este mismo GameObject. Se crea automáticamente si es null.")]
        public InteractableHUD interactableHUD;

        // Accesos rápidos al HUD para compatibilidad interna
        // Accesos rápidos al HUD para compatibilidad interna y externa (QuestZonePoint)
        public GameObject hudObject
        {
            get => interactableHUD?.hudObject;
            set { if (interactableHUD != null) interactableHUD.hudObject = value; }
        }
        public TMPro.TextMeshPro hudText
        {
            get => interactableHUD?.hudText;
            set { if (interactableHUD != null) interactableHUD.hudText = value; }
        }

        public void EnsureHUDCreated()
        {
            if (interactableHUD == null)
                interactableHUD = gameObject.GetComponent<InteractableHUD>() ?? gameObject.AddComponent<InteractableHUD>();
            if (!string.IsNullOrEmpty(customUIMessage))
                interactableHUD.mensajePorDefecto = customUIMessage;
            interactableHUD.EnsureCreated();
        }

        [Header("Efectos Visuales (Ej: Velas / Brillo)")]
        [Tooltip("Partículas o luces que se encenderán al completar la interacción (y se apagarán al resetear el día).")]
        public GameObject[] efectosVisualesAlCompletar;
        [Tooltip("Partículas o luces de la estatua que se encenderán mientras el jugador está dentro de la zona/Trigger.")]
        public GameObject efectoEstatuaAlEstarEnZona;
        [Tooltip("Sistema de Partículas (ParticleSystem) o luz que emitirá destellos de brillo al recoger la esencia.")]
        public ParticleSystem efectoBrilloAlRecoger;
        [Tooltip("Si se activa, el modelo principal del objeto (renderers y colliders) se ocultará al completarse la interacción.")]
        public bool ocultarModeloAlCompletar = false;

        [Header("Configuración Entrega de Materiales (Sin caja física)")]
        [Tooltip("Si la entrega requiere entregar materiales directamente del inventario (mochila).")]
        public Core.Enums.MaterialType materialRequeridoParaEntrega = Core.Enums.MaterialType.Ninguno;
        public int cantidadRequeridaParaEntrega = 1;

        [Header("Configuración Shader de Limpieza / Restregar")]
        [Tooltip("Nombre del parámetro float en el shader para blending de textura (ej: _Cleanliness, _Dirtiness, _Blend)")]
        public string nombrePropiedadShader = "_Cleanliness";
        [Tooltip("Renderer del objeto que cambiará de apariencia visual al limpiar.")]
        public Renderer rendererObjetivoLimpieza;

        private bool jugadorEnZona = false;
        private GameObject jugadorLocal = null;
        private bool estaInteractuando = false;
        private float tiempoTranscurrido = 0f;
        private float duracionActual = 0f;
        private string toolActual = "";

        private float cooldownGolpeAcumulado = 0f;
        private const float COOLDOWN_GOLPE = 0.8f; // Cooldown entre golpes de hacha para coincidir con la animación

        private float currentTimingMin = 0.4f;
        private float currentTimingMax = 0.6f;

        // Caché de componentes del jugador para evitar GetComponent en Update
        private StarterAssets.ThirdPersonController cachedTPC = null;
        private StarterAssets.StarterAssetsInputs cachedInputs = null;
        private PlayerInput cachedPlayerInput = null;
        private Core.QuestSystem.PlayerToolVisuals cachedToolVisuals = null;
        private GameplayUI cachedGameplayUI = null;
        private Animator cachedAnimatorJugador = null;
        private Animator cachedNpcAnimator = null;
        private PlayerInventory cachedInventory = null;
        private Core.QuestSystem.PlayerQuestTracker cachedTracker = null;
        private Camera cachedCamMain = null;

        // ESTADO DE RED: Progreso Diario (Ej: 0 de 3)
        private NetworkVariable<int> progresoActual = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private Renderer[] renderersParaOcultar;
        private Collider[] collidersParaOcultar;

        private void Start()
        {
            if (mecanica == MecanicaInteraccion.Transportar || ocultarModeloAlCompletar)
            {
                var allRenders = GetComponentsInChildren<Renderer>(true);
                var activeRenders = new System.Collections.Generic.List<Renderer>();
                foreach (var r in allRenders)
                {
                    if (r == null || !r.enabled) continue;
                    
                    // Excluir renderers que pertenecen a los efectos de completado
                    bool esParteDeEfecto = false;
                    if (efectosVisualesAlCompletar != null)
                    {
                        foreach (var efecto in efectosVisualesAlCompletar)
                        {
                            if (efecto != null && (r.gameObject == efecto || r.transform.IsChildOf(efecto.transform)))
                            {
                                esParteDeEfecto = true;
                                break;
                            }
                        }
                    }

                    // Excluir el cajón de plantación / jardinera / maceta para que no desaparezca
                    string lowerName = r.gameObject.name.ToLower();
                    if (lowerName.Contains("planter") || lowerName.Contains("box") || lowerName.Contains("maceta") || lowerName.Contains("jardinera"))
                    {
                        esParteDeEfecto = true;
                    }

                    if (!esParteDeEfecto) activeRenders.Add(r);
                }
                renderersParaOcultar = activeRenders.ToArray();

                var allCols = GetComponentsInChildren<Collider>(true);
                var activeCols = new System.Collections.Generic.List<Collider>();
                foreach (var c in allCols)
                {
                    if (c == null || !c.enabled) continue;
                    
                    // Excluir colliders que pertenecen a los efectos de completado
                    bool esParteDeEfecto = false;
                    if (efectosVisualesAlCompletar != null)
                    {
                        foreach (var efecto in efectosVisualesAlCompletar)
                        {
                            if (efecto != null && (c.gameObject == efecto || c.transform.IsChildOf(efecto.transform)))
                            {
                                esParteDeEfecto = true;
                                break;
                            }
                        }
                    }

                    // Excluir el cajón de plantación / jardinera / maceta para que no pierda colisión
                    string lowerName = c.gameObject.name.ToLower();
                    if (lowerName.Contains("planter") || lowerName.Contains("box") || lowerName.Contains("maceta") || lowerName.Contains("jardinera"))
                    {
                        esParteDeEfecto = true;
                    }

                    // Evitar desactivar nuestro propio trigger de interacción
                    if (c.gameObject == gameObject && c.isTrigger) continue;

                    if (!esParteDeEfecto) activeCols.Add(c);
                }
                collidersParaOcultar = activeCols.ToArray();
            }
            // Asegurarse de que el componente HUD existe y está inicializado
            if (interactableHUD == null)
                interactableHUD = gameObject.GetComponent<InteractableHUD>() ?? gameObject.AddComponent<InteractableHUD>();
            if (!string.IsNullOrEmpty(customUIMessage))
                interactableHUD.mensajePorDefecto = customUIMessage;
            interactableHUD.EnsureCreated();

            if (barraProgresoUI != null)
            {
                barraProgresoUI.fillAmount = 0;
                barraProgresoUI.gameObject.SetActive(false);
            }

            // Configurar colisionador Trigger de 10 metros para visualización de HUD
            SphereCollider sphere = GetComponent<SphereCollider>();
            if (sphere == null)
            {
                // Si tiene un BoxCollider viejo u otro collider, lo aseguramos como Trigger
                Collider col = GetComponent<Collider>();
                if (col != null && !(col is SphereCollider))
                {
                    col.isTrigger = true;
                }
                else if (col == null)
                {
                    sphere = gameObject.AddComponent<SphereCollider>();
                }
            }

            if (sphere != null)
            {
                sphere.isTrigger = true;
                sphere.radius = 10f; // Detección a 10 metros
            }
            else if (transform.parent != null)
            {
                Collider parentCol = transform.parent.GetComponent<Collider>();
                if (parentCol != null)
                {
                    parentCol.isTrigger = true;
                    var proxy = transform.parent.gameObject.GetComponent<QuestTriggerProxy>();
                    if (proxy == null)
                    {
                        proxy = transform.parent.gameObject.AddComponent<QuestTriggerProxy>();
                    }
                    proxy.targetInteractable = this;
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            // Sincronizar estado inicial visualmente si ya estaba completada
            progresoActual.OnValueChanged += OnProgresoChanged;
            
            // Forzar actualización visual inicial en clientes si alguien se conecta tarde
            OnProgresoChanged(0, progresoActual.Value);
        }

        public override void OnNetworkDespawn()
        {
            progresoActual.OnValueChanged -= OnProgresoChanged;
        }

        private void OnProgresoChanged(int previous, int current)
        {
            // --- ACTUALIZAR VISIBILIDAD EN RED (Global, para todos los jugadores) ---
            if (mecanica == MecanicaInteraccion.Transportar || ocultarModeloAlCompletar)
            {
                bool ocultar = current >= interaccionesRequeridas;
                if (renderersParaOcultar != null)
                {
                    foreach (var r in renderersParaOcultar) if (r != null) r.enabled = !ocultar;
                }
                if (collidersParaOcultar != null)
                {
                    foreach (var c in collidersParaOcultar) if (c != null) c.enabled = !ocultar;
                }
                if (hudObject != null && ocultar) hudObject.SetActive(false);
            }

            // --- ACTUALIZAR EFECTOS VISUALES (Global, para todos los jugadores) ---
            if (efectosVisualesAlCompletar != null && efectosVisualesAlCompletar.Length > 0)
            {
                bool activarEfectos = current >= interaccionesRequeridas;
                foreach (var efecto in efectosVisualesAlCompletar)
                {
                    if (efecto != null) efecto.SetActive(activarEfectos);
                }
            }

            // --- ACTUALIZAR UI (Solo si el jugador local está cerca y tiene esta misión) ---
            if (!jugadorEnZona || estaInteractuando) return;

            if (!EsPasoValidoDeMisionActual())
            {
                if (hudObject != null) hudObject.SetActive(false);
                if (barraProgresoUI != null) barraProgresoUI.gameObject.SetActive(false);
                return;
            }
            else
            {
                if (hudText != null) hudText.text = (interaccionesRequeridas > 1 && current < interaccionesRequeridas) ? $"{customUIMessage} ({current}/{interaccionesRequeridas})" : customUIMessage;
                if (barraProgresoUI != null)
                {
                    if (current > 0 && current < interaccionesRequeridas)
                    {
                        barraProgresoUI.fillAmount = (float)current / interaccionesRequeridas;
                        barraProgresoUI.color = Color.blue;
                        barraProgresoUI.gameObject.SetActive(true);
                    }
                    else
                    {
                        barraProgresoUI.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                var no = other.GetComponent<NetworkObject>();
                if (no != null && no.IsOwner)
                {
                    var tracker = other.GetComponent<PlayerQuestTracker>();

                    // 1. Validar si este objeto sirve para la misión activa (usando QuestValidator centralizado)
                    bool sirveParaMisionActiva =
                        mecanica == MecanicaInteraccion.PuntoEntrega
                            ? QuestValidator.EntregaEsValidaParaQuestActiva(tracker, materialRequeridoParaEntrega, materialAsignado)
                            : QuestValidator.JugadorTieneQuestActivaParaObjeto(
                                tracker, materialAsignado, zonaUbicacion, materialRequeridoParaEntrega,
                                questData?.questID);

                    // 2. Si no sirve para la misión activa, aplicar reglas generales del objeto
                    if (!sirveParaMisionActiva)
                    {
                        if (questData == null &&
                            materialAsignado == MaterialType.Ninguno &&
                            mecanica != MecanicaInteraccion.Transportar &&
                            mecanica != MecanicaInteraccion.PuntoEntrega)
                        {
                            return; // Objeto genérico inactivo
                        }

                        string activeQuestId = tracker != null ? tracker.currentQuestID.Value.ToString().TrimEnd('\0') : "";
                        if (questData != null && activeQuestId != questData.questID)
                        {
                            return; // Objeto de otra misión
                        }
                    }

                    jugadorEnZona = true;
                    jugadorLocal = other.gameObject;

                    // Cachear componentes para evitar GetComponent repetidos en Update
                    cachedTPC              = jugadorLocal.GetComponent<StarterAssets.ThirdPersonController>();
                    cachedInputs           = jugadorLocal.GetComponent<StarterAssets.StarterAssetsInputs>();
                    cachedPlayerInput      = jugadorLocal.GetComponent<PlayerInput>();
                    cachedToolVisuals      = jugadorLocal.GetComponentInChildren<Core.QuestSystem.PlayerToolVisuals>(true);
                    cachedAnimatorJugador  = jugadorLocal.GetComponentInChildren<Animator>();
                    cachedInventory        = jugadorLocal.GetComponent<PlayerInventory>();
                    cachedTracker          = jugadorLocal.GetComponent<Core.QuestSystem.PlayerQuestTracker>();
                    cachedGameplayUI       = Object.FindAnyObjectByType<GameplayUI>();
                    if (cachedCamMain == null) cachedCamMain = Camera.main;
                    
                    if (!estaInteractuando)
                    {
                        bool pasoValido = EsPasoValidoDeMisionActual();
                        if (pasoValido)
                        {
                            Debug.Log($"[Quest Trigger] Jugador local detectado en zona {zonaUbicacion}. Mostrando HUD flotante.");
                            OnProgresoChanged(0, progresoActual.Value); // Refresca los textos y barras
                            if (hudObject != null) hudObject.SetActive(true);

                            // Encender brillo de la estatua al entrar en la zona
                            if (efectoEstatuaAlEstarEnZona != null) efectoEstatuaAlEstarEnZona.SetActive(true);
                        }
                        else
                        {
                            if (hudObject != null) hudObject.SetActive(false);
                            if (efectoEstatuaAlEstarEnZona != null) efectoEstatuaAlEstarEnZona.SetActive(false);
                        }
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && jugadorLocal == other.gameObject)
            {
                jugadorEnZona = false;

                // Apagar brillo de la estatua al salir de la zona
                if (efectoEstatuaAlEstarEnZona != null) efectoEstatuaAlEstarEnZona.SetActive(false);

                // ESCUDO DE INTERACCIÓN: Si la física de ingravidez saca al jugador del Trigger,
                // pero ya estábamos en medio de la interacción, bloqueamos la destrucción visual.
                if (!estaInteractuando)
                {
                    jugadorLocal = null;
                    cachedTPC = null;
                    cachedInputs = null;
                    cachedPlayerInput = null;
                    cachedToolVisuals = null;
                    cachedGameplayUI = null;
                    if (hudObject != null) hudObject.SetActive(false);
                    if (barraProgresoUI != null && progresoActual.Value < interaccionesRequeridas) barraProgresoUI.gameObject.SetActive(false);
                }
            }
        }

        public void OnTriggerEnterProxy(Collider other) => OnTriggerEnter(other);
        public void OnTriggerExitProxy(Collider other) => OnTriggerExit(other);

        private float pulsacionesActuales = 0;
        private float tiempoInactividad = 0f;
        private float timingCooldownActual = 0f;
        private const float TIMING_ANIM_DURACION = 0.55f; // Duración del hachazo antes de poder pulsar de nuevo

        private void Update()
        {
            // Billboard delegado a InteractableHUD.Update()

            if (!jugadorEnZona || jugadorLocal == null) return;

            if (!estaInteractuando)
            {
                // Escudo de Movimiento y Animación: No iniciar una NUEVA interacción si el jugador está bloqueado (ej: animación de fallo 'headno' 14)
                if (cachedTPC == null) cachedTPC = jugadorLocal.GetComponent<StarterAssets.ThirdPersonController>();
                if (cachedTPC != null && !cachedTPC.CanMove) return;

                if (cachedAnimatorJugador == null) cachedAnimatorJugador = jugadorLocal.GetComponentInChildren<Animator>();
                if (cachedAnimatorJugador != null)
                {
                    if (cachedAnimatorJugador.GetInteger("InteractionType") == 14) return; // Bloquear si está haciendo headno
                }

                // Escudo de Input: No interactuar si el ActionMap está desactivado
                if (cachedPlayerInput != null && cachedPlayerInput.currentActionMap != null && !cachedPlayerInput.currentActionMap.enabled) return;

                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    // Escudo de Misión Activa: Si el objeto pertenece a una misión pero el jugador no la tiene activa en este paso, denegar la interacción.
                    if (!EsPasoValidoDeMisionActual())
                    {
                        Debug.LogWarning($"[Quest] {gameObject.name}: El jugador local no tiene esta misión o paso activo.");
                        var inv = cachedInventory ?? jugadorLocal.GetComponent<PlayerInventory>();
                        if (inv != null)
                        {
                            inv.CancelUniversalInteractionServerRpc(toolActual, true);
                        }

                        // Reproducción local inmediata de la animación de fallo (headno = 14)
                        if (cachedAnimatorJugador != null)
                        {
                            cachedAnimatorJugador.SetBool("IsInteracting", true);
                            cachedAnimatorJugador.SetInteger("InteractionType", 14);
                        }
                        if (cachedTPC != null)
                        {
                            cachedTPC.CanMove = false;
                            StartCoroutine(UnlockMovementAfterDelay(3.0f, cachedTPC));
                        }

                        if (hudText != null) hudText.text = "¡No tienes esta misión!";
                        return;
                    }

                    // Lógica especial Punto Entrega
                    if (mecanica == MecanicaInteraccion.PuntoEntrega)
                    {
                        var inv = jugadorLocal.GetComponent<PlayerInventory>();
                        if (materialRequeridoParaEntrega != Core.Enums.MaterialType.Ninguno)
                        {
                            if (inv != null && inv.materiales.Value.GetCount(materialRequeridoParaEntrega) >= cantidadRequeridaParaEntrega)
                            {
                                IniciarInteraccion();
                            }
                            else
                            {
                                if (hudText != null) hudText.text = $"¡Necesitas {materialRequeridoParaEntrega} x{cantidadRequeridaParaEntrega}!";
                            }
                        }
                        else
                        {
                            var tv = cachedToolVisuals;
                            string herramientaRequerida = !string.IsNullOrEmpty(nombreVisualManual) ? nombreVisualManual : "caja";
                            if (tv != null && tv.IsToolActive(herramientaRequerida))
                            {
                                IniciarInteraccion();
                            }
                            else
                            {
                                if (hudText != null) hudText.text = $"¡Necesitas {herramientaRequerida}!";
                            }
                        }
                    }
                    else
                    {
                        IniciarInteraccion();
                        if (mecanica == MecanicaInteraccion.AporrearBoton)
                        {
                            pulsacionesActuales = 1;
                            tiempoInactividad = 0f;
                        }
                    }
                }
            }
            else
            {
                // Detectar cancelación por movimiento utilizando componentes cacheados (excepto en Transportar)
                var inputs = cachedInputs != null ? cachedInputs : jugadorLocal.GetComponent<StarterAssets.StarterAssetsInputs>();
                if (inputs != null)
                {
                    Vector2 moveInput = inputs.move;
                    if (moveInput.sqrMagnitude > 0.05f && mecanica != MecanicaInteraccion.Transportar && mecanica != MecanicaInteraccion.RitmoAfilar && mecanica != MecanicaInteraccion.Restregar)
                    {
                        AbortarInteraccion(false); // Cancelar por movimiento -> salida inmediata sin animación de fallo
                        return;
                    }
                }

                if (mecanica == MecanicaInteraccion.MantenerPulsado || mecanica == MecanicaInteraccion.Transportar || mecanica == MecanicaInteraccion.PuntoEntrega)
                {
                    // Si es mantener pulsado y se suelta la tecla E, abortamos la interacción inmediatamente
                    if (mecanica == MecanicaInteraccion.MantenerPulsado && Keyboard.current != null && !Keyboard.current.eKey.isPressed)
                    {
                        AbortarInteraccion(true); // Salida voluntaria -> reproducir animación de fallo
                        return;
                    }

                    // Calcular Progreso por Tiempo
                    tiempoTranscurrido += Time.deltaTime;
                    if (barraProgresoUI != null)
                    {
                        float baseFill = (float)progresoActual.Value / interaccionesRequeridas;
                        float stepFill = (tiempoTranscurrido / duracionActual) * (1f / interaccionesRequeridas);
                        barraProgresoUI.fillAmount = Mathf.Clamp01(baseFill + stepFill);
                    }
                    if (cachedGameplayUI != null)
                    {
                        cachedGameplayUI.ActualizarBarraProgreso(tiempoTranscurrido, duracionActual);
                    }

                    if (tiempoTranscurrido >= duracionActual)
                    {
                        CompletarInteraccionLocal();
                    }
                }
                else if (mecanica == MecanicaInteraccion.AporrearBoton)
                {
                    tiempoInactividad += Time.deltaTime;

                    if (cooldownGolpeAcumulado > 0f)
                    {
                        cooldownGolpeAcumulado -= Time.deltaTime;
                    }
                    
                    // Si deja de pulsar 2.5s (dado el cooldown), se reinicia la tala
                    if (tiempoInactividad > 2.5f)
                    {
                        AbortarInteraccion(true); // Inactividad -> reproducir animación de fallo
                        return;
                    }

                    if (cooldownGolpeAcumulado <= 0f && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        cooldownGolpeAcumulado = COOLDOWN_GOLPE;
                        tiempoInactividad = 0f;

                        // Registrar el golpe con un retraso (0.5s) para que coincida visualmente con el impacto físico de la animación
                        StartCoroutine(RegistrarGolpeConRetraso(0.5f));
                    }
                }
                else if (mecanica == MecanicaInteraccion.Timing)
                {
                    // Reducir cooldown de animación
                    if (timingCooldownActual > 0f)
                    {
                        timingCooldownActual -= Time.deltaTime;
                    }

                    tiempoInactividad += Time.deltaTime;

                    if (tiempoInactividad > 4.0f)
                    {
                        AbortarInteraccion(true);
                        return;
                    }

                    // Puntero oscila horizontalmente (velocidad reducida un 30%)
                    float pointerVal = Mathf.PingPong(Time.time * 1.26f, 1.0f);
                    if (cachedGameplayUI != null)
                    {
                        cachedGameplayUI.ActualizarTimingBar(pointerVal);
                    }

                    // Bloquear input mientras el hachazo está en marcha
                    if (timingCooldownActual > 0f) return;

                    if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        tiempoInactividad = 0f;

                        // ── Reproducir la animación de golpe SIEMPRE al pulsar ──
                        if (cachedAnimatorJugador != null)
                        {
                            int animType = ObtenerAnimTypeActual();
                            cachedAnimatorJugador.speed = 1.0f;
                            cachedAnimatorJugador.SetBool("IsInteracting", true);
                            cachedAnimatorJugador.SetInteger("InteractionType", animType);
                        }

                        // Activar cooldown: bloquea nueva pulsación hasta terminar el swing
                        timingCooldownActual = TIMING_ANIM_DURACION;

                        // ── Comprobar zona verde en el INSTANTE de la pulsación ──
                        bool enZona = pointerVal >= currentTimingMin && pointerVal <= currentTimingMax;

                        if (enZona)
                        {
                            // ✅ GOLPE VÁLIDO
                            pulsacionesActuales++;

                            if (barraProgresoUI != null)
                            {
                                float baseFill = (float)progresoActual.Value / interaccionesRequeridas;
                                float stepFill = (pulsacionesActuales / pulsacionesRequeridas) * (1f / interaccionesRequeridas);
                                barraProgresoUI.fillAmount = Mathf.Clamp01(baseFill + stepFill);
                            }
                            if (cachedGameplayUI != null)
                            {
                                float baseFill = (float)progresoActual.Value;
                                float stepFill = pulsacionesActuales / pulsacionesRequeridas;
                                cachedGameplayUI.ActualizarBarraProgreso(baseFill + stepFill, interaccionesRequeridas);
                            }

                            Debug.Log($"[Timing Game] ¡Golpe perfecto! ({pulsacionesActuales}/{pulsacionesRequeridas}) — Puntero: {pointerVal:F3} Zona: [{currentTimingMin:F3}-{currentTimingMax:F3}]");

                            if (pulsacionesActuales >= pulsacionesRequeridas)
                            {
                                if (cachedGameplayUI != null) cachedGameplayUI.MostrarTimingBar(false, 0, 0);
                                CompletarInteraccionLocal();
                            }
                            else
                            {
                                // Siguiente zona verde en posición aleatoria
                                float center = Random.Range(0.2f, 0.8f);
                                currentTimingMin = Mathf.Max(0.1f, center - 0.08f);
                                currentTimingMax = Mathf.Min(0.9f, center + 0.08f);
                                if (cachedGameplayUI != null)
                                {
                                    cachedGameplayUI.MostrarTimingBar(true, currentTimingMin, currentTimingMax);
                                }
                            }
                        }
                        else
                        {
                            // ❌ GOLPE FALLIDO — animación de headno después del swing
                            Debug.Log($"[Timing Game] ¡Fallo! Puntero: {pointerVal:F3} — Zona verde: [{currentTimingMin:F3}-{currentTimingMax:F3}]");
                            // Esperamos a que el swing termine antes de mostrar el headno
                            StartCoroutine(FalloConRetraso(TIMING_ANIM_DURACION));
                        }
                    }
                }
            }
        }

        private void IniciarInteraccion()
        {
            estaInteractuando = true;
            tiempoTranscurrido = 0f;

            duracionActual = ObtenerDurationActual();

            int animType = ObtenerAnimTypeActual();

            toolActual = !string.IsNullOrEmpty(nombreVisualManual) ? nombreVisualManual 
                       : (questData != null ? questData.toolVisualName : "");

            if (string.IsNullOrEmpty(toolActual) && jugadorLocal != null)
            {
                var tracker = cachedTracker ?? jugadorLocal.GetComponent<Core.QuestSystem.PlayerQuestTracker>();
                if (tracker != null)
                {
                    string activeQuestId = tracker.currentQuestID.Value.ToString().TrimEnd('\0');
                    if (!string.IsNullOrEmpty(activeQuestId))
                    {
                        var activeQuest = Core.QuestSystem.QuestManager.Instance?.GetQuestByID(activeQuestId);
                        if (activeQuest != null && !string.IsNullOrEmpty(activeQuest.toolVisualName))
                            toolActual = activeQuest.toolVisualName;
                    }
                }
            }

            Debug.Log($"[Quest Trigger] Interacción iniciada. Mecanica={mecanica}, Tool={toolActual}, Duración={duracionActual}s.");

            if (hudObject != null) hudObject.SetActive(false);

            if (barraProgresoUI != null && mecanica != MecanicaInteraccion.Transportar && mecanica != MecanicaInteraccion.PuntoEntrega)
            {
                barraProgresoUI.fillAmount = (float)progresoActual.Value / interaccionesRequeridas;
                barraProgresoUI.color = Color.blue;
                barraProgresoUI.gameObject.SetActive(true);
            }

            if (cachedGameplayUI != null && mecanica != MecanicaInteraccion.Transportar && mecanica != MecanicaInteraccion.PuntoEntrega && mecanica != MecanicaInteraccion.RitmoAfilar && mecanica != MecanicaInteraccion.Restregar)
            {
                cachedGameplayUI.MostrarBarraProgreso(true);
                cachedGameplayUI.ActualizarBarraProgreso((float)progresoActual.Value, interaccionesRequeridas);
            }

            // ✅ FIX: Usar cachedTPC y cachedPlayerInput en vez de TryGetComponent + GetComponent
            if (cachedTPC != null && mecanica != MecanicaInteraccion.Transportar)
            {
                cachedTPC.CanMove = false;
                // ✅ FIX: NO desactivar el ActionMap. Solo bloqueamos CanMove.
                // El escudo de línea 281 ya impide que se inicien nuevas interacciones.
                // El ActionMap debe seguir activo para que el AporrearBoton funcione.
            }

            if (cachedAnimatorJugador != null)
            {
                cachedAnimatorJugador.SetBool("IsInteracting", true);
                cachedAnimatorJugador.SetInteger("InteractionType", animType);
            }

            if (cachedToolVisuals != null && !string.IsNullOrEmpty(toolActual) && mecanica != MecanicaInteraccion.Transportar)
                cachedToolVisuals.SetToolByName(toolActual, true);

            if (mecanica == MecanicaInteraccion.AporrearBoton)
            {
                pulsacionesActuales = 0;
                cooldownGolpeAcumulado = COOLDOWN_GOLPE;
                tiempoInactividad = 0f;
                // El primer golpe se registra de inmediato (sin retraso)
                StartCoroutine(RegistrarGolpeConRetraso(0f));
            }
            else if (mecanica == MecanicaInteraccion.Timing)
            {
                pulsacionesActuales = 0;
                tiempoInactividad = 0f;
                timingCooldownActual = 0f;
                currentTimingMin = 0.4f;
                currentTimingMax = 0.6f;
                if (cachedGameplayUI != null)
                {
                    cachedGameplayUI.MostrarTimingBar(true, currentTimingMin, currentTimingMax);
                }
            }
            else if (mecanica == MecanicaInteraccion.RitmoAfilar)
            {
                var minijuego = Object.FindAnyObjectByType<MinijuegoAfilarUI>(FindObjectsInactive.Include);
                if (minijuego != null)
                {
                    // Deshabilitar mapa de movimiento WASD para el minijuego de ritmo
                    if (cachedPlayerInput != null && cachedPlayerInput.actions != null)
                    {
                        var moveAction = cachedPlayerInput.actions.FindAction("Move");
                        if (moveAction != null) moveAction.Disable();
                    }

                    minijuego.OnMinijuegoCompletado = () =>
                    {
                        if (cachedPlayerInput != null && cachedPlayerInput.actions != null)
                        {
                            var moveAction = cachedPlayerInput.actions.FindAction("Move");
                            if (moveAction != null) moveAction.Enable();
                        }
                        CompletarInteraccionLocal();
                    };

                    minijuego.OnMinijuegoCancelado = () =>
                    {
                        if (cachedPlayerInput != null && cachedPlayerInput.actions != null)
                        {
                            var moveAction = cachedPlayerInput.actions.FindAction("Move");
                            if (moveAction != null) moveAction.Enable();
                        }
                        AbortarInteraccion(false);
                    };

                    minijuego.OnMinijuegoFallo = () =>
                    {
                        if (cachedAnimatorJugador != null)
                        {
                            cachedAnimatorJugador.SetBool("IsInteracting", true);
                            cachedAnimatorJugador.SetInteger("InteractionType", 14); // headno
                            StartCoroutine(RestaurarAnimacionTrasFallo(2.5f));
                        }
                    };

                    minijuego.IniciarMinijuego();
                }
            }
            else if (mecanica == MecanicaInteraccion.Restregar)
            {
                var minijuego = Object.FindAnyObjectByType<MinijuegoRestregarUI>(FindObjectsInactive.Include);
                if (minijuego != null)
                {
                    // Deshabilitar mapa de movimiento WASD para el minijuego de restregar
                    if (cachedPlayerInput != null && cachedPlayerInput.actions != null)
                    {
                        var moveAction = cachedPlayerInput.actions.FindAction("Move");
                        if (moveAction != null) moveAction.Disable();
                    }

                    minijuego.OnProgresoSuciedadChanged = (progresoLimpio) =>
                    {
                        ActualizarProgresoShaderLimpieza(progresoLimpio);
                    };

                    minijuego.OnTeclaPulsada = (tecla) =>
                    {
                        MoverJugadorEnZonaLimpieza(tecla);
                    };

                    minijuego.OnMinijuegoCompletado = () =>
                    {
                        if (cachedPlayerInput != null && cachedPlayerInput.actions != null)
                        {
                            var moveAction = cachedPlayerInput.actions.FindAction("Move");
                            if (moveAction != null) moveAction.Enable();
                        }
                        ActualizarProgresoShaderLimpieza(1f);
                        CompletarInteraccionLocal();
                    };

                    minijuego.OnMinijuegoCancelado = () =>
                    {
                        if (cachedPlayerInput != null && cachedPlayerInput.actions != null)
                        {
                            var moveAction = cachedPlayerInput.actions.FindAction("Move");
                            if (moveAction != null) moveAction.Enable();
                        }
                        ActualizarProgresoShaderLimpieza(0f);
                        AbortarInteraccion(false);
                    };

                    minijuego.OnMinijuegoFallo = () =>
                    {
                        if (cachedAnimatorJugador != null)
                        {
                            cachedAnimatorJugador.SetBool("IsInteracting", true);
                            cachedAnimatorJugador.SetInteger("InteractionType", 14); // headno
                            StartCoroutine(RestaurarAnimacionTrasFallo(2.0f));
                        }
                    };

                    minijuego.IniciarMinijuego();
                }
            }

            // Sincronizar efectos visuales y animación en red inmediatamente para todas las mecánicas
            float duracionPeticion = (mecanica == MecanicaInteraccion.MantenerPulsado) ? duracionActual : -1f;
            EnviarPeticionAServidor(duracionPeticion);

            // Animar al NPC si tiene Animator (cachear en primer uso)
            if (cachedNpcAnimator == null) cachedNpcAnimator = GetComponentInChildren<Animator>();
            if (cachedNpcAnimator != null)
            {
                cachedNpcAnimator.SetBool("IsInteracting", true);
                cachedNpcAnimator.SetInteger("InteractionType", animType);
            }
        }

        private void EnviarPeticionAServidor(float duracion)
        {
            if (jugadorLocal == null) return;
            var inventory = cachedInventory ?? jugadorLocal.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                int oro = questData != null ? questData.oroRecompensa : 0;
                int matAmount = modoInteraccion == InteractionMode.Recoleccion ? cantidadMaterialOtorgado : 0;
                int animType = ObtenerAnimTypeActual();
                inventory.ProcessUniversalInteractionServerRpc(transform.position, toolActual, duracion, oro, NetworkObjectId, materialAsignado, matAmount, animType);
            }
        }

        private void CompletarInteraccionLocal()
        {
            estaInteractuando = false;
            tiempoTranscurrido = 0f;
            if (hudObject != null) hudObject.SetActive(false);
            if (cachedGameplayUI != null)
            {
                cachedGameplayUI.MostrarBarraProgreso(false);
                cachedGameplayUI.MostrarTimingBar(false, 0, 0);
            }
            
            var tv = cachedToolVisuals;
            
            // Si se recoge la Esencia Antigua o es un objeto de transporte, activamos el temporizador de la tumba
            if (materialAsignado == Core.Enums.MaterialType.EsenciaAntigua || mecanica == MecanicaInteraccion.Transportar)
            {
                if (efectoBrilloAlRecoger != null)
                {
                    efectoBrilloAlRecoger.Play();
                }

                var tumba = Object.FindAnyObjectByType<TumbaConTiempo>();
                if (tumba != null) tumba.IniciarTemporizador();
            }

            if (mecanica == MecanicaInteraccion.Transportar)
            {
                // El jugador recogió la caja/esencia. Aparece en su mano.
                if (tv != null && !string.IsNullOrEmpty(toolActual)) tv.SetToolByName(toolActual, true);
                
                // Apagar animación de interacción localmente
                if (cachedAnimatorJugador != null)
                {
                    cachedAnimatorJugador.SetBool("IsInteracting", false);
                    cachedAnimatorJugador.SetInteger("InteractionType", 0);
                    if (cachedAnimatorJugador.HasState(0, Animator.StringToHash("Grounded")))
                    {
                        cachedAnimatorJugador.Play("Grounded");
                    }
                }

                // Enviar la petición al servidor para que registre la recogida y avance el paso de la misión
                EnviarPeticionAServidor(0f);
                return; // Transportar no da oro ni completa etapa aquí, se hace en el Punto de Entrega
            }

            // Apagar herramienta localmente de forma instantánea (Client Prediction)
            if (tv != null && !string.IsNullOrEmpty(toolActual)) tv.SetToolByName(toolActual, false);
            
            // Quitar animación de interacción
            if (cachedAnimatorJugador != null)
            {
                cachedAnimatorJugador.SetBool("IsInteracting", false);
                cachedAnimatorJugador.SetInteger("InteractionType", 0);
                if (cachedAnimatorJugador.HasState(0, Animator.StringToHash("Grounded")))
                {
                    cachedAnimatorJugador.Play("Grounded");
                }
            }
            
            // Si era Aporrear, Entrega, Timing, RitmoAfilar o Restregar, enviamos ahora el RPC con duración 0 para ejecución inmediata
            if (mecanica == MecanicaInteraccion.AporrearBoton || mecanica == MecanicaInteraccion.PuntoEntrega || mecanica == MecanicaInteraccion.Timing || mecanica == MecanicaInteraccion.RitmoAfilar || mecanica == MecanicaInteraccion.Restregar)
            {
                EnviarPeticionAServidor(0f);
            }

            // Limpiar animación del NPC si tiene Animator
            if (cachedNpcAnimator == null) cachedNpcAnimator = GetComponentInChildren<Animator>();
            if (cachedNpcAnimator != null)
            {
                cachedNpcAnimator.SetBool("IsInteracting", false);
                cachedNpcAnimator.SetInteger("InteractionType", 0);
                if (cachedNpcAnimator.HasState(0, Animator.StringToHash("Grounded")))
                {
                    cachedNpcAnimator.Play("Grounded");
                }
            }
        }

        private System.Collections.IEnumerator RegistrarGolpeConRetraso(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (estaInteractuando && jugadorLocal != null)
            {
                pulsacionesActuales++;
                
                // Actualizar barra de progreso
                if (barraProgresoUI != null)
                {
                    float baseFill = (float)progresoActual.Value / interaccionesRequeridas;
                    float stepFill = (pulsacionesActuales / pulsacionesRequeridas) * (1f / interaccionesRequeridas);
                    barraProgresoUI.fillAmount = Mathf.Clamp01(baseFill + stepFill);
                }
                if (cachedGameplayUI != null)
                {
                    float baseFill = (float)progresoActual.Value;
                    float stepFill = pulsacionesActuales / pulsacionesRequeridas;
                    cachedGameplayUI.ActualizarBarraProgreso(baseFill + stepFill, interaccionesRequeridas);
                }

                Debug.Log($"[Quest Trigger] ¡Golpe registrado! ({pulsacionesActuales}/{pulsacionesRequeridas})");

                if (pulsacionesActuales >= pulsacionesRequeridas)
                {
                    // Si es el golpe final, esperamos a que la animación de swing actual termine por completo (cooldown)
                    float tiempoRestante = Mathf.Max(0f, COOLDOWN_GOLPE - delay);
                    if (tiempoRestante > 0f)
                    {
                        yield return new WaitForSeconds(tiempoRestante);
                    }

                    if (estaInteractuando) // Validar por si el jugador abortó durante el remanente de la animación
                    {
                        CompletarInteraccionLocal();
                    }
                }
            }
        }

        private void AbortarInteraccion(bool reproducirFallo = false)
        {
            Debug.Log("[Quest Trigger] Interacción cancelada.");
            if (cachedGameplayUI != null)
            {
                cachedGameplayUI.MostrarBarraProgreso(false);
                cachedGameplayUI.MostrarTimingBar(false, 0, 0);
            }

            if (mecanica == MecanicaInteraccion.RitmoAfilar || mecanica == MecanicaInteraccion.Restregar)
            {
                if (mecanica == MecanicaInteraccion.RitmoAfilar)
                {
                    var minijuego = Object.FindAnyObjectByType<MinijuegoAfilarUI>(FindObjectsInactive.Include);
                    if (minijuego != null) minijuego.DetenerMinijuego();
                }
                else if (mecanica == MecanicaInteraccion.Restregar)
                {
                    var minijuego = Object.FindAnyObjectByType<MinijuegoRestregarUI>(FindObjectsInactive.Include);
                    if (minijuego != null) minijuego.DetenerMinijuego();
                    ActualizarProgresoShaderLimpieza(0f);
                }

                if (cachedPlayerInput != null && cachedPlayerInput.actions != null)
                {
                    var moveAction = cachedPlayerInput.actions.FindAction("Move");
                    if (moveAction != null) moveAction.Enable();
                }
            }

            if (cachedTPC != null)
            {
                if (!reproducirFallo)
                {
                    cachedTPC.CanMove = true;
                }
                else
                {
                    cachedTPC.CanMove = false;
                    StartCoroutine(UnlockMovementAfterDelay(3.0f, cachedTPC));
                }
            }

            if (cachedToolVisuals != null && !string.IsNullOrEmpty(toolActual))
                cachedToolVisuals.SetToolByName(toolActual, false);

            var inventory = cachedInventory ?? jugadorLocal?.GetComponent<PlayerInventory>();
            inventory?.CancelUniversalInteractionServerRpc(toolActual, reproducirFallo);

            // Limpiar animación del NPC si tiene Animator
            if (cachedNpcAnimator == null) cachedNpcAnimator = GetComponentInChildren<Animator>();
            if (cachedNpcAnimator != null)
            {
                cachedNpcAnimator.SetBool("IsInteracting", false);
                cachedNpcAnimator.SetInteger("InteractionType", 0);
                if (cachedNpcAnimator.HasState(0, Animator.StringToHash("Grounded")))
                {
                    cachedNpcAnimator.Play("Grounded");
                }
            }

            ResetVisualsLocales();
        }


        private void ResetVisualsLocales()
        {
            estaInteractuando = false;
            tiempoTranscurrido = 0f;
            if (cachedGameplayUI != null) cachedGameplayUI.MostrarBarraProgreso(false);

            if (jugadorEnZona)
            {
                OnProgresoChanged(0, progresoActual.Value);
            }
            else
            {
                if (hudObject != null) hudObject.SetActive(false);
                if (barraProgresoUI != null && progresoActual.Value < interaccionesRequeridas) barraProgresoUI.gameObject.SetActive(false);
                jugadorLocal = null;
                cachedTPC = null;
                cachedInputs = null;
                cachedPlayerInput = null;
                cachedToolVisuals = null;
            }
        }

        private bool EsPasoValidoDeMisionActual()
        {
            // Si el objeto ya alcanzó el máximo de interacciones/completado, denegar inmediatamente
            if (interaccionesRequeridas > 0 && progresoActual.Value >= interaccionesRequeridas) return false;

            // Objeto genérico sin misión asignada: siempre válido
            if (materialAsignado == MaterialType.Ninguno && zonaUbicacion == ZoneID.Desconocida && questData == null) return true;

            if (jugadorLocal == null) return false;
            var tracker = cachedTracker ?? jugadorLocal.GetComponent<PlayerQuestTracker>();
            if (tracker == null) return false;

            // Bloquear si ya completó esta misión
            if (QuestValidator.MisionBloqueada(tracker, questData?.questID)) return false;

            string activeQuestId = tracker.currentQuestID.Value.ToString().TrimEnd('\0');
            if (string.IsNullOrEmpty(activeQuestId) || QuestValidator.MisionBloqueada(tracker, activeQuestId)) return false;

            // Delegar la validación completa a QuestValidator
            return mecanica == MecanicaInteraccion.PuntoEntrega
                ? QuestValidator.EntregaEsValidaParaQuestActiva(tracker, materialRequeridoParaEntrega, materialAsignado)
                : QuestValidator.JugadorTieneQuestActivaParaObjeto(
                    tracker, materialAsignado, zonaUbicacion, materialRequeridoParaEntrega,
                    questData?.questID);
        }

        /// <summary>Obtiene el tipo de animación del paso activo, con fallback al QuestData y luego al campo legacy.</summary>
        private int ObtenerAnimTypeActual()
        {
            var step = GetCurrentQuestStep();
            if (step != null) return step.GetAnimType(questData);

            int animType = tipoAnimacionRecogida;
            if (animType == 0 && questData != null) animType = questData.animationInteractionType;
            return animType;
        }

        /// <summary>Obtiene la duración del paso activo, con fallback al QuestData y luego por mecánica.</summary>
        private float ObtenerDurationActual()
        {
            float mecDefault = mecanica == MecanicaInteraccion.Transportar  ? 0.5f
                             : mecanica == MecanicaInteraccion.PuntoEntrega ? 1.0f : 2.0f;

            var step = GetCurrentQuestStep();
            return step != null ? step.GetDuration(questData, mecDefault) : mecDefault;
        }

        /// <summary>Devuelve el QuestStep activo del tracker del jugador local, o null si no hay ninguno.</summary>
        private QuestStep GetCurrentQuestStep()
        {
            if (jugadorLocal == null) return null;
            var tracker = cachedTracker ?? jugadorLocal.GetComponent<PlayerQuestTracker>();
            if (tracker == null) return null;

            string qid = tracker.currentQuestID.Value.ToString().TrimEnd('\0');
            if (string.IsNullOrEmpty(qid)) return null;

            var quest = QuestManager.Instance?.GetQuestByID(qid);
            if (quest == null) return null;

            int idx = tracker.currentStepIndex.Value;
            return (idx >= 0 && idx < quest.pasos.Count) ? quest.pasos[idx] : null;
        }

        // ───────────────────────────────────────────────────────────────────────
        // MÉTODOS DE SERVIDOR
        // ───────────────────────────────────────────────────────────────────────
        
        /// <summary>
        /// Llamado por PlayerInventory al terminar con éxito una interacción en el Servidor.
        /// Retorna TRUE si se alcanzó el máximo de interacciones requeridas (para dar el oro).
        /// </summary>
        public bool MarcarProgresoServer()
        {
            if (!IsServer) return false;
            
            if (progresoActual.Value < interaccionesRequeridas)
            {
                progresoActual.Value++;
            }
            
            return progresoActual.Value >= interaccionesRequeridas;
        }

        /// <summary>
        /// Expuesto para que GameManager resetee el día.
        /// </summary>
        public void ResetearMisionDiaria()
        {
            if (!IsServer) return;
            progresoActual.Value = 0;
        }

        /// <summary>
        /// Restaura la velocidad del animator a 1 después de un tiempo para sincronizar impacto con el timing.
        /// </summary>
        private System.Collections.IEnumerator RestaurarVelocidadAnimacion(Animator anim, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (anim != null)
            {
                anim.speed = 1.0f;
            }
        }

        /// <summary>
        /// Espera a que el swing termine y luego lanza el fallo (headno).
        /// </summary>
        private System.Collections.IEnumerator FalloConRetraso(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (estaInteractuando) // Solo si no fue abortado antes
            {
                if (cachedGameplayUI != null) cachedGameplayUI.MostrarTimingBar(false, 0, 0);
                AbortarInteraccion(true);
            }
        }

        /// <summary>
        /// Espera el tiempo especificado y devuelve el control de movimiento al jugador local.
        /// </summary>
        private System.Collections.IEnumerator UnlockMovementAfterDelay(float delay, StarterAssets.ThirdPersonController tpc)
        {
            yield return new WaitForSeconds(delay);
            if (tpc != null)
            {
                tpc.CanMove = true;
            }
        }

        /// <summary>
        /// Restaura la animación de interacción activa tras reproducir el gesto de fallo 'headno'.
        /// </summary>
        private System.Collections.IEnumerator RestaurarAnimacionTrasFallo(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (cachedAnimatorJugador != null && estaInteractuando)
            {
                int animType = ObtenerAnimTypeActual();
                cachedAnimatorJugador.SetBool("IsInteracting", true);
                cachedAnimatorJugador.SetInteger("InteractionType", animType);
            }
        }

        /// <summary>
        /// Actualiza la propiedad float del shader para el blend de textura visual de limpieza.
        /// </summary>
        public void ActualizarProgresoShaderLimpieza(float progresoLimpio)
        {
            if (rendererObjetivoLimpieza == null)
            {
                rendererObjetivoLimpieza = GetComponentInChildren<Renderer>();
            }
            if (rendererObjetivoLimpieza != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                rendererObjetivoLimpieza.GetPropertyBlock(block);
                block.SetFloat(nombrePropiedadShader, progresoLimpio);
                rendererObjetivoLimpieza.SetPropertyBlock(block);
            }
        }

        /// <summary>
        /// Aplica un micro-movimiento al jugador al barrer/restregar sin salirse del radio de interacción.
        /// </summary>
        private void MoverJugadorEnZonaLimpieza(UnityEngine.InputSystem.Key tecla)
        {
            if (jugadorLocal == null || cachedTPC == null) return;
            var cc = cachedTPC.GetComponent<CharacterController>();
            if (cc == null) return;

            // Vector de paso lateral según la tecla A o D
            Vector3 offsetDir = (tecla == UnityEngine.InputSystem.Key.A) ? -jugadorLocal.transform.right : jugadorLocal.transform.right;
            float pasoDistancia = 0.15f; // Pequeño paso de 15 cm por pulsación
            Vector3 desplazamientoPropuesto = offsetDir * pasoDistancia;
            Vector3 posicionFutura = jugadorLocal.transform.position + desplazamientoPropuesto;

            // Radio máximo permitido desde el centro del objeto interactuable
            float radioMaximoZona = 2.0f;
            float distanciaAlCentro = Vector3.Distance(posicionFutura, transform.position);

            if (distanciaAlCentro > radioMaximoZona)
            {
                // Si la posición futura se sale de la zona, rebotar hacia el centro
                Vector3 direccionAlCentro = (transform.position - jugadorLocal.transform.position).normalized;
                direccionAlCentro.y = 0f;
                desplazamientoPropuesto = direccionAlCentro * pasoDistancia;
            }

            // Mover físicamente con el CharacterController
            cc.Move(desplazamientoPropuesto);

            // Orientar suavemente mirando al objeto de interacción
            Vector3 lookDir = (transform.position - jugadorLocal.transform.position);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                jugadorLocal.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }
}

