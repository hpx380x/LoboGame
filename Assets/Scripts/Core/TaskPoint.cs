using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class TaskPoint : MonoBehaviour
{
    [Header("Recompensa de la Tarea")]
    [Tooltip("El objeto que recibirá el jugador al acercarse aquí")]
    public TipoObjeto objetoRecompensa = TipoObjeto.Antorcha;

    [Tooltip("Si marcas esto, el jugador debe entrar en la zona y pulsar 'E'. Si lo desmarcas, se le da solo con acercarse.")]
    public bool requierePulsarBoton = true;

    // Memoria interna para saber si ESTAMOS nosotros en la zona
    private bool jugadorLocalCerca = false;
    private bool mensajeMostrado = false; // [NUEVO] Evitamos spam en la consola
    private PlayerState jugadorLocalState;
    private Collider miCollider;

    private void Awake()
    {
        miCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 0. Si el mapa acaba de cargar, ignoramos unos segundos para que los jugadores 
        // no choquen fantasmálmente en la coordenada (0,0,0) antes de ser teletransportados.
        if (Time.timeSinceLevelLoad < 2f) return;

        // 1. Verificamos si lo que acaba de tocar la zona es un jugador conectado en red
        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if(netObj == null) netObj = other.GetComponentInParent<NetworkObject>();
        
        // 2. Comprobamos si es NUESTRO jugador (el que controlamos en nuestra pantalla de PC)
        // (Ignoramos si es un amigo atravesando la zona en nuestra pantalla)
        if (netObj != null && netObj.IsOwner)
        {
            jugadorLocalState = netObj.GetComponent<PlayerState>();
            if (jugadorLocalState != null && !jugadorLocalState.isDead.Value)
            {
                if (!jugadorLocalCerca) // Solo la primera vez que entramos
                {
                    jugadorLocalCerca = true;
                    
                    if (requierePulsarBoton)
                    {
                        if (!mensajeMostrado) // Imprimimos de una sola vez
                        {
                            Debug.Log($"<color=yellow>[Tarea]</color> Estás cerca de la máquina. Pulsa 'E' para recoger: {objetoRecompensa}");
                            mensajeMostrado = true;
                        }
                    }
                    else
                    {
                        EntregarRecompensa();
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if(netObj == null) netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsOwner)
        {
            jugadorLocalCerca = false;
            mensajeMostrado = false; // Reset al mensaje amarillo
            jugadorLocalState = null;
        }
    }

    private void Update()
    {
        // Si creemos que el jugador está cerca...
        if (jugadorLocalCerca && jugadorLocalState != null)
        {
            // [Sistema Anti-Bugs] Si el jugador fue teletransportado de golpe (ej: asamblea de votación)
            // OnTriggerExit nunca se ejecuta porque Unity apaga el CharacterController. 
            // Usamos una distancia de 8 metros para saber si se fue de golpe (ya que bounds.Contains 
            // falla si el cubo está elevado y los pies del personaje tocan el suelo por debajo).
            if (miCollider != null && Vector3.Distance(jugadorLocalState.transform.position, miCollider.transform.position) > 8f)
            {
                // El jugador ya no está aquí, fue un teletransporte fantasma
                jugadorLocalCerca = false;
                mensajeMostrado = false;
                jugadorLocalState = null;
                return;
            }

            // Misión de Recolección (Pulsar E)
            if (requierePulsarBoton && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                EntregarRecompensa();
            }
        }
    }

    private void EntregarRecompensa()
    {
        if (jugadorLocalState != null && !jugadorLocalState.isDead.Value)
        {
            // 3. Magia Pura: Le enviamos la petición oficial por radio al Servidor
            jugadorLocalState.RecogerObjetoServerRpc(objetoRecompensa);
            
            Debug.Log($"<color=green>[Tarea] ¡Misión completada!</color> Solicitando {objetoRecompensa} al servidor.");
            
            // Opcional: Podrías destruirlo si solo sirve una vez, pero lo dejaremos 
            // infinito como dispensador para hacer pruebas técnicas.
        }
    }
}