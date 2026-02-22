using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

[InitializeOnLoad]
public class AutomatedUICentering
{
    static AutomatedUICentering()
    {
        EditorApplication.delayCall += Execute;
    }

    private static void Execute()
    {
        string marker = "Assets/Editor/AutomatedUICenteringRun.txt";
        if (System.IO.File.Exists(marker)) return;

        bool changed = false;

        // Buscamos todos los Canvas y forzamos que se escalen correctamente con la pantalla
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in canvases)
        {
            CanvasScaler scaler = c.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                changed = true;
            }
        }

        // Centrar botón Host
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button b in buttons)
        {
            RectTransform rt = b.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Si es el botón de Host
                if (b.gameObject.name.ToLower().Contains("host"))
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-150, 0); // Un poco a la izquierda
                    changed = true;
                }
                // Si es el botón Client
                else if (b.gameObject.name.ToLower().Contains("client"))
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(150, 0); // Un poco a la derecha
                    changed = true;
                }
                // Si es el Iniciar Partida
                else if (b.gameObject.name.ToLower().Contains("start") || b.gameObject.name.ToLower().Contains("iniciar"))
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0, -100); // Abajo al centro
                    changed = true;
                }
            }
        }

        if (changed)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("<color=green>[UICentering] Todos los botones han sido forzados al centro de la pantalla para evitar que desaparezcan en ventanas deformadas.</color>");
        }

        System.IO.File.WriteAllText(marker, "Done");
    }
}
