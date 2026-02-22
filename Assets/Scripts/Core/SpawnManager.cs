using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    [Header("Configuración de Aparición")]
    [Tooltip("Arrastra aquí los objetos vacíos que servirán como sillas/puntos de inicio")]
    [SerializeField] private List<Transform> spawnPoints;

    // Llevamos la cuenta de qué punto le toca al siguiente jugador
    private int currentSpawnIndex = 0;

    // Función pública para que el GameManager nos pida la siguiente silla vacía
    public Transform GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("SpawnManager: No has asignado ningún Spawn Point en el Inspector.");
            return null;
        }

        Transform targetSpawn = spawnPoints[currentSpawnIndex];
        currentSpawnIndex = (currentSpawnIndex + 1) % spawnPoints.Count;
        return targetSpawn;
    }
}
