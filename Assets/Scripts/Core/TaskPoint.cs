using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public enum TipoInteraccion
{
    Instantanea,           // Pulsas E y te lo da (o abre un panel de minijuego externo)
    MantenerBoton,         // Tienes que dejar pulsada la E durante X segundos (ej: afilando daga)
    PulsarRepetidamente    // Tienes que pulsar la E muchas veces para llenar una barra
}

public class TaskPoint : MonoBehaviour
{
    [Header("Recompensa de la Tarea")]
    [Tooltip("El objeto que recibirá el jugador al acercarse aquí")]
    public TipoObjeto objetoRecompensa = TipoObjeto.Antorcha;

    [Header("Tipo de Minijuego In-Game")]
    public TipoInteraccion tipoDeMinijuego = TipoInteraccion.Instantanea;

    [Tooltip("Segundos que hay que mantener pulsado, o toques necesarios (Solo para los minijuegos físicos)")]
    public float objetivoMinijuego = 5f; 

    // Progreso actual del minijuego in-game
    private float progresoActual = 0f;

    [Header("Ajustes")]
    [Tooltip("Si usas Instantanea, ¿Requiere pulsar la E o te lo da solo con pisar?")]
    public bool requierePulsarBoton = true;

    // Memoria interna para saber si ESTAMOS nosotros en la zona
    private bool jugadorLocalCerca = false;
    private bool mensajeMostrado = false; // [NUEVO] Evitamos spam en la consola
    private bool minijuegoEnProgreso = false; // [NUEVO] Evita doble activación
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

        // [NUEVO] Si lo que nos ha tocado es otro colisionador "invisible" (ej. el aura de ataque del Lobo o un radar), lo ignoramos.
        // Solo queremos que se active cuando el CUERPO SÓLIDO del personaje (CharacterController) choque contra la mesa.
        if (other.isTrigger) return;

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
                // [NUEVO] Anti-Grindeo: Si ya llevas este objeto exacto en las manos, te ignoramos.
                if (jugadorLocalState.objetoEnMano.Value == objetoRecompensa)
                {
                    return;
                }

