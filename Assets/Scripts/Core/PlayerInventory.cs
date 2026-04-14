using Unity.Netcode;
using UnityEngine;
using Core.Enums;

public class PlayerInventory : NetworkBehaviour
{
    [Header("Inventario Autoritativo")]
    public NetworkVariable<int> monedas = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<TipoObjeto> objetoEnMano = new NetworkVariable<TipoObjeto>(
        TipoObjeto.Ninguno, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    [Header("Materiales para Misiones")]
    public NetworkVariable<MaterialesMision> materiales = new NetworkVariable<MaterialesMision>(
        new MaterialesMision(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [System.Serializable]
    public struct MaterialesMision : INetworkSerializable
    {
        public int lingotes;
        public int piedrasMagicas;
        public int madera;
        public int cuerdas;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref lingotes);
            serializer.SerializeValue(ref piedrasMagicas);
            serializer.SerializeValue(ref madera);
            serializer.SerializeValue(ref cuerdas);
        }
    }

    [Header("Modelos 3D Visuales (Hijos de la Mano)")]
    [Tooltip("Asocia cada TipoObjeto a su GameObject (Malla 3D) correspondiente")]
    [SerializeField] private VisualObjetoMapping[] modelosVisuales;

    [System.Serializable]
    public struct VisualObjetoMapping
    {
        public TipoObjeto tipo;
        public GameObject modelo;
    }

    private PlayerState playerState;

    private void Awake()
    {
        playerState = GetComponent<PlayerState>();
    }

    public override void OnNetworkSpawn()
    {
        objetoEnMano.OnValueChanged += OnObjetoCambiado;
        monedas.OnValueChanged += OnMonedasCambiadas;
        
        // Cargar estado inicial por si entra tarde a la partida
        ActualizarVisualizacionObjeto(objetoEnMano.Value);
        
        if (IsOwner)
        {
            GameplayUI ui = FindFirstObjectByType<GameplayUI>();
            if (ui != null)
            {
                ui.ActualizarInventario(objetoEnMano.Value.ToString());
                ui.ActualizarMonedas(monedas.Value);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        objetoEnMano.OnValueChanged -= OnObjetoCambiado;
        monedas.OnValueChanged -= OnMonedasCambiadas;
        base.OnNetworkDespawn();
    }

    // --- ACCIONES SERVIDOR ---

    [Rpc(SendTo.Server)]
    public void RecogerObjetoServerRpc(TipoObjeto nuevoObjeto)
    {
        if (playerState != null && playerState.isDead.Value) return;
        objetoEnMano.Value = nuevoObjeto;
        Debug.Log($"[Server] El jugador {OwnerClientId} ha recogido exitosamente: {nuevoObjeto}");
    }

    [Rpc(SendTo.Server)]
    public void GanarMonedasServerRpc(int cantidad)
    {
        if (playerState != null && playerState.isDead.Value) return;
        monedas.Value += cantidad;
        Debug.Log($"[Server] El jugador {OwnerClientId} ganó {cantidad} monedas. Total: {monedas.Value}");
    }

    [Rpc(SendTo.Server)]
    public void ComprarObjetoServerRpc(TipoObjeto objetoDeseado, int coste)
    {
        if (playerState != null && playerState.isDead.Value) return;
        if (monedas.Value >= coste)
        {
            monedas.Value -= coste;
            objetoEnMano.Value = objetoDeseado;
            Debug.Log($"[Server] El jugador {OwnerClientId} compró {objetoDeseado} por {coste} monedas.");
        }
        else
        {
            Debug.LogWarning($"[Server] Jugador {OwnerClientId} intentó comprar {objetoDeseado} sin fondos suficientes.");
        }
    }

    // --- MÉTODOS LOCALES Y VISUALES ---

    private void OnObjetoCambiado(TipoObjeto viejo, TipoObjeto nuevo)
    {
        ActualizarVisualizacionObjeto(nuevo);

        if (IsOwner)
        {
            GameplayUI ui = FindFirstObjectByType<GameplayUI>();
            if (ui != null) ui.ActualizarInventario(nuevo.ToString());
        }
    }

    private void OnMonedasCambiadas(int viejo, int nuevo)
    {
        if (IsOwner)
        {
            GameplayUI ui = FindFirstObjectByType<GameplayUI>();
            if (ui != null) ui.ActualizarMonedas(nuevo);
        }
    }

    private void ActualizarVisualizacionObjeto(TipoObjeto obj)
    {
        if (modelosVisuales == null) return;

        foreach (var mapping in modelosVisuales)
        {
            if (mapping.modelo != null)
            {
                // Si el tipo coincide con el equipado, lo activamos. Si no, lo apagamos.
                mapping.modelo.SetActive(mapping.tipo == obj);
            }
        }
    }
}
