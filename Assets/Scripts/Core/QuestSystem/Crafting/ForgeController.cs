using Unity.Netcode;
using UnityEngine;
using Core.Enums;

/// <summary>
/// Gestiona el estado visual de la forja (humo/fuego).
/// Solo se activa si un jugador con el rol 'Herrero' está en las proximidades.
/// </summary>
public class ForgeController : NetworkBehaviour
{
    [Header("Visuales")]
    [Tooltip("Sistema de partículas o malla de humo que se activa al trabajar.")]
    public GameObject humoParticulas;
    
    [Tooltip("Distancia a la que el Herrero debe estar para que la forja 'funcione'.")]
    public float radioDeteccionHerrero = 7.0f;

    [Header("Estado de Red")]
    [Tooltip("Sincroniza si la forja está activa para que todos vean el humo.")]
    public NetworkVariable<bool> forjaActiva = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        // Estado inicial
        if (humoParticulas != null) humoParticulas.SetActive(forjaActiva.Value);
        
        // Suscribirse a cambios para activar/desactivar visuales en clientes
        forjaActiva.OnValueChanged += (v, n) => {
            if (humoParticulas != null) humoParticulas.SetActive(n);
        };
    }

    private void Update()
    {
        // Solo el servidor calcula la lógica de proximidad
        if (!IsServer) return;

        bool herreroPresente = false;
        
        // Buscamos en la lista de clientes conectados a alguien vivo que sea Herrero
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            PlayerState ps = client.PlayerObject.GetComponent<PlayerState>();
            if (ps != null && ps.rolAldea.Value == RolAldea.Herrero && !ps.isDead.Value)
            {
                float distancia = Vector3.Distance(transform.position, ps.transform.position);
                if (distancia <= radioDeteccionHerrero)
                {
                    herreroPresente = true;
                    break;
                }
            }
        }

        // Actualizar variable de red si el estado ha cambiado
        if (forjaActiva.Value != herreroPresente)
        {
            forjaActiva.Value = herreroPresente;
            Debug.Log($"[Forja] Estado cambiado a: {(herreroPresente ? "ACTIVA" : "INACTIVA")}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, radioDeteccionHerrero);
    }
}
