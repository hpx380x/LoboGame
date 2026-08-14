using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.UI;

namespace Core.Environment
{
    public enum TipoInteraccion
    {
        MantenerPulsado,
        PulsarRepetidamente,
        TransportarObjeto
    }

    [RequireComponent(typeof(NetworkObject))]
    public class InteractivableMisionUniversal : NetworkBehaviour
    {
        [Header("Configuración del Inspector")]
        [Tooltip("ID única para identificar la misión en red y sincronizar duplicados.")]
        public string idMision = "mision_huerto";
        
        [Tooltip("Define la mecánica de interacción.")]
        public TipoInteraccion tipoMision = TipoInteraccion.MantenerPulsado;

        public float tiempoRequerido = 3f;
        public int pulsacionesRequeridas = 5;

        [Tooltip("Herramienta a usar (ej. 'regadera', 'axe', 'escoba').")]
        public string nombreHerramientaVisual = "";

        [Header("UI Local")]
        public GameObject panelHUD;
        public Image barraProgreso;

        // Estado de red sincronizado y protegido
        private NetworkVariable<bool> completadaHoy = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private bool jugadorEnZona = false;
        private GameObject jugadorLocal = null;
        private bool estaInteractuando = false;

        private float progresoActual = 0f;
        private int pulsacionesActuales = 0;

        // Caché de componentes del jugador para evitar GetComponent en Update
        private StarterAssets.ThirdPersonController cachedTPC = null;
        private PlayerInput cachedPlayerInput = null;

        private void Awake()
        {
            if (panelHUD != null) panelHUD.SetActive(false);
            if (barraProgreso != null) barraProgreso.fillAmount = 0f;

            // Configurar colisionador o buscar en el cubo padre mediante proxy
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
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
                    proxy.targetMisionUniversal = this;
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            // Suscripción al cambio de estado para bloquear en cadena
            completadaHoy.OnValueChanged += OnEstadoMisionCambiado;

            // Sincronización inicial por si el jugador entra tarde y ya estaba completada
            if (completadaHoy.Value)
            {
                OnEstadoMisionCambiado(false, true);
            }
        }

        public override void OnNetworkDespawn()
        {
            completadaHoy.OnValueChanged -= OnEstadoMisionCambiado;
        }

        private void OnEstadoMisionCambiado(bool estadoAnterior, bool estadoActual)
        {
            if (estadoActual)
            {
                // Buscar TODOS los interactuables en la escena que compartan este script
                var todosLosInteractuables = FindObjectsByType<InteractivableMisionUniversal>(FindObjectsInactive.Exclude);
                
                foreach (var interactuable in todosLosInteractuables)
                {
                    // Si tienen la misma ID de misión, se desactivan localmente en bloque
                    if (interactuable.idMision == this.idMision)
                    {
                        interactuable.DesactivarInteractuableLocal();
                    }
                }
            }
        }

