using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Instancia el personaje VISUAL (cosmético) del jugador en la escena de Lobby usando Netcode.
/// Ahora los personajes son NetworkObjects para que todos se vean entre ellos.
/// Los aldeanos de prueba se ocultan completamente al empezar.
/// </summary>
public class LobbyPlayerSpawner : NetworkBehaviour
{
    [Header("Prefab del Personaje de Lobby")]
    [Tooltip("Prefab del personaje (debe tener NetworkObject y PlayerLobbyPose).")]
    [SerializeField] private GameObject prefabPersonajeLobby;

    [Header("Puntos de Asiento (Aldeanos de Prueba)")]
    [Tooltip("Arrastra aquí los 10 SM_Chr_Peasant_Male_01 (0-9) posicionados en la escena.")]
    [SerializeField] private List<GameObject> puntosDeAsiento = new List<GameObject>();

    [Header("Referencia de Cámara")]
    [SerializeField] private LobbyCameraManager cameraManager;

    public override void OnNetworkSpawn()
    {
        // Al arrancar la red, ocultamos TODOS los aldeanos falsos
        if (IsServer)
        {
            DesactivarAldeanosFalsosClientRpc();
        }

        if (IsClient)
        {
            SpawnPersonajeServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    /// <summary>
    /// Al cerrar la sesión de red (Stop Host/Client), reactivamos los aldeanos
    /// para que aparezcan correctamente si se vuelve a abrir el lobby.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        ReactivarAldeanosClientRpc();
    }

    private void OnDestroy()
    {
        // Fallback: si el objeto se destruye sin pasar por OnNetworkDespawn
        foreach (var asiento in puntosDeAsiento)
            if (asiento != null) asiento.SetActive(true);
    }

    [ClientRpc]
    private void DesactivarAldeanosFalsosClientRpc()
    {
        foreach (var asiento in puntosDeAsiento)
            if (asiento != null) asiento.SetActive(false);
    }

    [ClientRpc]
    private void ReactivarAldeanosClientRpc()
    {
        // [SERVER -> ALL CLIENTS] Reactivamos los aldeanos al cerrar la sesion
        foreach (var asiento in puntosDeAsiento)
            if (asiento != null) asiento.SetActive(true);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SpawnPersonajeServerRpc(ulong clientId)
    {
        if (prefabPersonajeLobby == null) return;

        // El ID determina el asiento
        int indexAsiento = (int)clientId;

        if (indexAsiento < 0 || indexAsiento >= puntosDeAsiento.Count)
            indexAsiento = 0;

        GameObject puntoReferencia = puntosDeAsiento[indexAsiento];
        if (puntoReferencia == null) return;

        // Instanciamos el personaje en el SERVIDOR (usamos la posición del asiento que hemos apagado)
        GameObject obj = Instantiate(prefabPersonajeLobby, puntoReferencia.transform.position, puntoReferencia.transform.rotation);
        
        // Lo spawneamos en la RED asignando la propiedad al cliente correspondiente
        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(clientId);

        Debug.Log($"[SERVER] Spawneado jugador real para Cliente {clientId} en asiento vacío {indexAsiento}");
    }

    /// <summary>
    /// Elimina el personaje visual del jugador local antes de desconectarse.
    /// Llamado desde LobbyUI.LeaveRoom().
    /// </summary>
    public void DestruirPersonajeLocal()
    {
        // Solo intentamos avisar si el objeto sigue en la red y el Manager está escuchando.
        // Esto evita el error de "Rpc methods can only be invoked after starting the NetworkManager" al cerrar el juego.
        if (IsSpawned && NetworkManager != null && NetworkManager.IsListening)
        {
            SolicitudDespawnServerRpc(NetworkManager.LocalClientId);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SolicitudDespawnServerRpc(ulong clientId)
    {
        // El servidor ya destruye automáticamente el NetworkObject del cliente al desconectarse.
        // Aquí puedes añadir más lógicas si el jugador abandona el asiento, pero por ahora queda vacío.
        Debug.Log($"[SERVER] El Cliente {clientId} ha abandonado la partida y su asiento vuelve a estar vacío.");
    }
}
