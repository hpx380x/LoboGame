using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinigameSlidingPuzzle : MinigameBase
{
    [Header("Piezas del Puzzle (9)")]
    [Tooltip("El index 8 debe ser la pieza en blanco o un sprite vacío.")]
    public Sprite[] spritesPiezas;

    [Header("UI - Botones de la Rejilla")]
    public Image[] tiles;

    private int emptyIndex = 8; // La pieza que falta

    public override void SetupMinigame(TaskPoint origen)
    {
        base.SetupMinigame(origen);

        // Si no se ha inicializado esta tarea nunca, barajamos
        if (!taskPointVinculado.minigameInitialized)
        {
            // Empezamos con el estado resuelto
            for (int i = 0; i < 9; i++) taskPointVinculado.slidingPuzzleState[i] = i;
            
            BarajarPuzzle(100); // 100 movimientos aleatorios para asegurar que sea resoluble
            taskPointVinculado.minigameInitialized = true;
        }

        ActualizarUI();
    }

    /// <summary>
    /// Los botones de la rejilla (0-8) deben llamar a esto.
    /// </summary>
    public void ClickPieza(int index)
    {
        if (!isMinigameActive) return;

        // ¿Esta pieza está al lado del hueco vacío?
        if (EsAdyacente(index, GetEmptySlotIndex()))
        {
            Swap(index, GetEmptySlotIndex());
            ActualizarUI();
            CheckWin();
        }
    }

    private int GetEmptySlotIndex()
    {
        for (int i = 0; i < 9; i++)
        {
            if (taskPointVinculado.slidingPuzzleState[i] == emptyIndex) return i;
        }
        return 8;
    }

    private bool EsAdyacente(int a, int b)
    {
        int rowA = a / 3; int colA = a % 3;
        int rowB = b / 3; int colB = b % 3;
        
        return (Mathf.Abs(rowA - rowB) == 1 && colA == colB) || 
               (Mathf.Abs(colA - colB) == 1 && rowA == rowB);
    }

    private void Swap(int a, int b)
    {
        int temp = taskPointVinculado.slidingPuzzleState[a];
        taskPointVinculado.slidingPuzzleState[a] = taskPointVinculado.slidingPuzzleState[b];
        taskPointVinculado.slidingPuzzleState[b] = temp;
    }

    private void BarajarPuzzle(int movimientos)
    {
        int currentEmpty = 8;
        for (int i = 0; i < movimientos; i++)
        {
            List<int> validMoves = new List<int>();
            for (int j = 0; j < 9; j++)
            {
                if (EsAdyacente(j, currentEmpty)) validMoves.Add(j);
            }
            
            int moveToMake = validMoves[Random.Range(0, validMoves.Count)];
            Swap(moveToMake, currentEmpty);
            currentEmpty = moveToMake;
        }
    }

    private void ActualizarUI()
    {
        for (int i = 0; i < 9; i++)
        {
            int pieceId = taskPointVinculado.slidingPuzzleState[i];
            tiles[i].sprite = spritesPiezas[pieceId];
            
            // Opcional: Hacer invisible la pieza vacía
            tiles[i].color = (pieceId == emptyIndex) ? new Color(1,1,1,0) : Color.white;
        }
    }

    private void CheckWin()
    {
        bool win = true;
        for (int i = 0; i < 9; i++)
        {
            if (taskPointVinculado.slidingPuzzleState[i] != i)
            {
                win = false;
                break;
            }
        }

        if (win)
        {
            // Pequeño feedback visual
            foreach (var t in tiles) t.color = Color.white;
            Invoke(nameof(CompletarExito), 0.5f);
        }
    }
}
