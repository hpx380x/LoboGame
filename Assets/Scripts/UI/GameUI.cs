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

        // Lo ocultamos por defecto para que el mero mortal (Cliente) no pueda pulsarlo por error
        gameObject.SetActive(false);
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
            gm.StartGame();
            // Desactivamos el botón una vez iniciada la partida para no spamear roles
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("[GameUI] No se encontró el GameManager en la escena.");
        }
    }
}
