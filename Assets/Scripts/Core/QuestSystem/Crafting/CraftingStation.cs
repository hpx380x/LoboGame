using Unity.Netcode;
using UnityEngine;
using Core.Enums;
using UnityEngine.InputSystem;

/// <summary>
/// Permite craftear el objeto legendario (Daga) combinando materiales,
/// siempre y cuando un Herrero esté cerca de la forja asociada.
/// </summary>
public class CraftingStation : NetworkBehaviour
{
    [Header("Referencias")]
    [Tooltip("La forja que debe estar activa (con humo) para poder trabajar.")]
    public ForgeController forge;
    
    [Tooltip("ID que debe coincidir con el paso 'CrafteoSocial' en QuestData.")]
    public string idMisionForja = "ForjarDaga"; 
    
    [Header("Receta (Costs)")]
    public int lingotesRequired = 5;
    public int piedrasRequired = 1;
    public TipoObjeto itemResultado = TipoObjeto.DagaCazador;

    private bool jugadorLocalCerca = false;
    private PlayerInventory invLocal;
    private PlayerQuestTracker pqtLocal;

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        
        NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsOwner)
        {
            invLocal = netObj.GetComponent<PlayerInventory>();
            pqtLocal = netObj.GetComponent<PlayerQuestTracker>();
            jugadorLocalCerca = true;
            ActualizarPrompt();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.isTrigger) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsOwner)
        {
            jugadorLocalCerca = false;
            invLocal = null;
            pqtLocal = null;
            GameplayUI ui = FindFirstObjectByType<GameplayUI>();
            if (ui != null) ui.MostrarMensajeTarea("", 0f);
        }
    }

    private void ActualizarPrompt()
    {
        if (!jugadorLocalCerca) return;
        
        GameplayUI ui = FindFirstObjectByType<GameplayUI>();
        if (ui == null) return;

        if (forge != null && forge.forjaActiva.Value)
        {
             // ¿Tiene materiales?
             if (invLocal != null && invLocal.materiales.Value.lingotes >= lingotesRequired)
             {
                 ui.MostrarMensajeTarea($"[E] Forjar {itemResultado} con {lingotesRequired} Lingotes", 0f);
             }
             else
             {
                 ui.MostrarMensajeTarea($"<color=orange>Necesitas {lingotesRequired} Lingotes para forjar.</color>", 0f);
             }
        }
        else
        {
            ui.MostrarMensajeTarea("<color=red>La forja está apagada. El Herrero debe estar aquí.</color>", 0f);
        }
    }

    private void Update()
    {
        if (jugadorLocalCerca && invLocal != null && Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                // Solo permitimos el intento si la forja está activa según la red
                if (forge != null && forge.forjaActiva.Value)
                {
                    SolicitarForjadoServerRpc(invLocal.OwnerClientId);
                }
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SolicitarForjadoServerRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            PlayerInventory inv = client.PlayerObject.GetComponent<PlayerInventory>();
            PlayerQuestTracker tracker = client.PlayerObject.GetComponent<PlayerQuestTracker>();
            
            if (inv != null && tracker != null)
            {
                // Validación final en el SERVIDOR (Seguridad)
                if (inv.materiales.Value.lingotes >= lingotesRequired && 
                    inv.materiales.Value.piedrasMagicas >= piedrasRequired)
                {
                    // Restar materiales
                    PlayerInventory.MaterialesMision actual = inv.materiales.Value;
                    actual.lingotes -= lingotesRequired;
                    actual.piedrasMagicas -= piedrasRequired;
                    inv.materiales.Value = actual;

                    // Otorgar el objeto físico
                    inv.objetoEnMano.Value = itemResultado;

                    // Avanzar la misión de crafteo
                    tracker.IntentarAvanzarMisionServerRpc(TipoPasoMision.CrafteoSocial, idMisionForja);
                    
                    Debug.Log($"[Servidor] Jugador {clientId} ha forjado {itemResultado} correctamente.");
                }
                else
                {
                    Debug.Log($"[Servidor] Intento de forja fallido por falta de materiales del Jugador {clientId}.");
                }
            }
        }
    }
}
