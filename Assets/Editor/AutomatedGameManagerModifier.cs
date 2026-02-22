using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class AutomatedGameManagerModifier
{
    static AutomatedGameManagerModifier()
    {
        EditorApplication.delayCall += ExecuteModification;
    }

    private static void ExecuteModification()
    {
        string markerPath = "Assets/Editor/AutomatedGameManagerRun.txt";
        
        if (System.IO.File.Exists(markerPath)) return;

        bool sceneDirty = false;

        // 1. Instalar el GameManager en la Escena
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<NetworkObject>();
            gmObj.AddComponent<GameManager>();
            sceneDirty = true;
            Debug.Log("[AutomatedGameManager] GameManager creado.");
        }

        // 2. Instalar el Botón en un Canvas Existente
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            // Creamos un botón "Start Game" en la parte baja de la pantalla
            GameObject buttonObj = DefaultControls.CreateButton(new DefaultControls.Resources());
            buttonObj.name = "StartGameButton";
            buttonObj.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f); // Anclado abajo en el centro
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 50); // A 50 pixeles del borde inferior
            rect.sizeDelta = new Vector2(250, 60);

            // Cambiamos el color para que destaque (Verde o algo)
            Image img = buttonObj.GetComponent<Image>();
            img.color = new Color(0.2f, 0.8f, 0.2f); // Verde

            Text txt = buttonObj.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text = "INICIAR PARTIDA";
                txt.fontSize = 24;
                txt.fontStyle = FontStyle.Bold;
                txt.color = Color.white;
            }

            // Agregamos nuestro script mágico para que el botón interactúe con el GameManager sin arrastrar cosas
            buttonObj.AddComponent<GameUI>();
            sceneDirty = true;
            
            Debug.Log("[AutomatedGameManager] Botón INICIAR PARTIDA añadido al Canvas.");
        }
        else
        {
            Debug.LogError("[AutomatedGameManager] No se encontró un Canvas en la escena. ¡Por favor crea tu UI primero!");
        }

        if (sceneDirty)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        System.IO.File.WriteAllText(markerPath, "Completado");
    }
}
