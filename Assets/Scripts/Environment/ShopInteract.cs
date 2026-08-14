using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Core.Enums;

/// <summary>
/// Módulo de Economía Híbrida: La tienda del Mercader (Ruta Segura).
/// </summary>
public class ShopInteract : MonoBehaviour
{
    [Header("Configuración de la Tienda")]
    [Tooltip("El objeto que vende este puesto/cofre.")]
    public TipoObjeto objetoVendido = TipoObjeto.PocionVelocidad;
    [Tooltip("Precio en Monedas/Oro.")]
    public int costeMonedas = 2;

    [Header("UI Visual")]
    [Tooltip("Nombre bonito del objeto para enseñar en pantalla (Ej: Poción de Velocidad)")]
    public string nombreMostrado = "Poción";

    private bool jugadorCerca = false;
    private PlayerState psLocal;

    // ── Caché: se resuelve una sola vez en Start ──────────────────────
    private GameplayUI cachedUI;

    private void Start()
    {
        cachedUI = Object.FindAnyObjectByType<GameplayUI>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        psLocal = netObj.GetComponent<PlayerState>();
        if (psLocal == null || psLocal.isDead.Value) return;

        jugadorCerca = true;

        if (cachedUI != null)
        {
            PlayerInventory inv = psLocal.GetComponent<PlayerInventory>();
            if (inv != null && inv.monedas.Value >= costeMonedas)
                cachedUI.MostrarMensajeTarea($"[E] Comprar {nombreMostrado} ({costeMonedas} Oro)", 0f);
            else
                cachedUI.MostrarMensajeTarea($"Necesitas {costeMonedas} Oro para {nombreMostrado}.", 0f);
        }
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
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            PlayerInventory inv = psLocal.GetComponent<PlayerInventory>();
            if (inv != null && inv.monedas.Value >= costeMonedas)
            {
                inv.ComprarObjetoServerRpc(objetoVendido, costeMonedas);
                if (cachedUI != null) cachedUI.MostrarMensajeTarea($"¡Compraste {nombreMostrado}!", 2f);
                jugadorCerca = false;
            }
            else
            {
                if (cachedUI != null) cachedUI.MostrarMensajeTarea($"<color=red>No tienes suficiente Oro.</color>", 2f);
            }
        }
    }
}
