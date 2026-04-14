using UnityEngine;
using UnityEngine.UI;

public class MinigameDescarga : MinigameBase
{
    [Header("UI Componentes")]
    public Button botonDescarga;
    public Slider barraProgreso;
    
    [Header("Configuración")]
    public float tiempoDescarga = 4f;
    private float tiempoActual = 0f;
    private bool descargando = false;

    public override void SetupMinigame(TaskPoint origen)
    {
        base.SetupMinigame(origen);
        
        // Reset UI
        if (barraProgreso != null) barraProgreso.value = 0f;
        
        if (botonDescarga != null)
        {
            botonDescarga.onClick.RemoveAllListeners();
            botonDescarga.onClick.AddListener(IniciarDescarga);
        }
    }

    private void IniciarDescarga()
    {
        if (descargando) return;
        descargando = true;
        if (botonDescarga != null) botonDescarga.interactable = false; // Desactivar botón mientras baja
    }

    protected override void Update()
    {
        base.Update();

        if (descargando && isMinigameActive)
        {
            tiempoActual += Time.deltaTime;
            if (barraProgreso != null)
            {
                barraProgreso.value = Mathf.Clamp01(tiempoActual / tiempoDescarga);
            }

            if (tiempoActual >= tiempoDescarga)
            {
                CompletarExito();
            }
        }
    }
}
