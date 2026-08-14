using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;

public class VotingUI : MonoBehaviour
{
    [Tooltip("El panel principal que oscurece o tapa la pantalla de juego")]
    public GameObject panelVotacion;
    
    [Tooltip("Un botón prefabricado oculto que se usará como molde para copiar")]
    public GameObject botonJugadorMolde;
    
    [Tooltip("El panel o cuadro (VerticalLayoutGroup recomendado) donde se alinearán los botones")]
    public Transform contenedorBotones;

    private List<GameObject> botonesActivos = new List<GameObject>();
    private bool yaVote = false;
    private bool asambleaActiva = false;

    private void Start()
    {
        // Seguro de Vida UI: Si te olvidaste ponerle un "Layout" al Contenedor en Unity, el código se lo inyecta solo
        // para que 10 o 15 botones no se encimen y aplasten en el mismo recuadro si hay muchos jugadores.
        if (contenedorBotones != null && contenedorBotones.GetComponent<VerticalLayoutGroup>() == null)
        {
            VerticalLayoutGroup layout = contenedorBotones.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false; 
            layout.childControlWidth = false;
            layout.spacing = 15f; 
        }

        // Nos aseguramos que inicie apagado al arrancar el juego
        OcultarPantallaVotacion();
    }

    public void MostrarPantallaVotacion()
    {
        asambleaActiva = true;

        if (Unity.Netcode.NetworkManager.Singleton.LocalClient != null && Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            var tpc = Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<StarterAssets.ThirdPersonController>();
            if (tpc != null) tpc.CanMove = false;

            var inputs = Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (inputs != null) 
            {
                inputs.cursorLocked = false;
                inputs.cursorInputForLook = false;
            }
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 1. Limpiar los botones de asambleas anteriores
        foreach (var btnObj in botonesActivos)
        {
            Destroy(btnObj);
        }
        botonesActivos.Clear();
        yaVote = false;
        panelVotacion.SetActive(true);

        // [MÉTODO INFALIBLE] Buscar a todos los jugadores por su componente físico (PlayerState) en la escena.
        // Esto ignora las listas secretas de Netcode y simplemente usa los ojos de Unity para ubicar los personajes.
        PlayerState[] todosLosJugadores = Object.FindObjectsByType<PlayerState>(FindObjectsInactive.Exclude);

        // 2. Averiguar si yo ("LocalPlayer") estoy muerto (los fantasmas no pueden votar)
        bool soyFantasma = true;
        foreach (PlayerState p in todosLosJugadores)
        {
            // 'IsOwner' significa: Este muñeco lo controla el ratón y teclado de MI pantalla actual.
            if (p.IsOwner)
            {
                if (!p.isDead.Value) soyFantasma = false;
                break; // Ya me encontré a mí mismo
            }
        }

        // 3. Crear botones por cada jugador vivo encontrado en la escena
        foreach (PlayerState estado in todosLosJugadores)
        {
            // Solo listamos a los que sigan vivos
            if (!estado.isDead.Value)
            {
                GameObject nuevoBotonObj = Instantiate(botonJugadorMolde, contenedorBotones);
                nuevoBotonObj.SetActive(true);
                botonesActivos.Add(nuevoBotonObj);

                Button botonUnity = nuevoBotonObj.GetComponent<Button>();
                Text texto = nuevoBotonObj.GetComponentInChildren<Text>();
                
                ulong idDestino = estado.OwnerClientId;
                string pName = estado.playerName.Value.ToString().TrimEnd('\0');

                if (texto != null)
                {
                    texto.text = $"Expulsar a: {pName}";
                }

                // Regla 1: Fantasmas miran pero no tocan.
                if (soyFantasma)
                {
                    botonUnity.interactable = false;
                    if (texto != null) texto.text += " (Modo Fantasma)";
                }
                else
                {
                    // Regla 2: No puedes autovotarte
                    if (estado.IsOwner) // ¡Ese soy yo mismito!
                    {
                        botonUnity.interactable = false;
                        if (texto != null) texto.text += " (Tú)";
                    }
                    else
                    {
                        botonUnity.onClick.AddListener(() =>
                        {
                            EmitirMiVoto(idDestino);
                        });
                    }
                }
            }
        }

        // 4. Agregar siempre un botón extra para NO hacer nada (Skip) a los vivos
        if (!soyFantasma)
        {
            GameObject nuevoBotonObj = Instantiate(botonJugadorMolde, contenedorBotones);
            nuevoBotonObj.SetActive(true);
            botonesActivos.Add(nuevoBotonObj);

            Button botonUnity = nuevoBotonObj.GetComponent<Button>();
            Text texto = nuevoBotonObj.GetComponentInChildren<Text>();
            
            if (texto != null) texto.text = "✖ Nadie. (Omitir Voto)";

            botonUnity.onClick.AddListener(() =>
            {
                EmitirMiVoto(ulong.MaxValue); // Código ultra secreto para "Nadie"
            });
        }
    }

    private void Update()
    {
        // [Cierre Fuerte de Ratón] Si estamos en Votación, obligamos al ratón y a Windows
        // a mantenerse libres, pase lo que pase si cambias de pantalla o haces Alt+Tab.
        if (asambleaActiva)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void EmitirMiVoto(ulong idObjetivo)
    {
        // Validar si ya emití el voto para no hacer spam click
        if (yaVote) return;
        yaVote = true;

        // Visualmente, apagamos la funcionalidad de los botones cuando ya elijas
        foreach (var btnObj in botonesActivos)
        {
            Button b = btnObj.GetComponent<Button>();
            if (b != null) b.interactable = false;
        }

        // ¡Táctica de Red Definitiva!
        // En vez de rogarle a GameManager que entregue nuestro voto...
        // ...usamos TÚ CUERPO físico en red, que tiene tu Autoridad 100% indiscutible:
        PlayerState[] todosLosEstados = Object.FindObjectsByType<PlayerState>(FindObjectsInactive.Exclude);
        
        foreach (PlayerState p in todosLosEstados)
        {
            if (p.IsOwner) // Ese soy yo (Mi cuerpo)
            {
                p.EnviarMiVotoServerRpc(idObjetivo);
                Debug.Log($"[VotingUI] Mi cuerpo {p.OwnerClientId} le envió el voto directo al Servidor Master.");
                break;
            }
        }
    }

    public void OcultarPantallaVotacion()
    {
        asambleaActiva = false;
        if (panelVotacion != null)
        {
            panelVotacion.SetActive(false);
        }

        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.LocalClient != null && Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            var tpc = Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<StarterAssets.ThirdPersonController>();
            if (tpc != null) tpc.CanMove = true;

            var inputs = Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (inputs != null) 
            {
                inputs.cursorLocked = true;
                inputs.cursorInputForLook = true;
            }
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}

