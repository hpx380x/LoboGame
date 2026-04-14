using Unity.Netcode;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// Componente individual para cada vela del puzzle de secuencia.
/// Detecta la interacción del jugador y responde a las órdenes del PuzzleSequenceManager.
/// </summary>
public class PuzzleCandle : NetworkBehaviour
{
    [Header("Configuración")]
    public int indexEnPuzzle;
    public PuzzleSequenceManager manager;
    
    [Tooltip("La malla de la llama o luz que se enciende y apaga.")]
    public GameObject luzVisual; 
    
    [Tooltip("Texto que aparece al estar cerca.")]
    public string mensajePrompt = "[E] Encender Vela";

    // ¿El jugador tiene permitido pulsar esta vela ahora? (Controlado por el Servidor)
    public NetworkVariable<bool> puedeInteractuar = new NetworkVariable<bool>(false);

    private bool jugadorCerca = false;
    private ulong localClientId;

    public override void OnNetworkSpawn()
    {
        // Asegurarnos de que las velas empiecen apagadas
        if (luzVisual != null) luzVisual.SetActive(false);
        
        // Suscribir al cambio de interactuabilidad para enfriar/calentar la UI
        puedeInteractuar.OnValueChanged += (vIE, nIE) => { if(jugadorCerca) ActualizarPrompt(); };
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        
        NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsOwner)
        {
            jugadorCerca = true;
            localClientId = netObj.OwnerClientId;
            ActualizarPrompt();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.isTrigger) return;
        
        NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsOwner)
        {
            jugadorCerca = false;
            GameplayUI ui = FindFirstObjectByType<GameplayUI>();
            if (ui != null) ui.MostrarMensajeTarea("", 0f);
        }
    }

    private void ActualizarPrompt()
    {
        GameplayUI ui = FindFirstObjectByType<GameplayUI>();
        if (ui != null)
        {
            if (puedeInteractuar.Value) 
                ui.MostrarMensajeTarea(mensajePrompt, 0f);
            else 
                ui.MostrarMensajeTarea("<color=gray>Observa la secuencia...</color>", 0f);
        }
    }

    private void Update()
    {
        // Solo el dueño del personaje que está cerca puede intentar pulsar
        if (jugadorCerca && puedeInteractuar.Value)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                PulsarVelaServerRpc(localClientId);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PulsarVelaServerRpc(ulong clientId)
    {
        if (puedeInteractuar.Value && manager != null)
        {
            manager.RecibirInputVela(indexEnPuzzle, clientId);
        }
    }

    /// <summary>Enciende o apaga la vela de forma persistente (Sincronizado vía RPC).</summary>
    [ClientRpc]
    public void EncenderClientRpc(bool estado)
    {
        if (luzVisual != null) luzVisual.SetActive(estado);
    }

    /// <summary>Hace un destello rápido para feedback visual de pulsación.</summary>
    [ClientRpc]
    public void FlashLocalClientRpc()
    {
        StartCoroutine(FlashRutina());
    }

    private IEnumerator FlashRutina()
    {
        if (luzVisual != null)
        {
            luzVisual.SetActive(true);
            yield return new WaitForSeconds(0.4f);
            // Solo la apagamos si el puzzle no está mostrando una secuencia de encendido largo
            if (manager != null && !puedeInteractuar.Value) 
                luzVisual.SetActive(false);
            else if (puedeInteractuar.Value)
                luzVisual.SetActive(false);
        }
    }
}
