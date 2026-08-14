using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Core.Enums;
using Core.QuestSystem;

namespace Core.Environment
{
    /// <summary>
    /// Objeto interactuable de misión en red (Server-Authoritative).
    /// </summary>
    public class QuestInteractable : NetworkBehaviour
    {
        [Header("Modular Quest Identificadores")]
        [Tooltip("Qué tipo de material es este objeto")]
        public MaterialType materialAsignado = MaterialType.AceroSierra;
        
        [Tooltip("En qué zona del mapa se encuentra")]
        public ZoneID zonaUbicacion = ZoneID.Desconocida;

        [Header("Configuración Opcional")]
        public bool destruirAlRecoger = true;
        public string textoInteraccion = "[E] Recoger Objeto";
        
        [Tooltip("Si es mayor a 0, otorga esta cantidad del MaterialAsignado al inventario del jugador.")]
        public int cantidadMaterialOtorgado = 1;

        [Header("Animación de Recogida")]
        [Tooltip("ID de Animación (1=recoger_aga, 3=recoger)")]
        public int tipoAnimacionRecogida = 3;

        private bool jugadorLocalCerca = false;
        private PlayerQuestTracker trackerLocal;
        private GameObject fireObject;
        private bool isProcessingInteraction = false;

        private void Awake()
        {
            // Si es una vela, buscar y apagar la llama/fuego al inicio
            if (materialAsignado == MaterialType.Vela)
            {
                fireObject = FindChildRecursive(transform, "fuego");
                if (fireObject == null) fireObject = FindChildRecursive(transform, "fire");
                if (fireObject == null) fireObject = FindChildRecursive(transform, "flame");
                if (fireObject == null) fireObject = FindChildRecursive(transform, "light");
                if (fireObject == null) fireObject = FindChildRecursive(transform, "particle");
                if (fireObject == null) fireObject = FindChildRecursive(transform, "particula");

                if (fireObject != null)
                {
                    fireObject.SetActive(false); // Apagada al principio de la partida
                }
            }
        }

        private void TriggerRecogidaAnimation()
        {
            if (trackerLocal == null) return;
            Animator anim = trackerLocal.GetComponent<Animator>();
            if (anim != null)
            {
                StartCoroutine(PlayQuickAnimationRoutine(anim, trackerLocal));
            }
        }

        private System.Collections.IEnumerator PlayQuickAnimationRoutine(Animator anim, PlayerQuestTracker tracker)
        {
            isProcessingInteraction = true;

            // Bloquear inputs para que el jugador no se mueva durante la animación
            var playerInput = tracker.GetComponent<PlayerInput>() ?? tracker.GetComponentInParent<PlayerInput>();
            if (playerInput != null && playerInput.currentActionMap != null)
            {
                playerInput.currentActionMap.Disable();
            }

            // [CLIENT-SIDE] Esta rutina ejecuta efectos visuales locales en el cliente del jugador local.
            anim.SetBool("IsInteracting", true);
            anim.SetInteger("InteractionType", tipoAnimacionRecogida);

            // --- Obtener referencia de herramientas en el cliente ---
            var toolVisuals = anim.GetComponent<PlayerToolVisuals>() ?? anim.GetComponentInParent<PlayerToolVisuals>();
            
            bool esLeña     = (materialAsignado == MaterialType.Leña);
            bool esEscoba   = (materialAsignado == MaterialType.Escoba);
            bool esRiego    = (materialAsignado == MaterialType.BaldeAgua);
            bool esBeber    = (materialAsignado == MaterialType.Frasco);
            bool esApunalar = (materialAsignado == MaterialType.Afilado);

            string toolName = "";
            if (esLeña)     toolName = "axe";
            if (esEscoba)   toolName = "escoba";
            if (esRiego)    toolName = "regadera";
            if (esBeber)    toolName = "pocion";
            if (esApunalar) toolName = "daga";

            // Sincronizar animación y herramienta en red a través del PlayerInventory del jugador local
            var inventory = tracker.GetComponent<PlayerInventory>();
            if (inventory != null && !string.IsNullOrEmpty(toolName))
            {
                inventory.StartInteractionSyncServerRpc(toolName, tipoAnimacionRecogida);
            }

            // Activar visualmente la herramienta correspondiente en el cliente
            if (toolVisuals != null)
            {
                if (esLeña)     toolVisuals.SetAxeActive(true);
                if (esEscoba)   toolVisuals.SetEscobaActive(true);
                if (esRiego)    toolVisuals.SetRegaderaActive(true);
                if (esBeber)    toolVisuals.SetPocionActive(true);
                if (esApunalar) toolVisuals.SetDagaActive(true);
            }

            // Si es la tarea de regar el huerto (BaldeAgua), activar partículas de agua locales
            if (esRiego)
            {
                var wateringParticles = anim.GetComponent<PlayerWateringParticles>() ?? anim.GetComponentInParent<PlayerWateringParticles>();
                if (wateringParticles != null)
                {
                    wateringParticles.PlayWaterParticles();
                }
            }

            // Si es la tarea de encender velas (Vela), encendemos el fuego permanentemente
            bool esVela = (materialAsignado == MaterialType.Vela);
            if (esVela && fireObject != null)
            {
                fireObject.SetActive(true);
                // Forzar looping y play en todos los sistemas de partículas (hijos incluidos) para que no se apaguen solos
                ParticleSystem[] allPS = fireObject.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in allPS)
                {
                    var main = ps.main;
                    main.loop = true; // Forzar bucle continuo
                    main.duration = 99999f; // Forzar duración ultra larga para que no expire
                    main.stopAction = ParticleSystemStopAction.None;
                    ps.Play(true);
                }
            }

