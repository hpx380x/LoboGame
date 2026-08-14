using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

namespace Core.EditorTools
{
    public class StyleProgressBar : EditorWindow
    {
        [MenuItem("Tools/Estilizar Barra de Progreso")]
        public static void StyleBar()
        {
            // Asegurarse de tener cargada la escena Gameplay
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name != "Scene_Gameplay")
            {
                Debug.LogWarning("[StyleBar] Debes tener abierta la escena 'Scene_Gameplay' para estilizar la barra.");
                return;
            }

            // Buscar BarraProgreso_Minijuegos
            GameObject barObj = GameObject.Find("Canvas_Gameplay/BarraProgreso_Minijuegos");
            if (barObj == null)
            {
                // Fallback por si la ruta difiere ligeramente
                barObj = GameObject.Find("BarraProgreso_Minijuegos");
            }

            if (barObj == null)
            {
                Debug.LogError("[StyleBar] No se encontró el GameObject 'BarraProgreso_Minijuegos' en la escena.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(barObj, "Style Progress Bar");

            // 1. Configurar RectTransform de la Barra
            RectTransform barRt = barObj.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 0f);
            barRt.anchorMax = new Vector2(0.5f, 0f);
            barRt.pivot = new Vector2(0.5f, 0.5f);
            barRt.sizeDelta = new Vector2(380f, 22f); // Más estilizada y moderna
            barRt.anchoredPosition = new Vector2(0f, 150f); // Centrado abajo

            // 2. Estilizar Background
            Transform bgTrans = barObj.transform.Find("Background");
            if (bgTrans != null)
            {
                Undo.RegisterCompleteObjectUndo(bgTrans.gameObject, "Style BG");
                Image bgImg = bgTrans.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.color = new Color(0.06f, 0.06f, 0.08f, 0.85f); // Fondo muy oscuro místico
                }

                // Añadir Outline dorado al Background para estética premium
                Outline outline = bgTrans.GetComponent<Outline>();
                if (outline == null) outline = bgTrans.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.85f, 0.65f, 0.15f, 0.35f); // Borde dorado difuso
                outline.effectDistance = new Vector2(1.5f, 1.5f);
            }

            // 3. Estilizar Fill Area y Fill
            Transform fillAreaTrans = barObj.transform.Find("Fill Area");
            if (fillAreaTrans != null)
            {
                RectTransform fillAreaRt = fillAreaTrans.GetComponent<RectTransform>();
                fillAreaRt.offsetMin = Vector2.zero;
                fillAreaRt.offsetMax = Vector2.zero;
            }

            Transform fillTrans = barObj.transform.Find("Fill Area/Fill");
            if (fillTrans != null)
            {
                Undo.RegisterCompleteObjectUndo(fillTrans.gameObject, "Style Fill");
                Image fillImg = fillTrans.GetComponent<Image>();
                if (fillImg != null)
                {
                    // Cambiar a un color de gradiente cian místico brillante
                    fillImg.color = new Color(0f, 0.9f, 1f, 1f); 
                }

                // Añadir un Shadow/Glow interno al Fill para efecto neón
                Shadow fillShadow = fillTrans.GetComponent<Shadow>();
                if (fillShadow == null) fillShadow = fillTrans.gameObject.AddComponent<Shadow>();
                fillShadow.effectColor = new Color(0f, 0.9f, 1f, 0.4f);
                fillShadow.effectDistance = new Vector2(1f, -1f);
            }

            // 4. Crear/Estilizar Etiqueta de Texto (Label)
            Transform labelTrans = barObj.transform.Find("TextLabel");
            GameObject labelObj;
            if (labelTrans == null)
            {
                labelObj = new GameObject("TextLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObj.transform.SetParent(barObj.transform, false);
            }
            else
            {
                labelObj = labelTrans.gameObject;
            }

            Undo.RegisterCompleteObjectUndo(labelObj, "Style Label");
            
            RectTransform labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;
            labelRt.anchoredPosition = Vector2.zero;

            TextMeshProUGUI labelText = labelObj.GetComponent<TextMeshProUGUI>();
            labelText.text = "PROGRESO DE ACCIÓN";
            labelText.fontSize = 11f;
            labelText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.characterSpacing = 1.5f;

            // Añadir Shadow al texto para que resalte
            Shadow textShadow = labelObj.GetComponent<Shadow>();
            if (textShadow == null) textShadow = labelObj.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            textShadow.effectDistance = new Vector2(1f, -1f);

            // Guardar cambios en la escena
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[StyleBar] ¡BarraProgreso_Minijuegos estilizada con éxito y escena guardada!");
        }
    }
}
