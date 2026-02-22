using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class AutomatedCanvasSeparationModifier
{
    static AutomatedCanvasSeparationModifier()
    {
        EditorApplication.delayCall += ExecuteModification;
    }

    private static void ExecuteModification()
    {
        string markerPath = "Assets/Editor/AutomatedCanvasSeparationRun.txt";
        
        // Evitamos que se ejecute en bucle tras cada compilación futura
        if (System.IO.File.Exists(markerPath)) return;

        bool sceneDirty = false;

        // 1. Crear el nuevo Canvas_GameManager si no existe
        GameObject newCanvasObj = GameObject.Find("Canvas_GameManager");
        if (newCanvasObj == null)
        {
            newCanvasObj = new GameObject("Canvas_GameManager");
            
            // Componentes vitales de UI
            Canvas canvas = newCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = newCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            newCanvasObj.AddComponent<GraphicRaycaster>();
            
            sceneDirty = true;
            Debug.Log("[AutomatedCanvasSeparation] Nuevo 'Canvas_GameManager' creado con su CanvasScaler y GraphicRaycaster.");
        }

        // 2. Localizar el botón de Inicio de Partida
        GameObject startButton = GameObject.Find("StartGameButton");
        if (startButton != null)
        {
            // 3. Moverlo al nuevo Canvas (separándolo de la jerarquía del LobbyUI que se apaga)
            if (startButton.transform.parent != newCanvasObj.transform)
            {
                // Movemos el botón. (false) hace que no cambie su tamaño y proporciones relativas al nuevo Canvas
                startButton.transform.SetParent(newCanvasObj.transform, false); 
                
                // Re-anclamos la UI para que siga igual de bonita abajo al centro
                RectTransform rect = startButton.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = new Vector2(0, 50);
                }

                sceneDirty = true;
                Debug.Log("[AutomatedCanvasSeparation] ¡Botón 'StartGameButton' rescatado y movido al Canvas_GameManager de forma segura!");
            }
        }
        else
        {
            Debug.LogWarning("[AutomatedCanvasSeparation] No se encontró un botón llamado 'StartGameButton'. Verifica que exista en la jerarquía.");
        }

        // 4. Guardado seguro usando la API de Unity
        if (sceneDirty)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        System.IO.File.WriteAllText(markerPath, "Completado");
    }
}