        public void DesactivarInteractuableLocal()
        {
            if (panelHUD != null) panelHUD.SetActive(false);
            
            // Si el jugador justo estaba interactuando cuando otro la completó, se le aborta
            if (estaInteractuando) 
            {
                DescongelarJugador(jugadorLocal);
            }
            
            jugadorEnZona = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Bloqueo estricto: Si ya se completó, ignorar completamente el Trigger
            if (completadaHoy.Value) return;

            if (other.CompareTag("Player"))
            {
                var networkObj = other.GetComponent<NetworkObject>();
                if (networkObj != null && networkObj.IsOwner)
                {
                    // --- REGLA: Bloquear si el jugador tiene una misión activa distinta a esta ---
                    var tracker = other.GetComponent<Core.QuestSystem.PlayerQuestTracker>();
                    if (tracker != null)
                    {
                        string activeQuestId = tracker.currentQuestID.Value.ToString().TrimEnd('\0');
                        if (!string.IsNullOrEmpty(activeQuestId) && activeQuestId != this.idMision)
                        {
                            return;
                        }
                    }

                    jugadorEnZona = true;
                    jugadorLocal = other.gameObject;

                    // Cachear componentes para evitar GetComponent repetidos en Update
                    cachedTPC = jugadorLocal.GetComponent<StarterAssets.ThirdPersonController>();
                    cachedPlayerInput = jugadorLocal.GetComponent<PlayerInput>();

                    if (panelHUD != null) panelHUD.SetActive(true);
                    if (barraProgreso != null) barraProgreso.fillAmount = 0f;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && jugadorLocal == other.gameObject)
            {
                jugadorEnZona = false;
                
                if (estaInteractuando)
                {
                    CancelarInteraccionLocal();
                }
                else
                {
                    jugadorLocal = null;
                    cachedTPC = null;
                    cachedPlayerInput = null;
                    if (panelHUD != null) panelHUD.SetActive(false);
                }
            }
        }

        public void OnTriggerEnterProxy(Collider other) => OnTriggerEnter(other);
        public void OnTriggerExitProxy(Collider other) => OnTriggerExit(other);

        private void CancelarInteraccionLocal()
        {
            if (estaInteractuando && jugadorLocal != null)
            {
                DescongelarJugador(jugadorLocal);
                jugadorLocal = null;
                cachedTPC = null;
                cachedPlayerInput = null;
                if (panelHUD != null) panelHUD.SetActive(false);
            }
        }

        private void Update()
        {
            // Escudo de entrada estricto en el Cliente
            if (completadaHoy.Value || !jugadorEnZona || jugadorLocal == null) return;

            // Billboard HUD
            if (panelHUD != null && panelHUD.activeSelf && Camera.main != null)
            {
                panelHUD.transform.rotation = Camera.main.transform.rotation;
            }

            if (!estaInteractuando)
            {
                // Escudo de Input: No interactuar si el ActionMap está desactivado (ej: leyendo pergaminos)
                if (cachedPlayerInput != null && cachedPlayerInput.currentActionMap != null && !cachedPlayerInput.currentActionMap.enabled) return;

                // Escuchar el input solo si no se está interactuando ya
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    IniciarInteraccionLocal();
                }
            }
            else
            {
                // Lógica de progreso local
                switch (tipoMision)
                {
                    case TipoInteraccion.MantenerPulsado:
                        progresoActual += Time.deltaTime;
                        if (barraProgreso != null) barraProgreso.fillAmount = progresoActual / tiempoRequerido;

                        if (progresoActual >= tiempoRequerido)
                        {
                            FinalizarProgresoLocal();
                        }
                        break;

                    case TipoInteraccion.PulsarRepetidamente:
                        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                        {
                            pulsacionesActuales++;
                            if (barraProgreso != null) barraProgreso.fillAmount = (float)pulsacionesActuales / pulsacionesRequeridas;

                            if (pulsacionesActuales >= pulsacionesRequeridas)
                            {
                                FinalizarProgresoLocal();
                            }
                        }
                        break;

                    case TipoInteraccion.TransportarObjeto:
                        FinalizarProgresoLocal();
                        break;
                }
            }
        }

        private void IniciarInteraccionLocal()
        {
            estaInteractuando = true;
            progresoActual = 0f;
            pulsacionesActuales = 0;

            if (cachedTPC != null)
            {
                cachedTPC.CanMove = false;
            }
            else if (jugadorLocal != null && jugadorLocal.TryGetComponent(out StarterAssets.ThirdPersonController tpc))
            {
                tpc.CanMove = false;
            }

            if (cachedPlayerInput != null && cachedPlayerInput.currentActionMap != null)
            {
                cachedPlayerInput.currentActionMap.Disable();
            }
            else if (jugadorLocal != null)
            {
                var playerInput = jugadorLocal.GetComponent<PlayerInput>();
                if (playerInput != null && playerInput.currentActionMap != null)
                {
                    playerInput.currentActionMap.Disable();
                }
            }

            // Iniciar animación
            var anim = jugadorLocal != null ? jugadorLocal.GetComponentInChildren<Animator>() : null;
            if (anim != null) anim.SetBool("IsInteracting", true);

            // Activar herramienta visual
            var toolVisuals = jugadorLocal != null ? jugadorLocal.GetComponentInChildren<Core.QuestSystem.PlayerToolVisuals>() : null;
            if (toolVisuals != null && !string.IsNullOrEmpty(nombreHerramientaVisual))
            {
                toolVisuals.SetToolByName(nombreHerramientaVisual, true);
            }
        }

        private void FinalizarProgresoLocal()
        {
            estaInteractuando = false;
            
            // Enviamos petición al servidor para validar y recibir recompensa
            CompletarMisionServerRpc();
        }

        // ───────────────────────────────────────────────────────────────────────
        // VALIDACIÓN Y RECOMPENSAS EN EL SERVIDOR
        // ───────────────────────────────────────────────────────────────────────

