using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using Core.Enums;

public class PlayerQuestTracker : NetworkBehaviour
{
    [Header("Misión Actual (Server-Auth)")]
    // Nombre del archivo ScriptableObject dentro de la carpeta Resources/Quests/
    public NetworkVariable<FixedString32Bytes> misionActivaId = new NetworkVariable<FixedString32Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> pasoActualIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> progresoPasoActual = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // [NUEVO] Para la misión colaborativa: Guarda el ID del jugador objetivo al que hay que ayudar
    public NetworkVariable<ulong> aliadoObjetivoId = new NetworkVariable<ulong>(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Datos locales para mostrar en UI
    private QuestData datosLocalesMision;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        misionActivaId.OnValueChanged += AlCambiarMision;
        pasoActualIndex.OnValueChanged += AlCambiarPaso;
        progresoPasoActual.OnValueChanged += AlCambiarProgreso;

        // Cargar estado inicial si nos unimos tarde
        if (misionActivaId.Value.ToString() != "")
        {
            CargarMisionLocal(misionActivaId.Value.ToString());
        }
    }

    public override void OnNetworkDespawn()
    {
        misionActivaId.OnValueChanged -= AlCambiarMision;
        pasoActualIndex.OnValueChanged -= AlCambiarPaso;
        progresoPasoActual.OnValueChanged -= AlCambiarProgreso;
        base.OnNetworkDespawn();
    }

    private void CargarMisionLocal(string questName)
    {
        // Se asume que los ScriptableObjects se guardarán en una carpeta llamada Resources/Quests/
        datosLocalesMision = Resources.Load<QuestData>("Quests/" + questName);
        ActualizarUILocal();
    }

    // ==========================================
    // SECCIÓN SERVIDOR (Lógica de Misión)
    // ==========================================

    public void AsignarMisionDesdeServidor(string questName)
    {
        if (!IsServer) return;
        misionActivaId.Value = new FixedString32Bytes(questName);
        pasoActualIndex.Value = 0;
        progresoPasoActual.Value = 0;
        
        // Cargarla también en el servidor para leer sus pasos y ver si requiere aliado
        QuestData serverQuestData = Resources.Load<QuestData>("Quests/" + questName);
        if (serverQuestData != null && serverQuestData.pasos.Count > 0)
        {
            PrepararPasoServidor(serverQuestData.pasos[0]);
        }
        
        Debug.Log($"[Servidor] Misión {questName} asignada al jugador {OwnerClientId}");
    }

