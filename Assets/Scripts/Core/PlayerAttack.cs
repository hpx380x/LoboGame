using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : NetworkBehaviour
{
    private PlayerState playerState;
    private PlayerStatusEffects statusEffects;
    private GameManager gameManager;

    [Tooltip("La máscara de caja de 'Capa' (Layer) a la que pertenecerán los otros jugadores para poder tocarlos con el cuchillo")]
    public LayerMask layerJugadores;

    private void Start()
    {
        playerState = GetComponent<PlayerState>();
        statusEffects = GetComponent<PlayerStatusEffects>();
        gameManager = GameManager.Instance;
    }

    private void Update()
    {
        // Regla Dorada de Netcode Local: Tú solo vigilas tus propios botones
        if (!IsOwner) return;

        if (gameManager == null) gameManager = GameManager.Instance;

        // Comprobación de ActionMap habilitado (evita ataques en menús, pergaminos, lobby...)
        if (TryGetComponent(out PlayerInput playerInput) && playerInput.currentActionMap != null && !playerInput.currentActionMap.enabled)
        {
            return;
        }

        // Comprobaciones de Seguridad Local antes de soltar un ataque al vacío:
        // 1. Soy lobo. 2. Estoy vivo. 3. Está la FASE DE LA NOCHE en curso (o tengo el poder del Lobo Albino).
        if (playerState != null && playerState.isWolf.Value && !playerState.isDead.Value)
        {
            bool puedeAtacar = (gameManager != null && gameManager.currentPhase.Value == GamePhase.Noche) || (statusEffects != null && statusEffects.hasLoboAlbinoPower.Value);
            if (puedeAtacar)
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
                Debug.Log($"[Cliente] ¡Alcanzado: {objetivoNet.NetworkObjectId}! Llamando al Servidor...");
                
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

        if (gameManager == null) gameManager = GameManager.Instance;

        // ¿Realmente es de noche y no usó un truco para congelar el reloj localmente?
        bool esNoche = gameManager != null && gameManager.currentPhase.Value == GamePhase.Noche;
        bool tienePoderAlbino = statusEffects != null && statusEffects.hasLoboAlbinoPower.Value;

        if (!esNoche && !tienePoderAlbino)
        {
            Debug.LogWarning($"[HACK] El jugador {OwnerClientId} solicitó matar... de Día y sin poderes de Lobo Albino.");
            return;
        }

        // Si usó el ataque de día gracias al Lobo Albino, lo gastamos
        if (!esNoche && tienePoderAlbino)
        {
            if (statusEffects != null) statusEffects.hasLoboAlbinoPower.Value = false;
            Debug.Log($"<color=white>[Leyenda] El lobo {OwnerClientId} ha consumido su ataque de Día de Lobo Albino.</color>");
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
            PlayerStatusEffects statusVictima = victimaObj.GetComponent<PlayerStatusEffects>();
            if (estadoDeLaVictima != null && !estadoDeLaVictima.isDead.Value)
            {
                // [Mecánica Cota De Malla] Comprobar si tiene el chaleco protector equipado
                if (statusVictima != null && statusVictima.tieneCotaMalla.Value)
                {
                    // La armadura se rompe pero el aldeano sobrevive
                    statusVictima.tieneCotaMalla.Value = false;
                    Debug.Log($"<color=cyan>[Servidor] El agresor {OwnerClientId} atacó a {victimaNetworkId}, ¡pero su COTA DE MALLA le salvó la vida y se rompió!</color>");
                    return; // Abortamos el asesinato
                }

                estadoDeLaVictima.isDead.Value = true; // Sentencia de muerte escrita en la variable Autorizada.
                Debug.Log($"<color=red>====== ¡ASESINATO CONCEDIDO! ====== Lobo [{OwnerClientId}] fulminó a Aldeano [{victimaNetworkId}]</color>");
            }
        }
    }
}

