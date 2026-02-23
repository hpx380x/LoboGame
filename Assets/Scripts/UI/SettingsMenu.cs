using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [Header("Elementos de UI")]
    [Tooltip("El menú desplegable para las resoluciones")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [Tooltip("La casilla de verificación para Pantalla Completa (Toggle)")]
    [SerializeField] private Toggle fullscreenToggle;

    private Resolution[] resoluciones;

    private void Start()
    {
        // 1. Obtener resoluciones compatibles con el monitor del jugador
        resoluciones = Screen.resolutions;

        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < resoluciones.Length; i++)
            {
                // Formateamos el texto ej: "1920 x 1080"
                string option = resoluciones[i].width + " x " + resoluciones[i].height;
                options.Add(option);

                // Buscamos cuál es la resolución actual para dejarla marcada
                if (resoluciones[i].width == Screen.currentResolution.width &&
                    resoluciones[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();

            // Avisar cuando el jugador cambie la opción
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }

        if (fullscreenToggle != null)
        {
            // Marcar la casilla si el juego ya está en pantalla completa
            fullscreenToggle.isOn = Screen.fullScreen;
            
            // Avisar cuando el jugador haga clic en la casilla
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    private void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resoluciones[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        Debug.Log($"[Ajustes] Resolución cambiada a {resolution.width}x{resolution.height}");
    }

    private void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        Debug.Log($"[Ajustes] Pantalla Completa: {isFullscreen}");
    }
}