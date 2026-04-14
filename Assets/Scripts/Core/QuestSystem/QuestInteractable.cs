using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class QuestInteractable : NetworkBehaviour
{
    [Header("Identificación")]
    [Tooltip("El ID que la misión pedirá. Ej: 'Manzana' o 'Comerciante'")]
    public string idObjetivoIdentificador = "ObjetoGenerico";

    [Tooltip("Qué acción representa este objeto de cara a la misión.")]
    public TipoPasoMision accionFisica = TipoPasoMision.Recolectar;

    [Tooltip("¿Es un objeto común o Legendario?")]
    public RarezaObjeto rareza = RarezaObjeto.Normal;

    [Tooltip("Si es true, se destruirá (Despawn) al ser recogido.")]
    public bool destruirAlRecoger = true;

    [Header("Configuración UI")]
    public string textoInteraccion = "[E] Interactuar";

    private bool jugadorLocalCerca = false;
    private PlayerQuestTracker trackerLocal;

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        
        NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsOwner)
        {
            trackerLocal = netObj.GetComponent<PlayerQuestTracker>();
            if (trackerLocal != null)
            {
                jugadorLocalCerca = true;
                GameplayUI ui = FindFirstObjectByType<GameplayUI>();
                if (ui != null) 
                {
                    string extraText = rareza == RarezaObjeto.Legendario ? "<color=orange>[Legendario]</color> " : "";
                    ui.MostrarMensajeTarea(extraText + textoInteraccion, 0f); // Se queda fijo hasta salir
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.isTrigger) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsOwner)
        {
             if (trackerLocal != null)
             {
                 jugadorLocalCerca = false;
                 trackerLocal = null;
                 GameplayUI ui = FindFirstObjectByType<GameplayUI>();
                 if (ui != null) ui.MostrarMensajeTarea("", 0f);
             }
        }
    }

    private void Update()
    {
        if (jugadorLocalCerca && trackerLocal != null)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                trackerLocal.IntentarAvanzarMisionServerRpc(accionFisica, idObjetivoIdentificador);
                
                if (destruirAlRecoger)
                {
                    DespawnObjetoServerRpc();
                }
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DespawnObjetoServerRpc()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
    }
}
