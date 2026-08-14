using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Core.Enums;
using Core.QuestSystem;

/// <summary>
/// Gestiona un puzzle de secuencia (estilo Simon Says) con velas.
/// El puzzle se completa tras superar X rondas de dificultad creciente.
/// </summary>
public class PuzzleSequenceManager : NetworkBehaviour
{
    [Header("Identificadores de Misión (Modular)")]
    [Tooltip("Material considerado para avanzar la misión al resolver el puzzle.")]
    public MaterialType materialPuzzle = MaterialType.EsenciaAntigua;
    public ZoneID zonaPuzzle = ZoneID.Altar;
    
    [Tooltip("Lista de velas que forman parte del puzzle (mínimo 4 recomendadas).")]
    public List<PuzzleCandle> velas = new List<PuzzleCandle>();
    
    [Tooltip("Cuántas rondas (longitud de secuencia) hay que completar para ganar.")]
    public int totalRondas = 3;
    
    [Tooltip("Tiempo entre el encendido de una vela y la siguiente en la muestra.")]
    public float delayEntreFlashes = 0.6f;

    [Header("Estado (Solo Server)")]
    private List<int> secuenciaGenerada = new List<int>();
    private List<int> secuenciaUsuario = new List<int>();
    private int rondaActual = 0;
    private bool mostrandoSecuencia = false;
    private bool puzzleCompletado = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Por ahora registramos el manager en las velas
            for (int i = 0; i < velas.Count; i++)
            {
                velas[i].indexEnPuzzle = i;
                velas[i].manager = this;
            }
        }
    }

    /// <summary>Llamado por el primer jugador que interactúa con el puzzle.</summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void IniciarPuzzleServerRpc()
    {
        if (puzzleCompletado || mostrandoSecuencia) return;
        
        Debug.Log("[Puzzle] Generando secuencia inicial...");
        rondaActual = 0;
        secuenciaGenerada.Clear();
        GenerarSiguienteRonda();
    }

    private void GenerarSiguienteRonda()
    {
        rondaActual++;
        // Añadimos un paso más a la secuencia
        secuenciaGenerada.Add(Random.Range(0, velas.Count));
        secuenciaUsuario.Clear();
        
        StartCoroutine(MostrarSecuenciaRutina());
    }

    private IEnumerator MostrarSecuenciaRutina()
    {
        mostrandoSecuencia = true;
        SetVelasInteractivas(false);
        
        yield return new WaitForSeconds(1.5f); // Tiempo de preparación

        foreach (int index in secuenciaGenerada)
        {
            // Avisamos a las velas para que brillen
            velas[index].EncenderClientRpc(true);
            yield return new WaitForSeconds(delayEntreFlashes);
            velas[index].EncenderClientRpc(false);
            yield return new WaitForSeconds(delayEntreFlashes / 3f);
        }

        mostrandoSecuencia = false;
        SetVelasInteractivas(true);
        Debug.Log($"[Puzzle] Ronda {rondaActual}. ¡Esperando secuencia del jugador!");
    }

    private void SetVelasInteractivas(bool estado)
    {
        foreach (var v in velas) v.puedeInteractuar.Value = estado;
    }

    public void RecibirInputVela(int index, ulong playerClientId)
    {
        if (!IsServer || mostrandoSecuencia || puzzleCompletado) return;

        // Feedback visual para el jugador
        velas[index].FlashLocalClientRpc();
        
        secuenciaUsuario.Add(index);

        // Validar paso
        int pasoActual = secuenciaUsuario.Count - 1;
        if (secuenciaUsuario[pasoActual] != secuenciaGenerada[pasoActual])
        {
            Debug.Log("[Puzzle] SECUENCIA INCORRECTA. Reiniciando...");
            FallarPuzzle();
            return;
        }

        // ¿Terminó la ronda actual?
        if (secuenciaUsuario.Count == secuenciaGenerada.Count)
        {
            if (rondaActual >= totalRondas)
            {
                CompletarPuzzle(playerClientId);
            }
            else
            {
                Debug.Log("[Puzzle] Ronda superada. Siguiente...");
                GenerarSiguienteRonda();
            }
        }
    }

    private void FallarPuzzle()
    {
        // Feedback de fallo (todas parpadean rojo por ejemplo, o solo sonido)
        secuenciaGenerada.Clear();
        rondaActual = 0;
        puzzleCompletado = false;
        GenerarSiguienteRonda();
    }

    private void CompletarPuzzle(ulong clientId)
    {
        puzzleCompletado = true;
        SetVelasInteractivas(false);
        Debug.Log("[Puzzle] ¡Misión de Velas Completada con éxito!");

        // Notificar al sistema de misiones del jugador que lo logró
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            PlayerQuestTracker tracker = client.PlayerObject.GetComponent<PlayerQuestTracker>();
            if (tracker != null)
            {
                tracker.ProcessStepServerRpc(materialPuzzle, zonaPuzzle, transform.position);
            }
        }
        
        // Dejar las velas encendidas como decoración de victoria
        foreach (var v in velas) v.EncenderClientRpc(true);
    }
}
