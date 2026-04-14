using Unity.Netcode;
using UnityEngine;
using Core.Enums;

public class JobItem : NetworkBehaviour
{
    [Header("Configuración del Oficio")]
    public RolAldea rolAsociado = RolAldea.Herrero;
    public string nombreMostrable = "Martillo de Herrero";
    
    [Header("Visuales")]
    public GameObject visualMalla;

    private bool interactuable = true;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Asegurarse de que el objeto sea visible para todos
        if (visualMalla != null) visualMalla.SetActive(true);
    }

    [Rpc(SendTo.Server)]
    public void RecogerOficioServerRpc(ulong clientId)
    {
        if (!interactuable) return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            PlayerState ps = client.PlayerObject.GetComponent<PlayerState>();
            if (ps != null && !ps.isDead.Value)
            {
                // Cambiar el rol al nuevo jugador
                ps.rolAldea.Value = rolAsociado;
                
                Debug.Log($"[Servidor] El jugador {clientId} ahora es {rolAsociado} tras recoger {nombreMostrable}");
                
                // Despawnear el objeto del suelo
                interactuable = false;
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}
