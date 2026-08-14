using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Core.Enums;
using Core.Environment;

/// <summary>
/// Gestiona el spawn aleatorio de objetos recolectables dentro de un volumen.
/// </summary>
public class SpawnAreaManager : NetworkBehaviour
{
    [Header("Configuración de Spawn")]
    [Tooltip("Prefab con NetworkObject y QuestInteractable.")]
    public GameObject prefabARecolectar;
    
    [Tooltip("Cuántos objetos aparecerán en esta zona.")]
    public int cantidadASpawnear = 5;
    
    [Tooltip("Tamaño del área (Ancho, Alto, Largo).")]
    public Vector3 areaSize = new Vector3(10, 2, 10);
    
    [Header("Identificadores Misión Modular")]
    public MaterialType materialARecolectar = MaterialType.AceroSierra;
    public ZoneID zonaSpawn = ZoneID.Aserradero;

    private List<NetworkObject> spawnedObjects = new List<NetworkObject>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnObjects();
        }
    }

    [ContextMenu("Spawn Objects Now")]
    public void SpawnObjects()
    {
        if (!IsServer || prefabARecolectar == null) return;

        // Limpiar si hubiera objetos antiguos (opcional)
        for (int i = 0; i < cantidadASpawnear; i++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
                Random.Range(0, areaSize.y), // Un poco de margen vertical
                Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
            );

            Vector3 worldPos = transform.position + randomOffset;

            // Intentar pegar al suelo (Raycast) para que no floten
            if (Physics.Raycast(worldPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f))
            {
                worldPos = hit.point;
            }

            GameObject instance = Instantiate(prefabARecolectar, worldPos, Quaternion.identity);
            NetworkObject netObj = instance.GetComponent<NetworkObject>();
            
            if (netObj != null)
            {
                netObj.Spawn();
                spawnedObjects.Add(netObj);
                
                // Sincronizamos las propiedades del interactuable con la misión
                QuestInteractable qi = instance.GetComponent<QuestInteractable>();
                if (qi != null)
                {
                    qi.materialAsignado = materialARecolectar;
                    qi.zonaUbicacion = zonaSpawn;
                }
            }
        }
        
        Debug.Log($"[Servidor] Aparecieron {cantidadASpawnear} objetos de tipo '{materialARecolectar}' en {name}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawCube(transform.position + Vector3.up * (areaSize.y / 2f), areaSize);
    }
}
