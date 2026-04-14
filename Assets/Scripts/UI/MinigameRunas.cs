using UnityEngine;
using UnityEngine.UI;

public class MinigameRunas : MinigameBase
{
    [Header("Sprites de Runas")]
    public Sprite[] runasDisponibles;

    [Header("UI - Ranuras Actuales")]
    public Image[] slotIcons;

    [Header("UI - Patrón Objetivo")]
    public Image[] targetIcons;

    public override void SetupMinigame(TaskPoint origen)
    {
        base.SetupMinigame(origen);

        // Si es la primera vez que se abre esta tarea específica, generamos el patrón
        if (!taskPointVinculado.minigameInitialized)
        {
            for (int i = 0; i < 3; i++)
            {
                taskPointVinculado.runeTarget[i] = Random.Range(0, runasDisponibles.Length);
                // Ponemos las runas del jugador en una posición aleatoria diferente
                taskPointVinculado.runeState[i] = (taskPointVinculado.runeTarget[i] + Random.Range(1, runasDisponibles.Length)) % runasDisponibles.Length;
            }
            taskPointVinculado.minigameInitialized = true;
        }

        ActualizarVisuales();
    }

    /// <summary>
    /// Los botones de las ranuras deben llamar a esto.
    /// Slot 0, 1 o 2.
    /// </summary>
    public void RotarRuna(int slot)
    {
        if (!isMinigameActive || slot < 0 || slot > 2) return;

        // Rotar al siguiente índice
        taskPointVinculado.runeState[slot] = (taskPointVinculado.runeState[slot] + 1) % runasDisponibles.Length;
        
        ActualizarVisuales();
        ComprobarVictoria();
    }

    private void ActualizarVisuales()
    {
        if (runasDisponibles.Length == 0) return;

        for (int i = 0; i < 3; i++)
        {
            // Actualizar lo que el jugador tiene puesto
            if (i < slotIcons.Length)
                slotIcons[i].sprite = runasDisponibles[taskPointVinculado.runeState[i]];

            // Actualizar el objetivo a seguir
            if (i < targetIcons.Length)
                targetIcons[i].sprite = runasDisponibles[taskPointVinculado.runeTarget[i]];
        }
    }

    private void ComprobarVictoria()
    {
        bool coincide = true;
        for (int i = 0; i < 3; i++)
        {
            if (taskPointVinculado.runeState[i] != taskPointVinculado.runeTarget[i])
            {
                coincide = false;
                break;
            }
        }

        if (coincide)
        {
            // Pequeño feedback visual o sonido antes de cerrar
            foreach(var img in slotIcons) img.color = Color.green;
            Invoke(nameof(CompletarExito), 0.6f);
        }
    }
}
