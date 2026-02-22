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

    public void MostrarRol(string rol)
    {
        if (textoRol != null)
        {
            textoRol.text = rol;
            textoRol.gameObject.SetActive(true);
            
            // Iniciamos la rutina para desaparecerlo tras 3 segundos
            StartCoroutine(OcultarRolRutina());
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
