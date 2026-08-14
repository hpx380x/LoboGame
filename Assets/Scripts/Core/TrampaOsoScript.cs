using UnityEngine;
using Unity.Netcode;

public class TrampaOsoScript : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // El servidor gestiona las trampas
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        PlayerState victima = other.GetComponentInParent<PlayerState>();
        PlayerStatusEffects statusVictima = other.GetComponentInParent<PlayerStatusEffects>();
        if (victima != null && !victima.isDead.Value && statusVictima != null)
        {
            Debug.Log($"[Trampa] ¡Un jugador ha pisado la trampa de oso!");
            
            // Stun de 10 segundos
            statusVictima.AplicarStunEnServidor(10f);
            
            // Notificar a todos los clientes para destruir la representación visual local
            PlayerItemController pic = FindAnyObjectByType<PlayerItemController>();
            if (pic != null)
            {
                pic.NotificarDestruccionTrampaClientRpc(transform.position);
            }
            
            // La trampa se destruye al usarse
            Destroy(gameObject);
        }
    }
}
