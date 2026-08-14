using Core.Enums;

namespace Core.QuestSystem
{
    /// <summary>
    /// Clase estatica de validacion de misiones.
    /// Centraliza la logica de matching de pasos (material + zona) que antes estaba
    /// copiada en OnTriggerEnter, EsPasoValidoDeMisionActual y PlayerQuestTracker.
    /// </summary>
    public static class QuestValidator
    {
        /// <summary>
        /// Comprueba si un paso de mision coincide con el material y zona de un interactuable.
        /// Permite coincidencias neutras (Ninguno/Desconocida/Cualquiera).
        /// </summary>
        public static bool PasoCoincide(
            QuestStep step,
            MaterialType materialObjeto,
            ZoneID zonaObjeto,
            MaterialType materialEntrega = MaterialType.Ninguno)
        {
            bool materialCoincide =
                step.materialRequerido == materialObjeto ||
                step.materialRequerido == MaterialType.Cualquiera ||
                materialObjeto         == MaterialType.Cualquiera ||
                step.materialRequerido == MaterialType.Ninguno    ||
                materialObjeto         == MaterialType.Ninguno    ||
                (materialEntrega != MaterialType.Ninguno && step.materialRequerido == materialEntrega);

            bool zonaCoincide =
                step.zonaRequerida == zonaObjeto         ||
                step.zonaRequerida == ZoneID.Desconocida ||
                zonaObjeto         == ZoneID.Desconocida;

            return materialCoincide && zonaCoincide;
        }

        /// <summary>
        /// Comprueba si el jugador tiene una mision activa cuyo paso actual coincide con el objeto dado.
        /// </summary>
        public static bool JugadorTieneQuestActivaParaObjeto(
            PlayerQuestTracker tracker,
            MaterialType materialObjeto,
            ZoneID zonaObjeto,
            MaterialType materialEntrega = MaterialType.Ninguno,
            string questDataID = null)
        {
            if (tracker == null) return false;

            string activeQuestId = tracker.currentQuestID.Value.ToString().TrimEnd('\0');
            if (string.IsNullOrEmpty(activeQuestId)) return false;

            var quest = QuestManager.Instance?.GetQuestByID(activeQuestId);
            if (quest == null || tracker.currentStepIndex.Value >= quest.pasos.Count) return false;

            var step = quest.pasos[tracker.currentStepIndex.Value];

            // Si el interactuable especifica un questDataID (ej. asset enlazado), debe coincidir con la misión activa
            if (!string.IsNullOrEmpty(questDataID) && questDataID != activeQuestId) return false;

            return PasoCoincide(step, materialObjeto, zonaObjeto, materialEntrega);
        }

        /// <summary>
        /// Comprueba si el jugador tiene la mision bloqueada (ya completada).
        /// </summary>
        public static bool MisionBloqueada(PlayerQuestTracker tracker, string questID)
        {
            if (tracker == null || string.IsNullOrEmpty(questID)) return false;
            return tracker.EstaMisionCompletada(questID);
        }

        /// <summary>
        /// Comprueba si un Punto de Entrega sirve para la mision activa del jugador.
        /// </summary>
        public static bool EntregaEsValidaParaQuestActiva(
            PlayerQuestTracker tracker,
            MaterialType materialEntrega,
            MaterialType materialAsignado)
        {
            if (tracker == null) return false;

            string activeQuestId = tracker.currentQuestID.Value.ToString().TrimEnd('\0');
            if (string.IsNullOrEmpty(activeQuestId)) return false;

            var quest = QuestManager.Instance?.GetQuestByID(activeQuestId);
            if (quest == null || tracker.currentStepIndex.Value >= quest.pasos.Count) return false;

            var step = quest.pasos[tracker.currentStepIndex.Value];

            return materialEntrega == MaterialType.Ninguno ||
                   step.materialRequerido == materialEntrega ||
                   step.materialRequerido == materialAsignado;
        }
    }
}
