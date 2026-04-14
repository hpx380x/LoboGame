using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class TapestryMinigame : MinigameBase
{
    [Header("Configuración Visual")]
    public VisualTreeAsset uxmlDocument;
    public PanelSettings panelSettings;

    private VisualElement columnsContainer;
    private VisualElement threadsLayer;
    private Label progressLabel;
    private Label difficultyLabel;

    // --- Colores Originales ---
    private readonly string[] hexColors = { "#991B1B", "#1E40AF", "#854D0E", "#6B21A8" };
    private readonly Color[] threadColors = { 
        new Color(0.6f, 0.1f, 0.1f), // Rojo
        new Color(0.12f, 0.25f, 0.68f), // Azul
        new Color(0.52f, 0.3f, 0.05f), // Dorado
        new Color(0.42f, 0.13f, 0.66f)  // Púrpura
    };

    // --- Estado del Juego ---
    private int numColumns = 2;
    private List<List<string>> columnsData = new List<List<string>>();
    private List<VisualElement> spoolElements = new List<VisualElement>();
    
    // Arrastre activo
    private bool isDragging = false;
    private Vector2 startPos;
    private Vector2 currentMousePos;
    private int startCol = -1;
    private int startIdx = -1;
    private Color activeColor;

    // --- Audio Procedural ---
    private AudioSource audioSource;
    private float noiseVolume = 0f;
    private float targetNoiseVolume = 0f;

    public override void SetupMinigame(TaskPoint origen)
    {
        base.SetupMinigame(origen);
        
        // Cargar UI
        var root = GetComponent<UIDocument>().rootVisualElement;
        columnsContainer = root.Q<VisualElement>("ColumnsContainer");
        threadsLayer = root.Q<VisualElement>("ThreadsLayer");
        progressLabel = root.Q<Label>("ProgressLabel");
        difficultyLabel = root.Q<Label>("DifficultyLabel");

        // Configurar capas de dibujo
        threadsLayer.generateVisualContent += OnGenerateVisualContent;

        // Recuperar nivel guardado o iniciar
        numColumns = 2 + taskPointVinculado.tapestryLevel; // 2, 3 o 4
        
        InicializarNivel();
        
        // Setup Audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    private void InicializarNivel()
    {
        columnsContainer.Clear();
        columnsData.Clear();
        spoolElements.Clear();

        difficultyLabel.text = numColumns == 2 ? "DIFICULTAD: FACIL" : (numColumns == 3 ? "DIFICULTAD: AVANZADO" : "DIFICULTAD: DIFICIL");
        progressLabel.text = $"TAPESTRY {taskPointVinculado.tapestryLevel + 1}/3";

        for (int c = 0; c < numColumns; c++)
        {
            var colData = hexColors.OrderBy(x => Random.value).ToList();
            columnsData.Add(colData);

            var colElement = new VisualElement();
            colElement.AddToClassList("spool-column");
            columnsContainer.Add(colElement);

            for (int i = 0; i < colData.Count; i++)
            {
                var spool = new VisualElement();
                spool.AddToClassList("spool-node");
                
                var dot = new VisualElement();
                dot.AddToClassList("spool-color-dot");
                Color col;
                ColorUtility.TryParseHtmlString(colData[i], out col);
                dot.style.backgroundColor = col;
                
                spool.Add(dot);
                colElement.Add(spool);

                // Eventos de interacción
                int colIdx = c;
                int threadIdx = i;
                spool.RegisterCallback<PointerDownEvent>(evt => OnSpoolPointerDown(evt, colIdx, threadIdx));
                spool.RegisterCallback<PointerEnterEvent>(evt => OnSpoolPointerEnter(evt, colIdx, threadIdx));
                
                spoolElements.Add(spool);
            }
        }

        threadsLayer.MarkDirtyRepaint();
    }

    private void OnSpoolPointerDown(PointerDownEvent evt, int col, int idx)
    {
        if (col >= numColumns - 1) return; // No se puede empezar desde la última columna
        
        // Verificar si este ya está conectado a la derecha
        bool alreadyConnected = taskPointVinculado.tapestryState.Any(conn => conn.phase == col && conn.fromIdx == idx);
        if (alreadyConnected) return;

        // Si no es la primera columna, debe tener conexión desde la izquierda
        if (col > 0)
        {
            bool hasLeftConn = taskPointVinculado.tapestryState.Any(conn => conn.phase == col - 1 && conn.toIdx == idx);
            if (!hasLeftConn) return;
        }

        isDragging = true;
        startCol = col;
        startIdx = idx;
        startPos = (Vector2)evt.localPosition + (Vector2)spoolElements[col * hexColors.Length + idx].worldBound.position - (Vector2)threadsLayer.worldBound.position;
        currentMousePos = startPos;
        
        ColorUtility.TryParseHtmlString(columnsData[col][idx], out activeColor);
        
        threadsLayer.CapturePointer(evt.pointerId);
        targetNoiseVolume = 0.2f;
    }

    private void OnSpoolPointerEnter(PointerEnterEvent evt, int col, int idx)
    {
        if (!isDragging) return;
        if (col != startCol + 1) return; // Solo conectar a la siguiente columna

        string startColor = columnsData[startCol][startIdx];
        string endColor = columnsData[col][idx];

        if (startColor == endColor)
        {
            // ¡Conexión exitosa!
            taskPointVinculado.tapestryState.Add(new TaskPoint.TapestryConnectionData {
                phase = startCol,
                fromIdx = startIdx,
                toIdx = idx,
                colorHex = startColor
            });

            PlayTones(true);
            isDragging = false;
            threadsLayer.ReleasePointer(evt.pointerId);
            targetNoiseVolume = 0f;
            
            ValidarVictoria();
        }
    }

    protected override void Update()
    {
        base.Update();
        if (!isMinigameActive) return;

        if (isDragging)
        {
            // Seguir ratón (ajustado a la capa local)
            var root = threadsLayer.panel.visualTree;
            // Nota: En UI Toolkit real, evt.localPosition suele ser suficiente si registramos en el layer.
            // Para simplificar aquí simulamos el movimiento en Update si hay captura.
        }

        // Suavizado de volumen de audio
        noiseVolume = Mathf.Lerp(noiseVolume, targetNoiseVolume, Time.deltaTime * 10f);
    }

    public System.Action OnMinigameWon;

    private void ValidarVictoria()
    {
        // ... (lógica de validación omitida por brevedad en el pensamiento, pero mantenida en el archivo)
        bool complete = hexColors.All(color => {
            int currentCol = 0;
            int currentIdx = columnsData[0].IndexOf(color);
            if (currentIdx == -1) return false;

            while (currentCol < numColumns - 1)
            {
                var conn = taskPointVinculado.tapestryState.FirstOrDefault(c => c.phase == currentCol && c.fromIdx == currentIdx);
                if (conn.colorHex == null) return false;
                currentIdx = conn.toIdx;
                currentCol++;
            }
            return true;
        });

        if (complete)
        {
            taskPointVinculado.tapestryLevel++;
            taskPointVinculado.tapestryState.Clear();

            if (taskPointVinculado.tapestryLevel >= 3)
            {
                PlayTones(true);
                OnMinigameWon?.Invoke(); // Avisar al mundo que ganamos
                Invoke(nameof(CompletarExito), 1.6f); // Esperar a que la animación termine
            }
            else
            {
                PlayTones(true);
                InicializarNivel();
            }
        }
        else
        {
            threadsLayer.MarkDirtyRepaint();
        }
    }

    // --- Dibujado Dinámico ---
    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        var painter = mgc.painter2D;
        painter.lineCap = LineCap.Round;
        painter.lineJoin = LineJoin.Round;

        // 1. Dibujar conexiones guardadas
        foreach (var conn in taskPointVinculado.tapestryState)
        {
            if (conn.phase >= columnsData.Count - 1) continue;
            
            Vector2 p1 = GetSpoolCenter(conn.phase, conn.fromIdx);
            Vector2 p2 = GetSpoolCenter(conn.phase + 1, conn.toIdx);
            
            Color c;
            ColorUtility.TryParseHtmlString(conn.colorHex, out c);
            DrawThread(painter, p1, p2, c);
        }

        // 2. Dibujar hilo activo
        if (isDragging)
        {
            // Obtenemos la posición actual del puntero del panel
            Vector2 mousePos = threadsLayer.WorldToLocal(Input.mousePosition);
            // Invertir Y de Unity UI vs Input
            mousePos.y = threadsLayer.layout.height - mousePos.y; 
            
            DrawThread(painter, startPos, mousePos, activeColor);
            threadsLayer.MarkDirtyRepaint(); // Seguir repintando mientras arrastramos
        }
    }

    private void DrawThread(Painter2D p, Vector2 p1, Vector2 p2, Color color)
    {
        p.strokeColor = color;
        p.lineWidth = 10;
        
        p.BeginPath();
        p.MoveTo(p1);
        // Curva tipo catenaria (bolsa)
        Vector2 mid = (p1 + p2) * 0.5f;
        mid.y += Mathf.Min(30, Vector2.Distance(p1, p2) * 0.15f);
        p.QuadraticCurveTo(mid, p2);
        p.Stroke();

        // Detalle de trenzado (sombra)
        p.strokeColor = new Color(0, 0, 0, 0.3f);
        p.lineWidth = 2;
        p.Stroke();
    }

    private Vector2 GetSpoolCenter(int col, int idx)
    {
        if (col < 0 || col >= columnsData.Count) return Vector2.zero;
        var el = spoolElements[col * hexColors.Length + idx];
        var rect = el.layout;
        var worldPos = el.worldBound.position;
        var localPos = threadsLayer.WorldToLocal(worldPos);
        return localPos + new Vector2(rect.width * 0.5f, rect.height * 0.5f);
    }

    // --- Audio Procedural ---
    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (!isMinigameActive) return;

        for (int i = 0; i < data.Length; i += channels)
        {
            // Ruido de fricción (White noise filtrado)
            float noise = (Random.value * 2f - 1f) * noiseVolume;
            
            for (int j = 0; j < channels; j++)
            {
                data[i + j] = noise;
            }
        }
    }

    private void PlayTones(bool victory)
    {
        // Aquí podríamos disparar un AudioSource con un clip real si existiera,
        // pero por ahora usamos el feedback de la UI.
        Debug.Log(victory ? "[Tapestry] Nivel Completado" : "[Tapestry] Fallo");
    }
}
