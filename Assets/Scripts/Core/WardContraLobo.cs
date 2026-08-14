using UnityEngine;
using Unity.Netcode;

// [Mecánica: Manzana de Oro]
public class WardContraLobo : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        // El servidor gestiona el trigger
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        // Buscamos si el objeto que acaba de entrar (o se intenta quedar) es un jugador
        PlayerState ps = other.GetComponentInParent<PlayerState>();
        
        // Verificamos si existe en la red, está vivo, y sobre todo: Si es un Lobo
        if (ps != null && ps.isWolf.Value && !ps.isDead.Value)
        {
            // Repulsión Mágica Fuerte
            // Forzamos un empuje en la dirección contraria al centro de la barrera
            Vector3 pushDirection = (ps.transform.position - transform.position).normalized;
            pushDirection.y = 0; // Evitamos lanzarlo por los aires
            
            // Moverlo hacia atrás 0.15 metros localmente en su cliente de forma autoritativa
            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { ps.OwnerClientId } }
            };
            ps.RepelerJugadorClientRpc(pushDirection * 0.15f, rpcParams);
        }
    }
}
