using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    private Button startButton;

    void Awake()
    {
        startButton = GetComponent<Button>();
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartGameClicked);
        }
    }

    void Update()
    {
        // Mientras este botón esté en pantalla, forzamos que el ratón esté libre
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnStartGameClicked()
    {
        Debug.Log("[GameUI] ¡Has hecho clic en el botón 'Iniciar Partida'!");
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            // OJO: Ya no ocultamos el script con gameObject.SetActive(false) porque
            // lo rompería irreversiblemente para futuras partidas si la gente se sale
            // y vuelve a crear otra sala distinta.
            gm.StartGame();
        }
        else
        {
            Debug.LogError("[GameUI] No se encontró el GameManager en la escena.");
        }
    }
}
