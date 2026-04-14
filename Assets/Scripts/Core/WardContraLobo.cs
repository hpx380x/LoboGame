using UnityEngine;

// [Mecánica: Manzana de Oro]
public class WardContraLobo : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        // Buscamos si el objeto que acaba de entrar (o se intenta quedar) es un jugador
        PlayerState ps = other.GetComponentInParent<PlayerState>();
        
        // Verificamos si existe en la red, está vivo, y sobre todo: Si es un Lobo
        if (ps != null && ps.isWolf.Value && !ps.isDead.Value)
        {
            // Repulsión Mágica Fuerte
            // Forzamos un empuje en la dirección contraria al centro de la barrera
            Vector3 pushDirection = (ps.transform.position - transform.position).normalized;
            pushDirection.y = 0; // Evitamos lanzarlo por los aires
            
            // Moverlo hacia atrás 2 metros abruptamente para simular choque de pared
            ps.transform.position += pushDirection * 0.15f; // Lo aplicamos constantemente en Stay para que sea como un muro sólido
        }
    }
}
