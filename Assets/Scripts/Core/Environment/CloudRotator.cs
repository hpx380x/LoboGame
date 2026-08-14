using UnityEngine;

namespace Core.Environment
{
    /// <summary>
    /// Componente para simular el movimiento circular y suave de nubes (SM_Generic_CloudRing_01).
    /// Opcionalmente añade un leve movimiento flotante vertical para mayor realismo.
    /// </summary>
    public class CloudRotator : MonoBehaviour
    {
        [Header("Rotación")]
        [Tooltip("Velocidad de rotación en grados por segundo.")]
        public float velocidadRotacion = 5f;

        [Tooltip("Eje de rotación (por defecto es hacia arriba, eje Y).")]
        public Vector3 ejeRotacion = Vector3.up;

        [Header("Efecto de Flotación (Opcional)")]
        [Tooltip("¿Activar una leve fluctuación vertical de arriba a abajo?")]
        public bool flotar = true;

        [Tooltip("Velocidad de la flotación vertical.")]
        public float velocidadFlotacion = 1f;

        [Tooltip("Amplitud o rango del movimiento de flotación vertical.")]
        public float amplitudFlotacion = 0.2f;

        private Vector3 posicionInicial;

        private void Start()
        {
            posicionInicial = transform.localPosition;
        }

        private void Update()
        {
            // 1. Rotación continua alrededor del eje
            transform.Rotate(ejeRotacion * (velocidadRotacion * Time.deltaTime));

            // 2. Movimiento vertical sinusoidal suave para que flote como una nube real
            if (flotar)
            {
                float nuevoY = posicionInicial.y + Mathf.Sin(Time.time * velocidadFlotacion) * amplitudFlotacion;
                transform.localPosition = new Vector3(transform.localPosition.x, nuevoY, transform.localPosition.z);
            }
        }
    }
}
