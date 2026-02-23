using UnityEngine;
using TMPro;
using System.Collections;

public class GameplayUI : MonoBehaviour
{
    [Header("Elementos de Animación UI")]
    [Tooltip("El texto gigante en medio de la pantalla que dice 'ERES EL LOBO'")]
    [SerializeField] private TextMeshProUGUI textoRol;
    
    [Tooltip("El texto en una esquina que dice si es de DÍA o NOCHE")]
    [SerializeField] private TextMeshProUGUI textoFase;

    [Header("Panel de Tareas")]
    [Tooltip("El contenedor (Panel) entero de la ventana de tareas que acabas de crear")]
    [SerializeField] private GameObject taskPanel;
    [Tooltip("El componente de Texto donde iremos escribiendo la lista de misiones a hacer")]
    [SerializeField] private TextMeshProUGUI taskText;

    [Header("HUD del Inventario")]
    [Tooltip("El texto que dice el nombre del objeto en mano (ej: 'Antorcha')")]
    [SerializeField] private TextMeshProUGUI textoInventario;
    [Tooltip("El fondo o ranura del inventario para cambiarle el color activo/inactivo")]
    [SerializeField] private UnityEngine.UI.Image fondoInventario;

    public void MostrarRol(string rol)
    {
        if (textoRol != null)
        {
            textoRol.text = rol;
            textoRol.gameObject.SetActive(true);
            
            // Activamos el Panel de Tareas cuando arranca el juego
            GenerarListaDeTareas(rol);

            // Iniciamos la rutina para desaparecer el texto gigante tras 3 segundos
            StartCoroutine(OcultarRolRutina());
        }
    }

    private void GenerarListaDeTareas(string rolDelJugador)
    {
        // 1. Nos aseguramos de que el panel se vea
        if (taskPanel != null) taskPanel.SetActive(true);

        if (taskText != null)
        {
            if (rolDelJugador == "Lobo")
            {
                taskText.text = "<color=red><s>SIMULAR TAREAS:</s></color>\n- Limpiar turbinas\n- Vaciar basura\n\n<color=red><b>OBJETIVO REAL:\n¡Caza a los aldeanos!</b></color>";
            }
            else // Es un pobre e inocente Aldeano
            {
                taskText.text = "<color=yellow>MIS TAREAS:</color>\n- Descargar datos en el servidor\n- Reparar cableado eléctrico\n- Ajustar motores";
            }
        }
    }

    private IEnumerator OcultarRolRutina()
    {
        // Espera pacientemente 3 segundos reales
        yield return new WaitForSeconds(3f);
        
        if (textoRol != null)
        {
            textoRol.gameObject.SetActive(false);
        }
    }

    public void ActualizarFase(string fase)
    {
        if (textoFase != null)
        {
            textoFase.text = "Fase: " + fase.ToUpper();
        }
    }

    // --- SISTEMA DE HUD DE INVENTARIO ---
    // Este método lo llamará el jugador local automáticamente cada vez que recoja algo nuevo.
    public void ActualizarInventario(string nombreObjeto)
    {
        if (textoInventario != null)
        {
            if (nombreObjeto == "Ninguno")
            {
                textoInventario.text = "Mano Vacía";
                if (fondoInventario != null) fondoInventario.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Gris oscuro
            }
            else
            {
                textoInventario.text = nombreObjeto;
                // Si tienes un objeto, la ranura se ilumina de amarillo levemente (o naranja)
                if (fondoInventario != null) fondoInventario.color = new Color(1f, 0.8f, 0f, 0.6f); 
            }
        }
    }

    public void MostrarVictoria(string mensajeVictoria)
    {
        if (textoRol != null)
        {
            textoRol.text = mensajeVictoria;
            textoRol.transform.localScale = Vector3.one * 1.5f; // Lo hacemos más gigantesco
            textoRol.gameObject.SetActive(true);
            
            // Ya NO se oculta a los 3 segundos, se queda pegado celebrando hasta ir al Lobby.
        }
    }
}
