using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona la visualización del HUD de interacción en el lobby (Barra circular y textos).
/// </summary>
public class LobbyInteractionUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private Image progressCircle;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI targetNameText;
    [SerializeField] private TextMeshProUGUI exitPromptText; // [NUEVO] Texto fijo de "ESC para salir"

    private void Awake()
    {
        // Ocultar al inicio
        if (interactionPanel != null) interactionPanel.SetActive(false);
        if (progressCircle != null) progressCircle.fillAmount = 0;
        
        // El texto de salida debe estar siempre visible si existe
        if (exitPromptText != null) 
        {
            exitPromptText.gameObject.SetActive(true);
            exitPromptText.text = "[ESC] ABANDONAR SALA";
        }
    }

    public void ShowPrompt(string actionText, string targetName = "")
    {
        if (interactionPanel == null) return;
        
        interactionPanel.SetActive(true);
        if (promptText != null) promptText.text = actionText;
        if (targetNameText != null) targetNameText.text = targetName;
    }

    public void HidePrompt()
    {
        if (interactionPanel != null) interactionPanel.SetActive(false);
        SetProgress(0);
    }

    public void SetProgress(float progress)
    {
        if (progressCircle != null)
        {
            progressCircle.fillAmount = Mathf.Clamp01(progress);
        }
    }
}