                if (!jugadorLocalCerca) // Solo la primera vez que entramos
                {
                    jugadorLocalCerca = true;
                    
                    if (requierePulsarBoton)
                    {
                        if (!mensajeMostrado && !minijuegoEnProgreso) // Imprimimos de una sola vez
                        {
                            Debug.Log($"<color=yellow>[Tarea]</color> Estás cerca de la máquina. Pulsa 'E' para hacer el minijuego de: {objetoRecompensa}");
                            mensajeMostrado = true;
                        }
                    }
                    else
                    {
                        // Si no requiere botón, empieza el minijuego o da la recompensa instantánea al pisar
                        IntentarIniciarMinijuegoSimuladoUI();
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.isTrigger) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if(netObj == null) netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsOwner)
        {
            jugadorLocalCerca = false;
            mensajeMostrado = false; // Reset al mensaje amarillo
            jugadorLocalState = null;
            
            // Si nos alejamos a mitad del afilado de la daga, perdemos el progreso
            if (minijuegoEnProgreso && (tipoDeMinijuego == TipoInteraccion.MantenerBoton || tipoDeMinijuego == TipoInteraccion.PulsarRepetidamente))
            {
                Debug.Log("<color=red>[Tarea]</color> Te alejaste. El progreso del minijuego se ha perdido.");
                minijuegoEnProgreso = false;
                progresoActual = 0f;

                GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
                if (ui != null) ui.MostrarBarraProgreso(false);
            }
        }
    }

    private void Update()
    {
        // Si creemos que el jugador está cerca...
        if (jugadorLocalCerca && jugadorLocalState != null)
        {
            // [NUEVO] Si mientras estabas aquí dentro lograste coger el objeto, apagamos la mesa para ti.
            if (jugadorLocalState.objetoEnMano.Value == objetoRecompensa)
            {
                jugadorLocalCerca = false;
                mensajeMostrado = false;
                if (minijuegoEnProgreso)
                {
                    minijuegoEnProgreso = false;
                    progresoActual = 0f;
                    GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
                    if (ui != null) ui.MostrarBarraProgreso(false);
                }
                return;
            }

            // [Sistema Anti-Bugs] Evita que Unity crea que sigues dentro al ser teletransportado o al salir rápido
            if (miCollider != null)
            {
                // Magia matemática: ClosestPoint encuentra el borde físico de la caja verde.
                // Si la distancia entre la piel del jugador y la caja verde es mayor a 1.5 metros, nos fuimos.
                Vector3 puntoSuperficie = miCollider.ClosestPoint(jugadorLocalState.transform.position);
                
                if (Vector3.Distance(jugadorLocalState.transform.position, puntoSuperficie) > 1.5f)
                {
                    // El jugador ya no está aquí físicamente
                    jugadorLocalCerca = false;
                    mensajeMostrado = false;
                    jugadorLocalState = null;
                    
                    if (minijuegoEnProgreso)
                    {
                        minijuegoEnProgreso = false;
                        progresoActual = 0f;
                        GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
                        if (ui != null) ui.MostrarBarraProgreso(false);
                    }
                    return;
                }
            }

            // ============================================
            // LOGICA DEL MINIJUEGO IN-GAME DIRECTO
            // ============================================

            if (Keyboard.current == null) return;

            if (tipoDeMinijuego == TipoInteraccion.Instantanea)
            {
                // Misión Instantánea (o abridora de panel UI)
                if (requierePulsarBoton && !minijuegoEnProgreso && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    IntentarIniciarMinijuegoSimuladoUI();
                }
            }
            else if (tipoDeMinijuego == TipoInteraccion.MantenerBoton)
            {
                // Misión Física: Mantener forjado / afilado
                if (Keyboard.current.eKey.isPressed)
                {
                    if (!minijuegoEnProgreso) // Acabamos de pulsar la tecla por primera vez
                    {
                        minijuegoEnProgreso = true;
                        GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
                        if (ui != null) ui.MostrarBarraProgreso(true);
                    }

                    progresoActual += Time.deltaTime;
                    
                    GameplayUI uiHud = Object.FindFirstObjectByType<GameplayUI>();
                    if (uiHud != null) uiHud.ActualizarBarraProgreso(progresoActual, objetivoMinijuego);

                    // Solo imprimimos cada segundo entero para no reventar la consola
                    if (Mathf.Floor(progresoActual) > Mathf.Floor(progresoActual - Time.deltaTime))
                    {
                        Debug.Log($"<color=orange>[Forjando]</color> Afilando daga... {Mathf.Round(progresoActual)}s / {objetivoMinijuego}s");
                    }

                    if (progresoActual >= objetivoMinijuego)
                    {
                        CompletarMinijuegoInGame();
                    }
                }
                else if (minijuegoEnProgreso) // Si suelta la tecla a medias
                {
                    Debug.Log("<color=red>[Forjando]</color> Dejaste de afilar. Progreso reiniciado.");
                    progresoActual = 0f;
                    minijuegoEnProgreso = false;

                    GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
                    if (ui != null) ui.MostrarBarraProgreso(false);
                }
            }
            else if (tipoDeMinijuego == TipoInteraccion.PulsarRepetidamente)
            {
                // Misión Física: Martillar pulsando E a toda pastilla
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    if (!minijuegoEnProgreso)
                    {
                        minijuegoEnProgreso = true;
                        GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
                        if (ui != null) ui.MostrarBarraProgreso(true);
                    }

                    progresoActual += 1f;

                    GameplayUI uiHud = Object.FindFirstObjectByType<GameplayUI>();
                    if (uiHud != null) uiHud.ActualizarBarraProgreso(progresoActual, objetivoMinijuego);

                    Debug.Log($"<color=orange>[Martillando]</color> Golpe... {progresoActual} / {objetivoMinijuego}");

                    if (progresoActual >= objetivoMinijuego)
                    {
                        CompletarMinijuegoInGame();
                    }
                }
            }
        }
    }

    private void CompletarMinijuegoInGame()
    {
        Debug.Log("<color=green>[Misión In-Game]</color> ¡Objeto terminado con éxito!");
        progresoActual = 0f;
        minijuegoEnProgreso = false;

        GameplayUI ui = Object.FindFirstObjectByType<GameplayUI>();
        if (ui != null) ui.MostrarBarraProgreso(false);

        EntregarRecompensa();
    }

    // El sistema antiguo si quisieras usar Paneles Externos
    private void IntentarIniciarMinijuegoSimuladoUI()
    {
        if (jugadorLocalState == null || jugadorLocalState.isDead.Value) return;

        Debug.Log($"<color=cyan>[Minijuego UI]</color> Abriendo panel de minijuego inventado para: {objetoRecompensa}...");
        minijuegoEnProgreso = true;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(SimularResolucionMinijuego());
    }

    private System.Collections.IEnumerator SimularResolucionMinijuego()
    {
        yield return new WaitForSeconds(2f); // Finge que lo hiciste en la UI
        
        Debug.Log("<color=green>[Minijuego UI]</color> ¡Panel cerrado! Control devuelto.");
        minijuegoEnProgreso = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        EntregarRecompensa();
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