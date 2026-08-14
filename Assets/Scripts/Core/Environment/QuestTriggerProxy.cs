using UnityEngine;

namespace Core.Environment
{
    /// <summary>
    /// Proxy para redirigir eventos de Trigger desde un objeto padre (ej: un cubo colisionador)
    /// hacia los scripts de interacción de misiones que estén en objetos hijos.
    /// </summary>
    public class QuestTriggerProxy : MonoBehaviour
    {
        public UniversalQuestInteractable targetInteractable;
        public InteractivableMisionUniversal targetMisionUniversal;

        private void Awake()
        {
            // Auto-detectar los scripts de interacción en los hijos si no están asignados manualmente
            if (targetInteractable == null)
            {
                targetInteractable = GetComponentInChildren<UniversalQuestInteractable>();
            }
            if (targetMisionUniversal == null)
            {
                targetMisionUniversal = GetComponentInChildren<InteractivableMisionUniversal>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (targetInteractable != null)
            {
                targetInteractable.OnTriggerEnterProxy(other);
            }
            if (targetMisionUniversal != null)
            {
                targetMisionUniversal.OnTriggerEnterProxy(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (targetInteractable != null)
            {
                targetInteractable.OnTriggerExitProxy(other);
            }
            if (targetMisionUniversal != null)
            {
                targetMisionUniversal.OnTriggerExitProxy(other);
            }
        }
    }
}
