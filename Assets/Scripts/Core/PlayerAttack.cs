using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : NetworkBehaviour
{
    private PlayerState playerState;
    private GameManager gameManager;

    [Tooltip("La máscara de caja de 'Capa' (Layer) a la que pertenecerán los otros jugadores para poder tocarlos con el cuchillo")]
    public LayerMask layerJugadores;

    private void Start()
    {
        playerState = GetComponent<PlayerState>();
        // Intentamos encontrar al cerebro del juego si hemos caído en la escena principal
        gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Update()
    {
        // Regla Dorada de Netcode Local: Tú solo vigilas tus propios botones
        if (!IsOwner) return;

        // Comprobaciones de Seguridad Local antes de soltar un ataque al vacío:
        // 1. Soy lobo. 2. Estoy vivo. 3. Está la FASE DE LA NOCHE en curso.
        if (playerState != null && playerState.isWolf.Value && !playerState.isDead.Value)
        {
            if (gameManager != null && gameManager.currentPhase.Value == GamePhase.Noche)
            {
                // Pulsar E (Teclado) usando el NUEVO Input System -> Solo en tu compu local
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    EjecutarAtaqueLocal();
                }
            }
        }
    }

    private void EjecutarAtaqueLocal()
    {
        // Origen del rayo: Un metro encima de nuestros pies (Pecho/Cara)
        Vector3 origen = transform.position + Vector3.up * 1f;
        // Dirección: Hacia donde estamos mirando
        Vector3 direccion = transform.forward;
        // Distancia del cuchillo (cuerpo a cuerpo): 2.5 metros
        float alcance = 2.5f;
        // Grosor del ataque: 0.8 metros de radio para que no haya que "apuntar" de forma perfecta con el ratón
        float grosorBola = 0.8f;

        Debug.DrawRay(origen, direccion * alcance, Color.red, 2f); // Te dibujo el láser en Scene para pruebas

        // Disparamos un cilindro grueso hacia donde miramos, agarramos TODOS los objetos que toque
        RaycastHit[] impactos = Physics.SphereCastAll(origen, grosorBola, direccion, alcance);
        bool tocamosAAlguienVálido = false;

        foreach (RaycastHit hitChoque in impactos)
        {
            // Verificamos si lo que tocamos pertenece a un Jugador (tiene NetworkObject)
            NetworkObject objetivoNet = hitChoque.collider.GetComponentInParent<NetworkObject>();

            if (objetivoNet != null && objetivoNet.gameObject != this.gameObject)
            {
                // Si encontramos a alguien que no somos nosotros mismos, ¡le damos!
                tocamosAAlguienVálido = true;
                Debug.Log($"<color=orange>[Cliente] ¡Alcanzado: {objetivoNet.NetworkObjectId}! Llamando al Servidor...</color>");
                
                // Pedimos el asesinato
                MatarJugadorServerRpc(objetivoNet.NetworkObjectId);
                break; // Parar aquí para no matar a 5 personas de un golpe si están muy pegadas
            }
        }

        if (!tocamosAAlguienVálido)
        {
            Debug.Log("[Cliente] Lanzaste un Zarpazo, pero no le diste a nada...");
        }
    }


    [ServerRpc]
    private void MatarJugadorServerRpc(ulong victimaNetworkId, ServerRpcParams rpcParams = default)
    {
        // Aquí el código se teletransporta Mágicamente y corre ÚNICAMENTE en la memoria RAM del Servidor (Host)

        // Verificamos por seguridad Anti-Hack: ¿El tío que nos llamó de verdad estaba autorizado para matar?
        if (playerState == null || !playerState.isWolf.Value || playerState.isDead.Value)
        {
            Debug.LogWarning($"[HACK] El jugador {OwnerClientId} solicitó matar, ¡pero no es el Lobo (o está muerto)!");
            return;
        }

        // ¿Realmente es de noche y no usó un truco para congelar el reloj localmente?
        if (gameManager == null || gameManager.currentPhase.Value != GamePhase.Noche)
        {
            Debug.LogWarning($"[HACK] El jugador {OwnerClientId} solicitó matar... de Día.");
            return;
        }

        // Buscamos el DNI de su víctima en la base de datos de los objetos vivos
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(victimaNetworkId, out NetworkObject victimaObj))
        {
            // OTRA CAPA DE SEGURIDAD. Calculamos la distancia real entre agresor y víctima (Los hacks pueden mentir diciendo que estaban juntos)
            float distanciaReal = Vector3.Distance(transform.position, victimaObj.transform.position);
            
            // Si la distancia es mayor a 3 metros (margen de Lag), anulamos el cuchillo
            if (distanciaReal > 3f)
            {
                Debug.LogWarning($"[HACK] Lobo {OwnerClientId} atacó a {victimaNetworkId} a 10km de distancia. Bloqueado.");
                return;
            }

            // Ejecución formalizada
            PlayerState estadoDeLaVictima = victimaObj.GetComponent<PlayerState>();
            if (estadoDeLaVictima != null && !estadoDeLaVictima.isDead.Value)
            {
                estadoDeLaVictima.isDead.Value = true; // Sentencia de muerte escrita en la variable Autorizada.
                Debug.Log($"<color=red>====== ¡ASESINATO CONCEDIDO! ====== Lobo [{OwnerClientId}] fulminó a Aldeano [{victimaNetworkId}]</color>");
            }
        }
    }
}
