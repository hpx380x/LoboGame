using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using Core.Enums;
using System;

namespace Core.QuestSystem
{
    public class PlayerQuestTracker : NetworkBehaviour
    {
        [Header("Misión Actual (Server-Auth)")]
        public NetworkVariable<FixedString32Bytes> currentQuestID = new NetworkVariable<FixedString32Bytes>(
            "",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> currentStepIndex = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        [Header("Misiones Diarias (Max 2)")]
        public NetworkVariable<int> misionesCompletadasHoy = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // Registro de misiones completadas hoy para bloquear re-interacción
        private readonly System.Collections.Generic.List<string> _misionesCompletadas = new System.Collections.Generic.List<string>();

        public bool EstaMisionCompletada(string questID)
        {
            if (string.IsNullOrEmpty(questID)) return false;
            return _misionesCompletadas.Contains(questID);
        }

        public void ResetDailyQuests()
        {
            if (!IsServer) return;
            misionesCompletadasHoy.Value = 0;
            _misionesCompletadas.Clear();
            ResetDailyQuestsClientRpc();

            // Resetear el progreso de todos los interactuables de la escena al cambiar de día
            var interactables = UnityEngine.Object.FindObjectsByType<Core.Environment.UniversalQuestInteractable>(FindObjectsSortMode.None);
            foreach (var interactable in interactables)
            {
                if (interactable != null) interactable.ResetearMisionDiaria();
            }
        }

        [ClientRpc]
        private void ResetDailyQuestsClientRpc()
        {
            _misionesCompletadas.Clear();
        }

        [ClientRpc]
        private void DesbloquearMisionClientRpc(string questID)
        {
            if (!string.IsNullOrEmpty(questID))
            {
                _misionesCompletadas.Remove(questID);
            }
        }

        // Eventos locales para la UI
        public Action<QuestData> OnQuestUpdated;
        public Action OnQuestCompleted;

        private QuestData _localQuestData;

        // Máxima distancia permitida para prevenir hacks
        private const float MAX_INTERACTION_DISTANCE = 4.5f;

        public override void OnNetworkSpawn()
        {
            currentQuestID.OnValueChanged += HandleQuestChanged;
            currentStepIndex.OnValueChanged += HandleStepChanged;

            if (!string.IsNullOrEmpty(currentQuestID.Value.ToString().TrimEnd('\0')))
            {
                LoadLocalQuest(currentQuestID.Value.ToString().TrimEnd('\0'));
            }

            if (IsOwner)
            {
                GameplayUI ui = FindAnyObjectByType<GameplayUI>();
                if (ui != null)
                {
                    ui.VincularJugadorLocal(gameObject);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            currentQuestID.OnValueChanged -= HandleQuestChanged;
            currentStepIndex.OnValueChanged -= HandleStepChanged;
        }

        private void HandleQuestChanged(FixedString32Bytes previous, FixedString32Bytes current)
        {
            LoadLocalQuest(current.ToString().TrimEnd('\0'));
        }

        private void HandleStepChanged(int previous, int current)
        {
            if (_localQuestData != null)
            {
                OnQuestUpdated?.Invoke(_localQuestData);
            }
        }

        private void LoadLocalQuest(string questID)
        {
            if (string.IsNullOrEmpty(questID))
            {
                _localQuestData = null;
                OnQuestUpdated?.Invoke(null);
                return;
            }

            _localQuestData = QuestManager.Instance?.GetQuestByID(questID);
            OnQuestUpdated?.Invoke(_localQuestData);
        }

        /// <summary>
        /// Fuerza a re-emitir el estado actual de la misión hacia la UI local.
        /// Útil cuando la UI se suscribe DESPUÉS de que el evento ya ocurrió.
        /// </summary>
        public void ForzarActualizacionUI()
        {
            string questActual = currentQuestID.Value.ToString().TrimEnd('\0');
            _localQuestData = string.IsNullOrEmpty(questActual)
                ? null
                : QuestManager.Instance?.GetQuestByID(questActual);
            OnQuestUpdated?.Invoke(_localQuestData);
        }

        // -------------------------------------------------------------
        // LOGICA DEL SERVIDOR (SERVER-AUTHORITATIVE)
        // -------------------------------------------------------------

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void AcceptQuestServerRpc(FixedString32Bytes newQuestID)
        {
            if (!IsServer) return;

            string qid = newQuestID.ToString();
            QuestData requestedQuest = QuestManager.Instance.GetQuestByID(qid);
            if (requestedQuest != null)
            {
                currentQuestID.Value = newQuestID;
                currentStepIndex.Value = 0;

                // Si la misión estaba marcada como completada anteriormente, la removemos para permitir re-hacerla
                _misionesCompletadas.Remove(qid);
                DesbloquearMisionClientRpc(qid);

                // Resetear el progreso de los objetos de esta misión en la escena
                ResetearInteractuablesDeMision(requestedQuest);

                Debug.Log($"[Server] Jugador {OwnerClientId} aceptó la misión {newQuestID}");
            }
        }

        public void ServerAssignQuest(string newQuestID)
        {
            if (!IsServer) return;
            QuestData requestedQuest = QuestManager.Instance.GetQuestByID(newQuestID);
            if (requestedQuest != null)
            {
                currentQuestID.Value = newQuestID;
                currentStepIndex.Value = 0;

                // Si la misión estaba marcada como completada anteriormente, la removemos para permitir re-hacerla
                _misionesCompletadas.Remove(newQuestID);
                DesbloquearMisionClientRpc(newQuestID);

                // Resetear el progreso de los objetos de esta misión en la escena
                ResetearInteractuablesDeMision(requestedQuest);

                Debug.Log($"[Server] Servidor forzó asignación de misión {newQuestID} al jugador {OwnerClientId}");
                
                if (IsOwner)
                {
                    LoadLocalQuest(newQuestID);
                }
            }
        }

        private void ResetearInteractuablesDeMision(QuestData qData)
        {
            if (qData == null || qData.pasos == null || qData.pasos.Count == 0) return;
            var interactables = UnityEngine.Object.FindObjectsByType<Core.Environment.UniversalQuestInteractable>(FindObjectsSortMode.None);
            foreach (var uqi in interactables)
            {
                if (uqi == null) continue;

                bool esTarget = uqi.questData?.questID == qData.questID;
                if (!esTarget)
                {
                    foreach (var step in qData.pasos)
                    {
                        if (QuestValidator.PasoCoincide(step, uqi.materialAsignado, uqi.zonaUbicacion, uqi.materialRequeridoParaEntrega))
                        {
                            esTarget = true;
                            break;
                        }
                    }
                }

                if (esTarget)
                {
                    uqi.ResetearMisionDiaria();
                }
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void AbandonQuestServerRpc()
        {
            if (!IsServer) return;
            currentQuestID.Value = "";
            currentStepIndex.Value = 0;
            Debug.Log($"[Server] Jugador {OwnerClientId} abandonó su misión.");
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void ProcessStepServerRpc(MaterialType interactedMaterial, ZoneID interactionZone, Vector3 interactionPos)
        {
            if (!IsServer) return;

            // 1. Validar Anti-Hack de Distancia
            float distance = Vector3.Distance(transform.position, interactionPos);
            if (distance > MAX_INTERACTION_DISTANCE)
            {
                Debug.LogWarning($"[Server Anti-Cheat] Jugador {OwnerClientId} intentó interactuar desde muy lejos ({distance}m).");
                return;
            }

            // 2. Validar que tenga una misión activa
            if (string.IsNullOrEmpty(currentQuestID.Value.ToString())) return;

            QuestData qData = QuestManager.Instance.GetQuestByID(currentQuestID.Value.ToString());
            if (qData == null) return;

            // 3. Validar si ya ha terminado
            if (currentStepIndex.Value >= qData.pasos.Count) return;

            QuestStep currentStep = qData.pasos[currentStepIndex.Value];

            if ((currentStep.materialRequerido == interactedMaterial ||
                 currentStep.materialRequerido == MaterialType.Cualquiera ||
                 interactedMaterial == MaterialType.Cualquiera) &&
                currentStep.zonaRequerida == interactionZone)
            {
                // Paso Correcto
                currentStepIndex.Value++;
                Debug.Log($"[Server] Jugador {OwnerClientId} completó el paso {currentStepIndex.Value} de {qData.questID}.");

                // Comprobar si completó toda la misión
                if (currentStepIndex.Value >= qData.pasos.Count)
                {
                    CompleteQuestOnServer(qData);
                }
            }
        }

        private void CompleteQuestOnServer(QuestData qData)
        {
            Debug.Log($"[Server] Jugador {OwnerClientId} COMPLETÓ LA MISIÓN: {qData.nombreMision}");
            
            PlayerInventory inventory = GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                // 1. Otorgar Oro si tiene
                if (qData.oroRecompensa > 0)
                {
                    inventory.monedas.Value += qData.oroRecompensa;
                    Debug.Log($"[Server] Otorgando {qData.oroRecompensa} de oro al jugador {OwnerClientId}");
                }

                // 2. Otorgar Objeto Físico si tiene
                if (qData.objetoRecompensa != null && qData.objetoRecompensa.objetoAsociado != TipoObjeto.Ninguno)
                {
                    inventory.objetoEnMano.Value = qData.objetoRecompensa.objetoAsociado;
                    Debug.Log($"[Server] Otorgando objeto {qData.objetoRecompensa.objetoAsociado} al jugador {OwnerClientId}");
                }
            }

            // Registrar la ID de la misión como completada para bloquear accesos repetidos
            if (qData != null && !_misionesCompletadas.Contains(qData.questID))
            {
                _misionesCompletadas.Add(qData.questID);
                RegistrarMisionCompletadaClientRpc(qData.questID);
            }

            // Incrementar contador de misiones del día
            misionesCompletadasHoy.Value++;
            
            NotifyQuestCompletionClientRpc();

            if (misionesCompletadasHoy.Value < 2)
            {
                // Asignar inmediatamente la segunda misión
                string nextQuestId = GameManager.Instance != null ? GameManager.Instance.GetRandomBasicQuestID(qData.questID) : "";
                if (!string.IsNullOrEmpty(nextQuestId))
                {
                    ServerAssignQuest(nextQuestId);
                    Debug.Log($"[Server] Segunda misión asignada al jugador {OwnerClientId}: {nextQuestId}");
                }
                else
                {
                    currentQuestID.Value = "";
                    currentStepIndex.Value = 0;
                }
            }
            else
            {
                // Reseteamos el estado de la misión
                currentQuestID.Value = "";
                currentStepIndex.Value = 0;
            }
        }

        [ClientRpc]
        private void RegistrarMisionCompletadaClientRpc(string questID)
        {
            if (!string.IsNullOrEmpty(questID) && !_misionesCompletadas.Contains(questID))
            {
                _misionesCompletadas.Add(questID);
            }
        }

        [ClientRpc]
        private void NotifyQuestCompletionClientRpc()
        {
            OnQuestCompleted?.Invoke();
        }
    }
}
