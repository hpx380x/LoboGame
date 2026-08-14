using Unity.Netcode;
using UnityEngine;
using Core.Enums;

public class PlayerInventory : NetworkBehaviour
{
    [Header("Inventario Autoritativo")]
    public NetworkVariable<int> monedas = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<TipoObjeto> objetoEnMano = new NetworkVariable<TipoObjeto>(
        TipoObjeto.Ninguno, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    [Header("Materiales para Misiones")]
    public NetworkVariable<MaterialesMision> materiales = new NetworkVariable<MaterialesMision>(
        new MaterialesMision(30), // Tamaño basado en el número de entradas en MaterialType (aumentado de 8 a 30)
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [System.Serializable]
    public struct MaterialesMision : INetworkSerializable
    {
        // Guardamos las cantidades en un array donde el índice es el valor del enum MaterialType
        public int[] cantidades;

        public MaterialesMision(int size)
        {
            cantidades = new int[size];
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            int length = 0;
            if (!serializer.IsReader) length = cantidades != null ? cantidades.Length : 0;
            serializer.SerializeValue(ref length);

            if (serializer.IsReader) cantidades = new int[length];

            for (int objIdx = 0; objIdx < length; objIdx++)
            {
                serializer.SerializeValue(ref cantidades[objIdx]);
            }
        }

        // Métodos de conveniencia
        public int GetCount(MaterialType type)
        {
            int idx = (int)type;
            if (cantidades == null || idx < 0 || idx >= cantidades.Length) return 0;
            return cantidades[idx];
        }

        public void SetCount(MaterialType type, int count)
        {
            int idx = (int)type;
            if (cantidades != null && idx >= 0 && idx < cantidades.Length)
            {
                cantidades[idx] = count;
            }
        }
    }

    [Header("Configuración de Agarre (Anclaje)")]
    [Tooltip("Arrastra aquí el hueso de la mano derecha del personaje para que los objetos no floten")]
    [SerializeField] private Transform puntoDeAgarreMano;

    [Header("Modelos 3D Visuales (Hijos de la Mano)")]
    [Tooltip("Asocia cada TipoObjeto a su GameObject (Malla 3D) correspondiente")]
    [SerializeField] private VisualObjetoMapping[] modelosVisuales;

    [System.Serializable]
    public struct VisualObjetoMapping
    {
        public TipoObjeto tipo;
        public GameObject modelo;
    }

    private PlayerState playerState;

    private void Awake()
    {
        playerState = GetComponent<PlayerState>();
    }

    public override void OnNetworkSpawn()
    {
        objetoEnMano.OnValueChanged += OnObjetoCambiado;
        monedas.OnValueChanged += OnMonedasCambiadas;
        
        // Cargar estado inicial por si entra tarde a la partida
        ActualizarVisualizacionObjeto(objetoEnMano.Value);
        
        if (IsOwner)
        {
            GameplayUI ui = FindAnyObjectByType<GameplayUI>();
            if (ui != null)
            {
                ui.VincularJugadorLocal(gameObject);
                ui.ActualizarInventario(objetoEnMano.Value.ToString());
                ui.ActualizarMonedas(monedas.Value);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        objetoEnMano.OnValueChanged -= OnObjetoCambiado;
        monedas.OnValueChanged -= OnMonedasCambiadas;
        base.OnNetworkDespawn();
    }

    // --- ACCIONES SERVIDOR ---

    [Rpc(SendTo.Server)]
    public void RecogerObjetoServerRpc(TipoObjeto nuevoObjeto)
    {
        if (playerState != null && playerState.isDead.Value) return;
        objetoEnMano.Value = nuevoObjeto;
        Debug.Log($"[Server] El jugador {OwnerClientId} ha recogido exitosamente: {nuevoObjeto}");
    }

    [Rpc(SendTo.Server)]
    public void GanarMonedasServerRpc(int cantidad)
    {
        if (playerState != null && playerState.isDead.Value) return;
        monedas.Value += cantidad;
        Debug.Log($"[Server] El jugador {OwnerClientId} ganó {cantidad} monedas. Total: {monedas.Value}");
    }

    [Rpc(SendTo.Server)]
    public void AddMaterialServerRpc(MaterialType type, int cantidad, Vector3 interactionPos)
    {
        if (playerState != null && playerState.isDead.Value) return;

        // [ANTI-CHEAT] Validación Espacial en Servidor
        float distance = Vector3.Distance(transform.position, interactionPos);
        if (distance > 4.5f)
        {
            Debug.LogWarning($"[Server Anti-Cheat] Jugador {OwnerClientId} intentó generar material desde muy lejos ({distance}m). Petición rechazada.");
            return;
        }
        
        MaterialesMision actual = materiales.Value;
        int nuevoTotal = actual.GetCount(type) + cantidad;
        actual.SetCount(type, nuevoTotal);
        materiales.Value = actual;

        Debug.Log($"[Server] Jugador {OwnerClientId} recibió {cantidad} de {type}. Total: {nuevoTotal}");
    }

    [Rpc(SendTo.Server)]
    public void ComprarObjetoServerRpc(TipoObjeto objetoDeseado, int coste)
    {
        if (playerState != null && playerState.isDead.Value) return;
        if (monedas.Value >= coste)
        {
            monedas.Value -= coste;

            // Si es un pergamino de misión legendaria, activamos la misión en vez de equiparlo
            if (EsPergaminoMision(objetoDeseado))
            {
                var tracker = GetComponent<Core.QuestSystem.PlayerQuestTracker>();
                if (tracker != null)
                {
                    string questId = ObtenerQuestIdParaObjeto(objetoDeseado);
                    tracker.AcceptQuestServerRpc(questId);
                }
            }
            else
            {
                objetoEnMano.Value = objetoDeseado;
            }
            Debug.Log($"[Server] El jugador {OwnerClientId} compró {objetoDeseado} por {coste} monedas.");
        }
        else
        {
            Debug.LogWarning($"[Server] Jugador {OwnerClientId} intentó comprar {objetoDeseado} sin fondos suficientes.");
        }
    }

    private bool EsPergaminoMision(TipoObjeto tipo)
    {
        return tipo >= TipoObjeto.PergaminoCupido && tipo <= TipoObjeto.PergaminoSombrero;
    }

    private string ObtenerQuestIdParaObjeto(TipoObjeto tipo)
    {
        switch (tipo)
        {
            case TipoObjeto.PergaminoCupido: return "mision_arco_cupido";
            case TipoObjeto.PergaminoDaga: return "mision_daga_muerte";
            case TipoObjeto.PergaminoManzana: return "mision_manzana_oro";
            case TipoObjeto.PergaminoPocion: return "mision_pocion_bruja";
            case TipoObjeto.PergaminoAntifaz: return "mision_antifaz_ladron";
            case TipoObjeto.PergaminoRelicario: return "mision_relicario_nina";
            case TipoObjeto.PergaminoSombrero: return "mision_sombrero_tonto";
            default: return "";
        }
    }

    // --- MÉTODOS LOCALES Y VISUALES ---

    private void OnObjetoCambiado(TipoObjeto viejo, TipoObjeto nuevo)
    {
        ActualizarVisualizacionObjeto(nuevo);

        if (IsOwner)
        {
            GameplayUI ui = FindAnyObjectByType<GameplayUI>();
            if (ui != null) ui.ActualizarInventario(nuevo.ToString());
        }
    }

    private void OnMonedasCambiadas(int viejo, int nuevo)
    {
        if (IsOwner)
        {
            GameplayUI ui = FindAnyObjectByType<GameplayUI>();
            if (ui != null) ui.ActualizarMonedas(nuevo);
        }
    }

    private void ActualizarVisualizacionObjeto(TipoObjeto obj)
    {
        if (modelosVisuales == null) return;

        foreach (var mapping in modelosVisuales)
        {
            if (mapping.modelo != null)
            {
                // Si el tipo coincide con el equipado, lo activamos. Si no, lo apagamos.
                bool activar = (mapping.tipo == obj);
                mapping.modelo.SetActive(activar);
            }
        }
    }

    // ───────────────────────────────────────────────────────────────────────
    // SISTEMA DE INTERACCIONES UNIVERSALES (SERVER AUTHORITATIVE)
    // ───────────────────────────────────────────────────────────────────────

        private Coroutine activeUniversalCoroutine = null;
        private Coroutine activeFailureCoroutine = null;

        [Rpc(SendTo.Server)]
        public void ProcessUniversalInteractionServerRpc(Vector3 interactionPos, string toolName, float duration, int goldReward, ulong interactableId = 0, Core.Enums.MaterialType matType = Core.Enums.MaterialType.Ninguno, int matAmount = 0, int animType = 0)
        {
            if (playerState != null && playerState.isDead.Value) return;

            // [ANTI-CHEAT] Validación de distancia en el servidor
            float distance = Vector3.Distance(transform.position, interactionPos);
            if (distance > 4.5f)
            {
                Debug.LogWarning($"[Server Anti-Cheat] Jugador {OwnerClientId} intentó interactuar desde muy lejos ({distance}m).");
                return;
            }

            // Informar al resto de clientes para que activen sus visuales de la herramienta y animación
            SyncUniversalVisualsClientRpc(toolName, true, animType);

            // Iniciar recompensa asíncrona
            if (duration >= 0f)
            {
                if (activeUniversalCoroutine != null) StopCoroutine(activeUniversalCoroutine);
                activeUniversalCoroutine = StartCoroutine(UniversalRewardRoutine(toolName, duration, goldReward, interactableId, matType, matAmount, interactionPos));
            }
        }

        [Rpc(SendTo.Server)]
        public void CancelUniversalInteractionServerRpc(string toolName, bool playFailure = false)
        {
            if (activeUniversalCoroutine != null)
            {
                StopCoroutine(activeUniversalCoroutine);
                activeUniversalCoroutine = null;
            }

            if (playFailure)
            {
                if (activeFailureCoroutine != null) StopCoroutine(activeFailureCoroutine);
                activeFailureCoroutine = StartCoroutine(ServerJugarAnimacionFalloYRestaurar(toolName));
            }
            else
            {
                if (activeFailureCoroutine != null)
                {
                    StopCoroutine(activeFailureCoroutine);
                    activeFailureCoroutine = null;
                }
                SyncUniversalVisualsClientRpc(toolName, false);
            }
            Debug.Log($"[Server] Jugador {OwnerClientId} canceló la interacción. Fallo: {playFailure}");
        }

        private System.Collections.IEnumerator ServerJugarAnimacionFalloYRestaurar(string toolName)
        {
            // Apagar la herramienta a todos primero
            SyncUniversalVisualsClientRpc(toolName, false);
            
            // Esperar 1 frame para la limpieza
            yield return null;

            Animator anim = GetComponent<Animator>();
            if (anim == null) anim = GetComponentInChildren<Animator>();
            
            if (anim != null)
            {
                anim.SetBool("IsInteracting", true);
                anim.SetInteger("InteractionType", 14); // headno (14)
            }

            LockMovementClientRpc(true);

            yield return new WaitForSeconds(3.0f); // Duración de headno (3 segundos)

            if (anim != null)
            {
                anim.SetBool("IsInteracting", false);
                anim.SetInteger("InteractionType", 0);
                if (anim.HasState(0, Animator.StringToHash("Grounded")))
                {
                    anim.Play("Grounded");
                }
            }

            LockMovementClientRpc(false);
            activeFailureCoroutine = null;
        }

        [Rpc(SendTo.Owner)]
        private void LockMovementClientRpc(bool lockMove)
        {
            if (TryGetComponent(out StarterAssets.ThirdPersonController tpc))
            {
                tpc.CanMove = !lockMove;
            }
        }

        private System.Collections.IEnumerator UniversalRewardRoutine(string toolName, float duration, int goldReward, ulong interactableId, Core.Enums.MaterialType matType, int matAmount, Vector3 interactionPos)
        {
            yield return new WaitForSeconds(duration);
            activeUniversalCoroutine = null;

            bool esFaseFinal = false;
            NetworkObject interactableObj = null;

            // [SERVER] Avanzar el progreso si tiene ID válido
            if (interactableId != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(interactableId, out interactableObj))
            {
                var interactable = interactableObj.GetComponent<Core.Environment.UniversalQuestInteractable>();
                if (interactable != null)
                {
                    var questTracker = GetComponent<Core.QuestSystem.PlayerQuestTracker>();
                    if (interactable.questData != null && questTracker != null && questTracker.currentQuestID.Value.ToString().TrimEnd('\0') != interactable.questData.questID)
                    {
                        Debug.LogWarning($"[Server Anti-Cheat] Jugador {OwnerClientId} intentó interactuar con la quest {interactable.questData.questID} sin tenerla asignada.");
                        goldReward = 0;
                        esFaseFinal = false;
                    }
                    else
                    {
                        esFaseFinal = interactable.MarcarProgresoServer();
                        if (interactable.questData != null)
                        {
                            // Si el objeto está asociado a una QuestData, no damos oro por paso/objeto individual.
                            // El oro se otorgará únicamente al completar todos los pasos en PlayerQuestTracker.
                            goldReward = 0;
                        }
                    }
                }
            }
            else
            {
                Debug.LogError($"[Server Anti-Cheat/Bug] El interactuable no tiene NetworkObject o su ID es 0. ¡Asegúrate de haber añadido el componente 'NetworkObject' al Prefab del huerto en Unity!");
                // Abortar si el huerto está mal configurado
            }

            if (esFaseFinal)
            {
                var questTracker = GetComponent<Core.QuestSystem.PlayerQuestTracker>();
                if (questTracker != null && interactableObj != null)
                {
                    var interactable = interactableObj.GetComponent<Core.Environment.UniversalQuestInteractable>();
                    if (interactable != null)
                    {
                        questTracker.ProcessStepServerRpc(interactable.materialAsignado, interactable.zonaUbicacion, interactionPos);
                    }
                }

                // [SERVER] Consumir material si era un Punto de Entrega de material
                if (interactableObj != null)
                {
                    var interactable = interactableObj.GetComponent<Core.Environment.UniversalQuestInteractable>();
                    if (interactable != null && interactable.mecanica == Core.Enums.MecanicaInteraccion.PuntoEntrega)
                    {
                        if (interactable.materialRequeridoParaEntrega != Core.Enums.MaterialType.Ninguno)
                        {
                            MaterialesMision actual = materiales.Value;
                            int count = actual.GetCount(interactable.materialRequeridoParaEntrega);
                            actual.SetCount(interactable.materialRequeridoParaEntrega, Mathf.Max(0, count - interactable.cantidadRequeridaParaEntrega));
                            materiales.Value = actual;
                            Debug.Log($"[Server] Consumidos {interactable.cantidadRequeridaParaEntrega} de {interactable.materialRequeridoParaEntrega} del jugador {OwnerClientId} al entregar.");
                        }
                    }
                }

                // [SERVER] Recompensa autoritativa (oro)
                if (goldReward > 0)
                {
                    monedas.Value += goldReward;
                    Debug.Log($"[Server] Jugador {OwnerClientId} completó tarea FINAL. Oro actual: {monedas.Value}");
                }

                // [SERVER] Recompensa de material
                if (matAmount > 0 && matType != Core.Enums.MaterialType.Ninguno)
                {
                    AddMaterialServerRpc(matType, matAmount, interactionPos);
                }

                // [SERVER] Limpieza eliminada: la misión y el HUD ahora se gestionan y limpian
                // autoritativamente en PlayerQuestTracker.CompleteQuestOnServer al completar todos los pasos.
            }
            else
            {
                Debug.Log($"[Server] Jugador {OwnerClientId} completó un paso de la tarea, pero aún no es la fase final.");
            }

            // [SERVER] Apagar visuales a todos
            SyncUniversalVisualsClientRpc(toolName, false);

            // [SERVER] Indicar al llamador original que ha terminado (liberar input)
            FinishUniversalTargetRpc(esFaseFinal ? goldReward : 0, RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server)]
        public void StartInteractionSyncServerRpc(string toolName, int animType)
        {
            SyncUniversalVisualsClientRpc(toolName, true, animType);
        }

        [Rpc(SendTo.Server)]
        public void StopInteractionSyncServerRpc(string toolName)
        {
            SyncUniversalVisualsClientRpc(toolName, false, 0);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SyncUniversalVisualsClientRpc(string toolName, bool active, int animType = 0)
        {
            // El llamador original ya lo activó por predicción en el cliente.
            if (active && IsOwner) return;

            var tv = GetComponentInChildren<Core.QuestSystem.PlayerToolVisuals>(true);
            if (tv != null && !string.IsNullOrEmpty(toolName))
            {
                tv.SetToolByName(toolName, active);
            }

            Animator anim = GetComponent<Animator>();
            if (anim == null) anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetBool("IsInteracting", active);
                if (active)
                {
                    anim.SetInteger("InteractionType", animType);
                }
                else
                {
                    anim.SetInteger("InteractionType", 0);
                    if (anim.HasState(0, Animator.StringToHash("Grounded")))
                    {
                        anim.Play("Grounded");
                    }
                }
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void FinishUniversalTargetRpc(int goldReward, RpcParams rpcParams = default)
        {
            // [CLIENT] Terminar la tarea y liberar el input
            if (TryGetComponent(out StarterAssets.ThirdPersonController tpc))
            {
                tpc.CanMove = true;
                var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
                if (playerInput != null && playerInput.currentActionMap != null)
                {
                    playerInput.currentActionMap.Enable();
                }
            }

            Animator anim = GetComponent<Animator>();
            if (anim == null) anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetBool("IsInteracting", false);
                anim.SetInteger("InteractionType", 0);
                if (anim.HasState(0, Animator.StringToHash("Grounded")))
                {
                    anim.Play("Grounded");
                }
            }

            // La herramienta local se apaga mediante SyncUniversalVisualsClientRpc, pero el ClientRpc omite al dueño si active==true.
            // Para apagar (active==false), el ClientRpc NO omite al dueño, así que ya se apaga ahí. 
            // O podemos forzar apagar todo:
            var tv = GetComponentInChildren<Core.QuestSystem.PlayerToolVisuals>(true);
            if (tv != null) tv.HideAll();

            // Feedback visual
            if (goldReward > 0)
            {
                GameplayUI ui = Object.FindAnyObjectByType<GameplayUI>();
                if (ui != null) ui.MostrarMensajeTarea($"¡Tarea completada!\n+{goldReward} Oro", 3f);
            }
        }
    }

