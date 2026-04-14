using UnityEngine;
using Unity.Netcode;

public class TrampaOsoScript : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // El servidor gestiona las trampas
        if (!NetworkManager.Singleton.IsServer) return;

        PlayerState victima = other.GetComponentInParent<PlayerState>();
        PlayerStatusEffects statusVictima = other.GetComponentInParent<PlayerStatusEffects>();
        if (victima != null && !victima.isDead.Value && statusVictima != null)
        {
            Debug.Log($"<color=red>🐻 [Trampa] ¡Un jugador ha pisado la trampa de oso!</color>");
            
            // Stun de 10 segundos
            statusVictima.AplicarStunEnServidor(10f);
            
            // Reproduciríamos un sonido global o efecto aquí si hubiera
            
            // La trampa se destruye al usarse
            Destroy(gameObject);
        }
    }
}
