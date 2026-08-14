using Unity.Netcode;
using UnityEngine;
using Core.Enums;
using UnityEngine.InputSystem;
using Core.QuestSystem;
using System.Collections.Generic;

/// <summary>
/// Permite craftear el objeto legendario (Daga) combinando materiales,
/// siempre y cuando un Herrero esté cerca de la forja asociada.
/// </summary>
public class CraftingStation : NetworkBehaviour
{
    [Header("Referencias")]
    [Tooltip("La forja que debe estar activa (con humo) para poder trabajar.")]
    public ForgeController forge;
    
    [Header("Identificadores de Misión (Modular)")]
    [Tooltip("Material considerado para avanzar la misión al forjar.")]
    public MaterialType materialForjado = MaterialType.AceroSierra;
    public ZoneID zonaForja = ZoneID.Torre;
    
    [System.Serializable]
    public struct MaterialRequirement
    {
        public MaterialType material;
        public int cantidad;
    }

    [Header("Receta Modular")]
    public List<MaterialRequirement> requisitosCrafteo = new List<MaterialRequirement>();
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
            GameplayUI ui = FindAnyObjectByType<GameplayUI>();
            if (ui != null) ui.MostrarMensajeTarea("", 0f);
        }
    }

    private void ActualizarPrompt()
    {
        if (!jugadorLocalCerca) return;
        
        GameplayUI ui = FindAnyObjectByType<GameplayUI>();
        if (ui == null) return;

        if (forge != null && forge.forjaActiva.Value)
        {
             // Validamos si tiene todos los materiales de la receta
             bool tieneTodo = true;
             string listaFaltante = "";

             foreach(var req in requisitosCrafteo)
             {
                 int count = invLocal.materiales.Value.GetCount(req.material);
                 if (count < req.cantidad)
                 {
                     tieneTodo = false;
                     listaFaltante += $"{req.cantidad - count} de {req.material}, ";
                 }
             }

             if (tieneTodo)
             {
                 ui.MostrarMensajeTarea($"[E] Forjar {itemResultado}", 0f);
             }
             else
             {
                 ui.MostrarMensajeTarea($"<color=orange>Falta: {listaFaltante.TrimEnd(' ', ',')}</color>", 0f);
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
                bool puedeCraftear = true;
                foreach(var req in requisitosCrafteo)
                {
                    if (inv.materiales.Value.GetCount(req.material) < req.cantidad)
                    {
                        puedeCraftear = false;
                        break;
                    }
                }

                if (puedeCraftear)
                {
                    // Restar materiales
                    PlayerInventory.MaterialesMision actual = inv.materiales.Value;
                    foreach(var req in requisitosCrafteo)
                    {
                        int nuevoTotal = actual.GetCount(req.material) - req.cantidad;
                        actual.SetCount(req.material, nuevoTotal);
                    }
                    inv.materiales.Value = actual;

                    // Otorgar el objeto físico
                    inv.objetoEnMano.Value = itemResultado;

                    // Avanzar la misión de crafteo (Usando el nuevo sistema modular)
                    tracker.ProcessStepServerRpc(materialForjado, zonaForja, transform.position);
                    
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