        [Rpc(SendTo.Server)]
        private void CompletarMisionServerRpc(RpcParams rpcParams = default)
        {
            if (completadaHoy.Value) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                var playerObject = client.PlayerObject;
                if (playerObject != null)
                {
                    // Validación Espacial Anti-Cheat
                    float distanciaFisica = Vector3.Distance(transform.position, playerObject.transform.position);
                    
                    if (distanciaFisica > 4.5f)
                    {
                        Debug.LogWarning($"[Anti-Cheat Server] El jugador {clientId} intentó completar '{idMision}' desde muy lejos ({distanciaFisica}m). Petición rechazada.");
                        
                        // Fail-safe de descongelamiento: Ordenar al cliente mentiroso/bugeado que se libere.
                        RpcParams targetParams = RpcTarget.Single(clientId, RpcTargetUse.Temp);
                        DescongelarClienteClientRpc(targetParams);
                        return;
                    }

                    // 1. Estado Autoritativo Sincronizado
                    completadaHoy.Value = true;

                    // 2. Entrega de Recompensas Segura
                    var inventario = playerObject.GetComponent<PlayerInventory>();
                    if (inventario != null)
                    {
                        inventario.monedas.Value += 10; // Ejemplo de recompensa. 
                        Debug.Log($"[Server] Jugador {clientId} ha completado la misión {idMision} correctamente.");
                    }

                    // 3. Liberación Final del Cliente
                    RpcParams successParams = RpcTarget.Single(clientId, RpcTargetUse.Temp);
                    DescongelarClienteClientRpc(successParams);
                }
            }
        }

        // ───────────────────────────────────────────────────────────────────────
        // ACCIONES DIRIGIDAS (TARGET RPC) AL CLIENTE
        // ───────────────────────────────────────────────────────────────────────

        [Rpc(SendTo.SpecifiedInParams)]
        private void DescongelarClienteClientRpc(RpcParams rpcParams = default)
        {
            // Forzar descongelamiento seguro si este cliente estaba interactuando
            if (jugadorLocal != null)
            {
                DescongelarJugador(jugadorLocal);
            }
        }

        private void DescongelarJugador(GameObject jugadorTarget)
        {
            if (jugadorTarget == null) return;

            // 1. Liberar Movimiento e Input
            if (jugadorTarget == jugadorLocal)
            {
                if (cachedTPC != null)
                {
                    cachedTPC.CanMove = true;
                }
                else if (jugadorTarget.TryGetComponent(out StarterAssets.ThirdPersonController tpc))
                {
                    tpc.CanMove = true;
                }

                if (cachedPlayerInput != null && cachedPlayerInput.currentActionMap != null)
                {
                    cachedPlayerInput.currentActionMap.Enable();
                }
                else
                {
                    var playerInput = jugadorTarget.GetComponent<PlayerInput>();
                    if (playerInput != null && playerInput.currentActionMap != null)
                    {
                        playerInput.currentActionMap.Enable();
                    }
                }
            }
            else
            {
                if (jugadorTarget.TryGetComponent(out StarterAssets.ThirdPersonController tpc))
                {
                    tpc.CanMove = true;
                    
                    var playerInput = jugadorTarget.GetComponent<PlayerInput>();
                    if (playerInput != null && playerInput.currentActionMap != null)
                    {
                        playerInput.currentActionMap.Enable();
                    }
                }
            }

            // 2. Apagar Animaciones
            var anim = jugadorTarget.GetComponentInChildren<Animator>();
            if (anim != null) anim.SetBool("IsInteracting", false);

            // 3. Ocultar Herramienta
            var toolVisuals = jugadorTarget.GetComponentInChildren<Core.QuestSystem.PlayerToolVisuals>();
            if (toolVisuals != null && !string.IsNullOrEmpty(nombreHerramientaVisual))
            {
                toolVisuals.SetToolByName(nombreHerramientaVisual, false);
            }

            // 4. Limpieza de variables de estado
            if (barraProgreso != null) barraProgreso.fillAmount = 0f;
            estaInteractuando = false;
        }

        // ───────────────────────────────────────────────────────────────────────
        // CONTROL DE DÍA / NOCHE
        // ───────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Expuesto para que el GameMode o TimeManager resetee la misión en un nuevo día.
        /// </summary>
        public void ResetearMisionDiaria()
        {
            if (IsServer)
            {
                completadaHoy.Value = false;
            }
        }
    }
}