            // Obtener el tiempo de la misión desde el QuestData si existe
            float waitTime = 1.5f;
            string activeQuestId = tracker.currentQuestID.Value.ToString();
            if (!string.IsNullOrEmpty(activeQuestId) && QuestManager.Instance != null)
            {
                QuestData qData = QuestManager.Instance.GetQuestByID(activeQuestId);
                if (qData != null) waitTime = qData.interactionDuration;
            }

            yield return new WaitForSeconds(waitTime);

            if (inventory != null && !string.IsNullOrEmpty(toolName))
            {
                inventory.StopInteractionSyncServerRpc(toolName);
            }

            anim.SetBool("IsInteracting", false);

            // Ocultar la herramienta al finalizar en el cliente
            if (toolVisuals != null)
            {
                if (esLeña)     toolVisuals.SetAxeActive(false);
                if (esEscoba)   toolVisuals.SetEscobaActive(false);
                if (esRiego)    toolVisuals.SetRegaderaActive(false);
                if (esBeber)    toolVisuals.SetPocionActive(false);
                if (esApunalar) toolVisuals.SetDagaActive(false);
            }

            // Ejecutar la acción lógica real EN EL SERVIDOR ahora que la animación terminó
            if (tracker != null)
            {
                tracker.ProcessStepServerRpc(materialAsignado, zonaUbicacion, transform.position);
                
                if (cantidadMaterialOtorgado > 0)
                {
                    if (inventory != null)
                    {
                        inventory.AddMaterialServerRpc(materialAsignado, cantidadMaterialOtorgado, transform.position);
                    }
                }
            }

            // Restaurar inputs
            if (playerInput != null && playerInput.currentActionMap != null)
            {
                playerInput.currentActionMap.Enable();
            }

            if (destruirAlRecoger)
            {
                DespawnObjetoServerRpc();
            }

            isProcessingInteraction = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger) return;
            
            NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                var tracker = netObj.GetComponent<PlayerQuestTracker>();
                if (tracker != null)
                {
                    // VALIDACIÓN: Solo permitir si el jugador tiene la misión correspondiente activa
                    if (TieneMisionRequerida(tracker))
                    {
                        trackerLocal = tracker;
                        jugadorLocalCerca = true;
                        // Mostrar UI si existe
                        var ui = FindAnyObjectByType<GameplayUI>();
                        if (ui != null) ui.MostrarMensajeTarea(textoInteraccion, 0f);
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.isTrigger) return;

            NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                 if (trackerLocal != null)
                 {
                     jugadorLocalCerca = false;
                     trackerLocal = null;
                     var ui = FindAnyObjectByType<GameplayUI>();
                     if (ui != null) ui.MostrarMensajeTarea("", 0f);
                 }
            }
        }

        private void Update()
        {
            if (jugadorLocalCerca && trackerLocal != null)
            {
                // Re-validar por si la misión cambió o se canceló
                if (!TieneMisionRequerida(trackerLocal))
                {
                    jugadorLocalCerca = false;
                    trackerLocal = null;
                    var ui = FindAnyObjectByType<GameplayUI>();
                    if (ui != null) ui.MostrarMensajeTarea("", 0f);
                    return;
                }

                if (!isProcessingInteraction && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    // Inicia la corrutina que bloqueará al jugador, reproducirá la animación y luego avanzará la misión.
                    TriggerRecogidaAnimation();
                }
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void DespawnObjetoServerRpc()
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
        }

        /// <summary>
        /// Valida si el jugador local tiene actualmente asignada la misión que requiere este interactuable.
        /// </summary>
        private bool TieneMisionRequerida(PlayerQuestTracker tracker)
        {
            if (tracker == null) return false;
            string activeQuestId = tracker.currentQuestID.Value.ToString();
            if (string.IsNullOrEmpty(activeQuestId)) return false;

            QuestData qData = QuestManager.Instance?.GetQuestByID(activeQuestId);
            if (qData == null) return false;

            int stepIdx = tracker.currentStepIndex.Value;
            if (stepIdx < 0 || stepIdx >= qData.pasos.Count) return false;

            QuestStep currentStep = qData.pasos[stepIdx];
            return (currentStep.materialRequerido == materialAsignado && currentStep.zonaRequerida == zonaUbicacion);
        }

        private GameObject FindChildRecursive(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name.ToLower().Contains(childName.ToLower()))
                {
                    return child.gameObject;
                }
                
                GameObject result = FindChildRecursive(child, childName);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}
