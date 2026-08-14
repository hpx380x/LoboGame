using UnityEngine;
using StarterAssets;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Core.Environment
{
    /// <summary>
    /// Componente para la Zona de Votación.
    /// </summary>
    public class VotingZoneInteract : MonoBehaviour
    {
        private bool jugadorCerca = false;
        private PlayerState psLocal;
        private Collider miCollider;

        // ── Caché: se resuelven una sola vez en Start ─────────────────
        private GameplayUI cachedUI;
        private VotingUI cachedVotingUI;

        private void Awake()
        {
            miCollider = GetComponent<Collider>();
            if (miCollider == null)
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size      = new Vector3(5, 5, 5);
                miCollider    = box;
            }
            else
            {
                miCollider.isTrigger = true;
            }
        }

        private void Start()
        {
            cachedUI       = Object.FindAnyObjectByType<GameplayUI>();
            cachedVotingUI = Object.FindAnyObjectByType<VotingUI>(FindObjectsInactive.Include);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger) return;

            NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
            if (netObj == null || !netObj.IsOwner) return;

            psLocal = netObj.GetComponent<PlayerState>();
            if (psLocal == null || psLocal.isDead.Value) return;

            jugadorCerca = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.isTrigger) return;

            NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
            if (netObj == null || !netObj.IsOwner) return;

            jugadorCerca = false;
            psLocal      = null;

            if (cachedUI != null)
                cachedUI.MostrarMensajeTarea("", 0f);
        }

        private void Update()
        {
            if (!jugadorCerca || psLocal == null) return;

            if (GameManager.Instance == null || GameManager.Instance.currentPhase.Value != GamePhase.Votacion)
                return;

            bool isVotingActive = cachedVotingUI != null && cachedVotingUI.panelVotacion.activeSelf;

            if (!isVotingActive)
            {
                if (cachedUI != null)
                    cachedUI.MostrarMensajeTarea("[E] Votar", 0f);

                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    if (cachedVotingUI != null)
                    {
                        cachedVotingUI.MostrarPantallaVotacion();
                        if (cachedUI != null) cachedUI.MostrarMensajeTarea("", 0f);
                    }
                }
            }
            else
            {
                if (cachedUI != null) cachedUI.MostrarMensajeTarea("", 0f);
            }
        }
    }
}
