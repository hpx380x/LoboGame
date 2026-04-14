using UnityEngine;
using TMPro;

public class MinigameTeclado : MinigameBase
{
    [Header("Referencias UI")]
    public TextMeshProUGUI textoCodigoObjetivo;
    public TextMeshProUGUI textoEntrada;
    
    [Header("Configuración")]
    public int longitudCodigo = 4;

    private string codigoSecreto = "";
    private string codigoEscrito = "";

    public override void SetupMinigame(TaskPoint origen)
    {
        base.SetupMinigame(origen);
        GenerarNuevoCodigo();
    }

    private void GenerarNuevoCodigo()
    {
        codigoSecreto = "";
        for (int i = 0; i < longitudCodigo; i++)
        {
            codigoSecreto += Random.Range(1, 10).ToString(); // Digitos del 1 al 9
        }
        
        if (textoCodigoObjetivo != null) textoCodigoObjetivo.text = codigoSecreto;
        codigoEscrito = "";
        ActualizarTextoEntrada();
    }

    /// <summary>
    /// Los botones de la UI (0-9) deben llamar a este método desde su evento OnClick.
    /// Pasando el número correspondiente.
    /// </summary>
    public void PulsarBoton(string numero)
    {
        if (!isMinigameActive) return;

        codigoEscrito += numero;
        ActualizarTextoEntrada();

        // Si llegó a la longitud esperada
        if (codigoEscrito.Length == codigoSecreto.Length)
        {
            if (codigoEscrito == codigoSecreto)
            {
                // Éxito
                if (textoEntrada != null) textoEntrada.color = Color.green;
                Invoke(nameof(CompletarExito), 0.5f); // Pequeño delay para ver el éxito
            }
            else
            {
                // Fallo
                if (textoEntrada != null) textoEntrada.color = Color.red;
                Invoke(nameof(ReiniciarFallo), 0.5f);
            }
        }
    }

    public void PulsarBorrar()
    {
        if (codigoEscrito.Length > 0 && isMinigameActive)
        {
            codigoEscrito = codigoEscrito.Substring(0, codigoEscrito.Length - 1);
            ActualizarTextoEntrada();
        }
    }

    private void ReiniciarFallo()
    {
        if (textoEntrada != null) textoEntrada.color = Color.white;
        GenerarNuevoCodigo(); // Cambia la password para evitar fuerza bruta ciega
    }

    private void ActualizarTextoEntrada()
    {
        if (textoEntrada != null)
        {
            textoEntrada.text = codigoEscrito.PadRight(longitudCodigo, '_');
        }
    }
}
