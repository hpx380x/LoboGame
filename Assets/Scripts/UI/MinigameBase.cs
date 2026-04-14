using UnityEngine;

public abstract class MinigameBase : MonoBehaviour
{
    protected TaskPoint taskPointVinculado;
    protected bool isMinigameActive = false;

    /// <summary>
    /// El TaskPoint llama a esto al instanciar el Canvas del minijuego.
    /// Inicia el juego, bloquea el jugador y libera el ratón.
    /// </summary>
    public virtual void SetupMinigame(TaskPoint origen)
    {
        taskPointVinculado = origen;
        isMinigameActive = true;

        // Liberar el ratón para poder jugar en el panel 2D
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Desactivar el movimiento temporalmente
        BloquearMovimientoJugador(true);
    }

    protected virtual void Update()
    {
        if (!isMinigameActive) return;

        // Si el jugador aprieta ESCAPE o la tecla asignada para cancelar
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CerrarYFallar("Minijuego cancelado manualmente.");
        }
        
        // El TaskPoint ya se encarga de vigilar la proximidad y llamar a CancelarDesdeTaskPoint si nos alejamos
    }

    /// <summary>
    /// Método seguro para apagar el movimiento (ThirdPersonController)
    /// </summary>
    private void BloquearMovimientoJugador(bool bloquear)
    {
        if (Unity.Netcode.NetworkManager.Singleton.LocalClient == null) return;
        var pObject = Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject;
        if (pObject != null)
        {
            var tpc = pObject.GetComponent("ThirdPersonController") as MonoBehaviour;
            if (tpc != null) tpc.enabled = !bloquear;

            // Bloquear/desbloquear input de cámara del StarterAssets
            var inputs = pObject.GetComponent("StarterAssetsInputs") as MonoBehaviour;
            if (inputs != null) 
            {
                var type = inputs.GetType();
                var cursorInputProp = type.GetField("cursorInputForLook");
                var cursorLockedProp = type.GetField("cursorLocked");
                
                if (cursorInputProp != null) cursorInputProp.SetValue(inputs, !bloquear);
                
                // Si desbloqueamos movimiento, volvemos a capturar ratón
                if (!bloquear && cursorLockedProp != null) 
                {
                    cursorLockedProp.SetValue(inputs, true);
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }

    /// <summary>
    /// Cuando las condiciones del minijuego interno se cumplen, llama a esto.
    /// </summary>
    public void CompletarExito()
    {
        if (!isMinigameActive) return;
        isMinigameActive = false;

        BloquearMovimientoJugador(false);
        taskPointVinculado.MinijuegoResueltoPorUI();

        Destroy(gameObject); // Autodestruir el canvas del minijuego
    }

    /// <summary>
    /// Llama a esto si cerraste el panel o apretaste cancelar.
    /// </summary>
    public void CerrarYFallar(string motivo)
    {
        if (!isMinigameActive) return;
        isMinigameActive = false;

        BloquearMovimientoJugador(false);
        taskPointVinculado.MinijuegoFalladoPorUI(motivo);

        Destroy(gameObject);
    }

    /// <summary>
    /// Si el TaskPoint detecta que nos alejamos físicamente o morimos, llama a esto desde fuera.
    /// </summary>
    public void ForzarCierreDesdeExterno()
    {
        if (!isMinigameActive) return;
        isMinigameActive = false;

        BloquearMovimientoJugador(false);
        Destroy(gameObject);
    }
}
