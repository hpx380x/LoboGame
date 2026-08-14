using UnityEngine;

namespace Core.QuestSystem
{
    /// <summary>
    /// Se coloca en cada objeto "vela" de la escena.
    /// Gestiona su propio sistema de partículas de fuego:
    /// - Lo apaga al inicio (antes de que el jugador tenga la misión).
    /// - Lo enciende de forma permanente cuando se llama a Encender().
    /// CLIENT-SIDE: Sin lógica de red. El fuego es visual local.
    /// </summary>
    public class VelaController : MonoBehaviour
    {
        // true = esta vela ya fue encendida (no se puede volver a apagar)
        public bool EstaEncendida { get; private set; } = false;

        private ParticleSystem[] sistemasParticulas;

        private void Awake()
        {
            // Recoger TODOS los sistemas de partículas del fuego (incluyendo hijos)
            sistemasParticulas = GetComponentsInChildren<ParticleSystem>(includeInactive: true);

            // Apagar el fuego al inicio — se encenderá solo si el jugador tiene la misión
            ApagarFuego();
        }

        /// <summary>
        /// Enciende esta vela permanentemente.
        /// Solo debe llamarse cuando el jugador con la misión interactúa en la zona.
        /// </summary>
        public void Encender()
        {
            if (EstaEncendida) return;
            EstaEncendida = true;

            gameObject.SetActive(true);

            foreach (var ps in sistemasParticulas)
            {
                if (ps == null) continue;
                ps.gameObject.SetActive(true);

                var main   = ps.main;
                main.loop  = true;
                main.duration = 99999f; // Forzar duración ultra larga
                main.stopAction = ParticleSystemStopAction.None;
                ps.Play(true);
            }

            Debug.Log($"[VelaController] Vela encendida: {gameObject.name}");
        }

        /// <summary>
        /// Apaga esta vela y la deja lista para ser encendida de nuevo.
        /// Se llama automáticamente cuando un jugador con la misión entra en la zona,
        /// garantizando que cada jugador encuentre las velas apagadas.
        /// [CLIENT-SIDE] Efecto visual local, sin lógica de red.
        /// </summary>
        public void Apagar()
        {
            EstaEncendida = false; // Resetear la guardia → Encender() funcionará de nuevo
            ApagarFuego();
            Debug.Log($"[VelaController] Vela apagada (reset para nuevo jugador): {gameObject.name}");
        }

        // ─── Privado ───────────────────────────────────────────────────────────
        private void ApagarFuego()
        {
            foreach (var ps in sistemasParticulas)
            {
                if (ps == null) continue;
                ps.Stop(withChildren: true, stopBehavior: ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }
        }
    }
}
