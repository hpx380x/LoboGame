using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    [Header("Configuración de Aparición - Inicio / Día")]
    [Tooltip("Arrastra aquí los objetos vacíos donde aparecerán al empezar la partida")]
    [SerializeField] private List<Transform> spawnPoints;

    [Header("Configuración de Reuniones - Asambleas")]
    [Tooltip("Arrastra aquí las sillas/puntos alrededor del objeto central para las reuniones")]
    [SerializeField] private List<Transform> votingSpawnPoints;

    // Llevamos la cuenta de qué punto le toca al siguiente jugador
    private int currentSpawnIndex = 0;
    private int currentVotingSpawnIndex = 0;

    private void Start()
    {
        // Regalo mágico: Si no has asignado pacientemente 10 sillas en el Inspector, 
        // el código las construye automáticamente alrededor de tu coordenada del Obelisco
        if (votingSpawnPoints == null || votingSpawnPoints.Count == 0)
        {
            votingSpawnPoints = new List<Transform>();
            Vector3 centroObelisco = new Vector3(107.305176f, -0.0100001693f, 485.22403f); // Las coordenadas que me pasaste
            
            for(int i = 0; i < 10; i++)
            {
                GameObject sillaVirtual = new GameObject($"Silla_Virtual_{i}");
                sillaVirtual.transform.SetParent(this.transform);
                
                // Calculamos 36 grados de espacio entre silla y silla
                float angulo = i * (360f / 10f);
                float radio = 4f; // Separación del centro (4 metros)
                
                sillaVirtual.transform.position = centroObelisco + new Vector3(Mathf.Sin(angulo * Mathf.Deg2Rad) * radio, 0, Mathf.Cos(angulo * Mathf.Deg2Rad) * radio);
                sillaVirtual.transform.LookAt(centroObelisco);
                
                votingSpawnPoints.Add(sillaVirtual.transform);
            }
            Debug.Log("<color=cyan>[SpawnManager]</color> Se han auto-creado 10 sillas invisibles alrededor del Obelisco.");
        }
    }

    // Función pública para que el GameManager nos pida la siguiente silla vacía
    public Transform GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("SpawnManager: No has asignado ningún Spawn Point en el Inspector.");
            return null;
        }

        Transform targetSpawn = spawnPoints[currentSpawnIndex];
        currentSpawnIndex = (currentSpawnIndex + 1) % spawnPoints.Count; // Ciclo
        return targetSpawn;
    }

    // [NUEVA FUNCIÓN] El GameManager llamará a esta para llevarlos a la mesa redonda.
    public Transform GetNextVotingSpawnPoint()
    {
        // Si fallara nuestra rutina mágica de sillas (no debería), devuelve el punto normal
        if (votingSpawnPoints == null || votingSpawnPoints.Count == 0)
        {
            return GetNextSpawnPoint();
        }

        Transform targetSpawn = votingSpawnPoints[currentVotingSpawnIndex];
        currentVotingSpawnIndex = (currentVotingSpawnIndex + 1) % votingSpawnPoints.Count; // Ciclo
        return targetSpawn;
    }
}
