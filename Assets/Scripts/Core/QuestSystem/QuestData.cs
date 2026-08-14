using System.Collections.Generic;
using UnityEngine;
using Core.Enums;

namespace Core.QuestSystem
{
    [System.Serializable]
    public class QuestStep
    {
        [Header("Requisitos del Paso")]
        public MaterialType materialRequerido = MaterialType.AceroSierra;
        public ZoneID zonaRequerida = ZoneID.Desconocida;

        [Tooltip("Texto opcional para la interfaz (ej. 'Busca hierbas en el bosque')")]
        public string descripcion;

        [Tooltip("ID de la animación específica de este paso (ej: 1 para agacharse, 8 para regar). Si es 0, usará la del interactuable.")]
        public int animationInteractionType = 0;

        [Tooltip("Duración en segundos para completar este paso específico. Si es 0 o menor, usará la de la misión o interactuable.")]
        public float interactionDuration = 0f;

        /// <summary>Devuelve el tipo de animación de este paso, con fallback al QuestData padre.</summary>
        public int GetAnimType(QuestData parent) =>
            animationInteractionType != 0 ? animationInteractionType : (parent != null ? parent.animationInteractionType : 0);

        /// <summary>Devuelve la duración de este paso, con fallback al QuestData padre y luego al valor por defecto.</summary>
        public float GetDuration(QuestData parent, float defaultDuration = 2f) =>
            interactionDuration > 0f ? interactionDuration : (parent != null && parent.interactionDuration > 0f ? parent.interactionDuration : defaultDuration);
    }

    [CreateAssetMenu(fileName = "NewQuestData", menuName = "LoboGame/Modular Quest Data", order = 1)]
    public class QuestData : ScriptableObject
    {
        [Header("Información de la Misión")]
        [Tooltip("ID único para red y guardado (ej. 'mision_espada')")]
        public string questID;
        public string nombreMision = "Nueva Misión";
        
        [TextArea]
        public string descripcionMision = "Resumen de lo que el jugador debe crear o conseguir.";

        [Header("Proceso (Pasos secuenciales)")]
        [Tooltip("Deben ser exactamente los pasos necesarios para fabricar/obtener el objeto (usualmente 3)")]
        public List<QuestStep> pasos = new List<QuestStep>();

        [Header("Recompensas Finales")]
        [Tooltip("El objeto real que se le dará al jugador al terminar todos los pasos")]
        public ItemData objetoRecompensa;
        
        [Tooltip("Cantidad de monedas de oro que recibirá el jugador")]
        public int oroRecompensa;

        [Header("Tipo de Misión")]
        [Tooltip("¿Es una misión básica/diaria para conseguir oro?")]
        public bool esMisionBasica = false;

        [Header("Configuración de Interacción Modular")]
        [Tooltip("ID de la animación en el Animator (ej: 9 para regar, 2 para talar).")]
        public int animationInteractionType;
        [Tooltip("Nombre exacto de la herramienta en PlayerToolVisuals (ej: 'regadera', 'axe', 'escoba').")]
        public string toolVisualName;
        [Tooltip("Tiempo que el jugador estará bloqueado realizando la tarea.")]
        public float interactionDuration = 3f;
    }
}
