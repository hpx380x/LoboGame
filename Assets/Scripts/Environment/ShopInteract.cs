using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Core.Enums;

/// <summary>
/// Módulo de Economía Híbrida: La tienda del Mercader (Ruta Segura).
/// Obliga a los jugadores que farmean tareas de 1 Oro a reunirse aquí
/// para gastar su dinero. Al ser un lugar predecible, el Lobo puede armar emboscadas.
/// </summary>
public class ShopInteract : MonoBehaviour
{
    [Header("Configuraci\u00f3n de la Tienda")]
    [Tooltip("El objeto que vende este puesto/cofre.")]
    public TipoObjeto objetoVendido = TipoObjeto.PocionVelocidad;
    [Tooltip("Precio en Monedas/Oro.")]
    public int costeMonedas = 2;

    [Header("UI Visual")]
    [Tooltip("Nombre bonito del objeto para enseñar en pantalla (Ej: Poción de Velocidad)")]
    public string nombreMostrado = "Poción";

    private bool jugadorCerca = false;
    private PlayerState psLocal;

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        psLocal = netObj.GetComponent<PlayerState>();
        if (psLocal == null || psLocal.isDead.Value) return;

        jugadorCerca = true;

        GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
        if (ui != null)
        {
            PlayerInventory inv = psLocal.GetComponent<PlayerInventory>();
            if (inv != null && inv.monedas.Value >= costeMonedas)
            {
                ui.MostrarMensajeTarea($"[E] Comprar {nombreMostrado} ({costeMonedas} Oro)", 0f);
            }
            else
            {
                ui.MostrarMensajeTarea($"Necesitas {costeMonedas} Oro para {nombreMostrado}.", 0f);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.isTrigger) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        jugadorCerca = false;
        psLocal = null;

        GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
        if (ui != null)
        {
            ui.MostrarMensajeTarea("", 0f);
        }
    }

    private void Update()
    {
        if (!jugadorCerca || psLocal == null) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
            PlayerInventory inv = psLocal.GetComponent<PlayerInventory>();
            if (inv != null && inv.monedas.Value >= costeMonedas)
            {
                // Enviar la petición de compra segura al servidor
                inv.ComprarObjetoServerRpc(objetoVendido, costeMonedas);
                
                if (ui != null) 
                    ui.MostrarMensajeTarea($"¡Compraste {nombreMostrado}!", 2f);
                
                jugadorCerca = false; // Desactivar temporalmente para que no spammee
            }
            else
            {
                if (ui != null) 
                    ui.MostrarMensajeTarea($"<color=red>No tienes suficiente Oro.</color>", 2f);
            }
        }
    }
}
