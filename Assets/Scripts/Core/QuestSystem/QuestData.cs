using System.Collections.Generic;
using UnityEngine;
using Core.Enums;

// Definimos el tipo de acción que requiere este paso
public enum TipoPasoMision
{
    Recolectar,     // Obtener X cantidad de un objeto tirado en el suelo
    Entregar,       // Llevar los objetos a un NPC o zona específica
    Interactuar,    // Pulsar o mantener E en una estación específica (minijuego)
    AyudarAldeano,  // Acercarse a otro jugador y mantener pulsado E para ayudarle
    MinijuegoPantalla,  // [NUEVO] Abrir HUD de minijuego (Cables)
    PuzzleSecuencia,    // [NUEVO] Resolver secuencia de velas (Simon Says)
    CrafteoSocial       // [NUEVO] Interactuar con el Herrero/Oficio para crear objeto
}

public enum RarezaObjeto
{
    Normal,
    Especial,
    Legendario
}

[System.Serializable]
public class PasoMision
{
    [Header("Configuración del Paso")]
    public TipoPasoMision tipoPaso = TipoPasoMision.Recolectar;
    
    [Tooltip("El identificador u objeto requerido para este paso. Ej: 'Manzana', o el ID del NPC.")]
    public string idObjetivo = "ObjetoGenerico";

    [Tooltip("Cantidad necesaria. Ej: Si es recolectar, 5 manzanas. Si es mantener E, 5 segundos.")]
    public int cantidadRequerida = 1;

    [Tooltip("Instrucción para el jugador (mostrar en UI). Ej: 'Recoge 5 manzanas'.")]
    public string descripcionPaso = "Realiza esta tarea";
}

[CreateAssetMenu(fileName = "NewQuest", menuName = "LoboGame/Quest Data", order = 1)]
public class QuestData : ScriptableObject
{
    [Header("Info Principal")]
    public string nombreMision = "Nueva Misión";
    [TextArea]
    public string descripcionMision = "Descripción completa de la historia o motivo.";

    [Header("Pasos de la Misión")]
    [Tooltip("La cadena de tareas que hay que completar en orden.")]
    public List<PasoMision> pasos = new List<PasoMision>();

    [Header("Recompensas Finales")]
    [Tooltip("Objeto final otorgado al jugador cuando completa el último paso.")]
    public TipoObjeto objetoRecompensaFinal = TipoObjeto.Ninguno;
    [Tooltip("Monedas otorgadas al finalizar la cadena.")]
    public int monedasRecompensaFinal = 0;
}
