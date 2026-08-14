using UnityEngine;
using Core.Enums;

namespace Core.QuestSystem
{
    [CreateAssetMenu(fileName = "NewItemData", menuName = "LoboGame/Item Data", order = 0)]
    public class ItemData : ScriptableObject
    {
        [Header("Información Básica")]
        public TipoObjeto objetoAsociado = TipoObjeto.Ninguno;
        
        [Tooltip("ID único para red y guardado (ej. 'daga_plata')")]
        public string itemID;
        public string nombreObjeto;
        
        [TextArea]
        public string descripcion;

        [Header("Visuales")]
        public Sprite iconoUI;
        [Tooltip("El modelo 3D que se instanciará en las manos del jugador")]
        public GameObject prefab3D;

        [Header("Propiedades")]
        public bool isLegendary = false;
    }
}
