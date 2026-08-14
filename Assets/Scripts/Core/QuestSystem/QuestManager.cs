using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Core.QuestSystem
{
    // El QuestManager almacena un diccionario centralizado de todas las misiones.
    // Como NGO no puede enviar ScriptableObjects por la red (RPCs), usamos el índice
    // o el hash de la string ("questID") para decirle al servidor o cliente qué misión tenemos.
    public class QuestManager : NetworkBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Registro de Misiones")]
        [Tooltip("Arrastra aquí todas las QuestData creadas en el proyecto para que el sistema las reconozca por red.")]
        public List<QuestData> registeredQuests = new List<QuestData>();

        private Dictionary<string, QuestData> questRegistry = new Dictionary<string, QuestData>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitRegistry();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitRegistry()
        {
            foreach (var q in registeredQuests)
            {
                RegistrarMision(q);
            }

            // Carga automática de cualquier otra misión en Resources/Quests/ para evitar olvidos
            QuestData[] autoLoaded = Resources.LoadAll<QuestData>("Quests");
            foreach (var q in autoLoaded)
            {
                RegistrarMision(q);
            }

            Debug.Log($"[QuestManager] Registro inicializado con {questRegistry.Count} misiones en total.");
        }

        private void RegistrarMision(QuestData q)
        {
            if (q != null && !string.IsNullOrEmpty(q.questID))
            {
                if (!questRegistry.ContainsKey(q.questID))
                {
                    questRegistry.Add(q.questID, q);
                }
            }
        }

        // Obtener una misión por ID
        public QuestData GetQuestByID(string questID)
        {
            if (string.IsNullOrEmpty(questID)) return null;
            
            string cleanQuestID = questID.Trim(' ', '\0', '\n', '\r').ToLowerInvariant();
            
            foreach (var kvp in questRegistry)
            {
                if (kvp.Key.Trim(' ', '\0', '\n', '\r').ToLowerInvariant() == cleanQuestID)
                {
                    return kvp.Value;
                }
            }
            
            Debug.LogWarning($"[QuestManager] Misión con ID '{questID}' (limpio: '{cleanQuestID}') no encontrada en el registro.");
            return null;
        }

        public List<QuestData> GetAllAvailableQuests()
        {
            return new List<QuestData>(questRegistry.Values);
        }
    }
}
