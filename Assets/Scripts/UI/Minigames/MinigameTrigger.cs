using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Se coloca en objetos que deben abrir un minijuego de UI (Pantalla).
/// Hereda la lógica de proximidad de QuestInteractable pero añade la apertura de UI.
/// </summary>
public class MinigameTrigger : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject minigameUIPrefab; // El objeto con UIDocument y MiniGameWireConnect
    public string taskId = "Cables";
    public string mensajePrompt = "[E] Reparar Cables";

    private bool jugadorLocalCerca = false;
    private GameObject activeUIInstance;

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsOwner)
        {
            jugadorLocalCerca = true;
            GameplayUI ui = FindFirstObjectByType<GameplayUI>();
            if (ui != null) ui.MostrarMensajeTarea(mensajePrompt, 0f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.isTrigger) return;
        NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsOwner)
        {
            jugadorLocalCerca = false;
            GameplayUI ui = FindFirstObjectByType<GameplayUI>();
            if (ui != null) ui.MostrarMensajeTarea("", 0f);
            
            // Cerrar el minijuego si se aleja (opcional)
            if (activeUIInstance != null) CerrarMinijuego();
        }
    }

    private void Update()
    {
        if (jugadorLocalCerca && activeUIInstance == null)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                AbrirMinijuego();
            }
        }
    }

    [Header("Misión")]
    public TaskPoint taskPointVinculado;

    private void AbrirMinijuego()
    {
        if (minigameUIPrefab == null) return;
        
        activeUIInstance = Instantiate(minigameUIPrefab);
        
        // Configurar Minigame
        var minigame = activeUIInstance.GetComponent<TapestryMinigame>();
        if (minigame != null)
        {
            if (taskPointVinculado == null) taskPointVinculado = GetComponent<TaskPoint>();
            minigame.SetupMinigame(taskPointVinculado);

            // Si hay un restorer de tapiz en este objeto, vincularlo
            var restorer = GetComponent<TapestryRestorer>();
            if (restorer != null) restorer.SuscribirseAlMinijuego(minigame);
        }

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    private void CerrarMinijuego()
    {
        if (activeUIInstance != null) Destroy(activeUIInstance);
        
        // Solo bloquear si no hay otros paneles de UI abiertos (check simple)
        if (FindObjectsByType<UIDocument>(FindObjectsSortMode.None).Length <= 1)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
    }
}
