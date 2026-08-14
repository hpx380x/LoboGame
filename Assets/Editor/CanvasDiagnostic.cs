using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public class CanvasDiagnostic
{
    static CanvasDiagnostic()
    {
        EditorApplication.delayCall += Execute;
    }

    private static void Execute()
    {
        string marker = "Assets/Editor/CanvasDiagnosticRun.txt";
        if (System.IO.File.Exists(marker)) return;

        Debug.Log("==== [DIAGNÓSTICO DE BOTONES] Empezando ====");

        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude);
        foreach (Button b in buttons)
        {
            Debug.Log($"Botón: {b.gameObject.name} | Está en el Canvas: {(b.GetComponentInParent<Canvas>() != null ? b.GetComponentInParent<Canvas>().name : "NULO")}");

            // Contar el número de eventos persistentes que tiene asignados en el Inspector (arrastrados a mano)
            int eventCount = b.onClick.GetPersistentEventCount();
            Debug.Log($"  -> Tiene {eventCount} eventos asignados en el Inspector.");
            
            for (int i = 0; i < eventCount; i++)
            {
                var target = b.onClick.GetPersistentTarget(i);
                var methodName = b.onClick.GetPersistentMethodName(i);
                Debug.Log($"     - Evento {i}: Target={target?.name} Método={methodName}");
                
                // Si el Host o Client button tiene el GameManager.StartGame asignado por error, lo borramos mágicamente
                if ((b.gameObject.name.ToLower().Contains("host") || b.gameObject.name.ToLower().Contains("client")) && methodName == "StartGame")
                {
                    Debug.LogWarning($"[CORRECCIÓN] ¡Borrando llamada a StartGame del botón {b.gameObject.name}!");
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(b.onClick, i);
                    EditorUtility.SetDirty(b);
                }
            }
        }

        Debug.Log("==== [DIAGNÓSTICO DE BOTONES] Terminado ====");
        System.IO.File.WriteAllText(marker, "Done");
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }
}