    private void PrepararPasoServidor(PasoMision paso)
    {
        if (paso.tipoPaso == TipoPasoMision.AyudarAldeano)
        {
            // Elegir a un jugador aleatorio de la sala que no sea yo
            ulong aliadoId = ulong.MaxValue;
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                if (client.Key != OwnerClientId)
                {
                    aliadoId = client.Key;
                    break; // Tomamos al primero distinto por simplicidad
                }
            }
            aliadoObjetivoId.Value = aliadoId;
            Debug.Log($"[Servidor] El jugador {OwnerClientId} debe ir a ayudar al Jugador {aliadoId}");
        }
        else
        {
            aliadoObjetivoId.Value = ulong.MaxValue;
        }
    }

    // ──────────────────────────────────────────
    // RECIBIDORES (Llamados desde Interacciones)
    // ──────────────────────────────────────────

    [Rpc(SendTo.Server)]
    public void IntentarAvanzarMisionServerRpc(TipoPasoMision accionFisica, string idObjetivoIdentificador)
    {
        if (string.IsNullOrEmpty(misionActivaId.Value.ToString())) return;

        QuestData qData = Resources.Load<QuestData>("Quests/" + misionActivaId.Value.ToString());
        if (qData == null || pasoActualIndex.Value >= qData.pasos.Count) return;

        PasoMision pasoValido = qData.pasos[pasoActualIndex.Value];

        // 1. ¿El tipo de acción o recolección coincide con lo que pide el paso?
        if (pasoValido.tipoPaso != accionFisica) return;
        
        // 2. ¿El ID del objeto al que le diste a [E] coincide con lo que se pide? 
        // Ej: recogiste "Manzana" y el paso pedía "Manzana".
        if (pasoValido.idObjetivo != idObjetivoIdentificador) return;

        // 3. ¡Es válido! Sumamos progreso.
        progresoPasoActual.Value++;
        Debug.Log($"[Servidor] Jugador {OwnerClientId} avanzó misión. Progreso: {progresoPasoActual.Value}/{pasoValido.cantidadRequerida}");

        // ¿Pasamos al siguiente nivel?
        if (progresoPasoActual.Value >= pasoValido.cantidadRequerida)
        {
            pasoActualIndex.Value++;
            progresoPasoActual.Value = 0;

            if (pasoActualIndex.Value >= qData.pasos.Count)
            {
                CompletarMisionExitosamente(qData);
            }
            else
            {
                // Preparamos el siguiente paso (por si toca asignar compañero co-op)
                PrepararPasoServidor(qData.pasos[pasoActualIndex.Value]);
            }
        }
    }

    // [NUEVO] MECÁNICA COOPERATIVA: Ayudar a un Aliado manteniendo [E] cerca de él
    [Rpc(SendTo.Server)]
    public void IntentarAyudarAliadoServerRpc()
    {
        if (string.IsNullOrEmpty(misionActivaId.Value.ToString())) return;

        QuestData qData = Resources.Load<QuestData>("Quests/" + misionActivaId.Value.ToString());
        if (qData == null || pasoActualIndex.Value >= qData.pasos.Count) return;

        PasoMision pasoValido = qData.pasos[pasoActualIndex.Value];

        if (pasoValido.tipoPaso == TipoPasoMision.AyudarAldeano && aliadoObjetivoId.Value != ulong.MaxValue)
        {
            // Verificar Distancia Física usando red
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(aliadoObjetivoId.Value, out var aliadoClient))
            {
                float distancia = Vector3.Distance(transform.position, aliadoClient.PlayerObject.transform.position);
                if (distancia <= 3.0f) // Radio de 3 metros
                {
                    // Lógica para decir que se mantuvo el botón con éxito (simplificado a sumarle todo el progreso)
                    progresoPasoActual.Value = pasoValido.cantidadRequerida; // Instantáneo para el prototipo
                    pasoActualIndex.Value++;
                    progresoPasoActual.Value = 0;

                    if (pasoActualIndex.Value >= qData.pasos.Count)
                    {
                        CompletarMisionExitosamente(qData);
                    }
                    else
                    {
                        PrepararPasoServidor(qData.pasos[pasoActualIndex.Value]);
                    }
                }
                else
                {
                    Debug.Log($"[Servidor] Jugador {OwnerClientId} intentó ayudar, pero estaba demasiado lejos ({distancia}m).");
                }
            }
        }
    }

    private void CompletarMisionExitosamente(QuestData qData)
    {
        Debug.Log($"[Servidor] ¡Jugador {OwnerClientId} ha COMPLETAOD la misión '{qData.nombreMision}'!");

        PlayerInventory inv = GetComponent<PlayerInventory>();
        if (inv != null)
        {
            if (qData.monedasRecompensaFinal > 0)
                inv.monedas.Value += qData.monedasRecompensaFinal;

            if (qData.objetoRecompensaFinal != TipoObjeto.Ninguno)
                inv.objetoEnMano.Value = qData.objetoRecompensaFinal;
        }

        // Limpiar
        misionActivaId.Value = "";
        pasoActualIndex.Value = 0;
        progresoPasoActual.Value = 0;
        aliadoObjetivoId.Value = ulong.MaxValue;
        datosLocalesMision = null;
    }

    // ==========================================
    // SECCIÓN CLIENTE (UI y Respuestas)
    // ==========================================

    private void AlCambiarMision(FixedString32Bytes viejo, FixedString32Bytes nuevo)
    {
        if (nuevo.ToString() != "") CargarMisionLocal(nuevo.ToString());
        else { datosLocalesMision = null; ActualizarUILocal(); }
    }

    private void AlCambiarPaso(int viejo, int nuevo) => ActualizarUILocal();
    private void AlCambiarProgreso(int viejo, int nuevo) => ActualizarUILocal();

    private void ActualizarUILocal()
    {
        if (!IsOwner) return;

        GameplayUI ui = FindFirstObjectByType<GameplayUI>();
        if (ui == null) return;

        if (datosLocalesMision != null && pasoActualIndex.Value < datosLocalesMision.pasos.Count)
        {
            PasoMision pasoDeAhora = datosLocalesMision.pasos[pasoActualIndex.Value];
            
            string textoExtra = "";
            if (pasoDeAhora.tipoPaso == TipoPasoMision.AyudarAldeano)
            {
                textoExtra = $" <color=yellow>(Ve al Jugador {aliadoObjetivoId.Value})</color>";
            }

            ui.MostrarMensajeTarea($"Misión: {datosLocalesMision.nombreMision}\n➔ {pasoDeAhora.descripcionPaso}{textoExtra} ({progresoPasoActual.Value}/{pasoDeAhora.cantidadRequerida})", 3f);
        }
    }
}
