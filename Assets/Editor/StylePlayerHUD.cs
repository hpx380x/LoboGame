using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

namespace Core.EditorTools
{
    public class StylePlayerHUD : EditorWindow
    {
        [MenuItem("Tools/Estilizar HUD del Jugador")]
        public static void StyleHUD()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name != "Scene_Gameplay")
            {
                Debug.LogWarning("[StylePlayerHUD] Abre la escena 'Scene_Gameplay' primero para aplicar el estilo.");
                return;
            }

            GameplayUI hud = Object.FindAnyObjectByType<GameplayUI>();
            if (hud == null)
            {
                Debug.LogError("[StylePlayerHUD] No se encontró el componente 'GameplayUI' en la escena.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(hud.gameObject, "Style Player HUD");

            // 1. Estilizar Fondo de Inventario (Mano)
            Image invBg = GetPrivateField<Image>(hud, "fondoInventario");
            if (invBg != null)
            {
                Undo.RegisterCompleteObjectUndo(invBg.gameObject, "Style Inventory BG");
                invBg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f); // Fondo oscuro premium
                
                // Outline dorado premium
                Outline outline = invBg.GetComponent<Outline>() ?? invBg.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.85f, 0.65f, 0.15f, 0.35f);
                outline.effectDistance = new Vector2(1.5f, 1.5f);

                // Posicionamiento más elegante (Abajo a la derecha)
                RectTransform invRt = invBg.GetComponent<RectTransform>();
                invRt.anchorMin = new Vector2(1f, 0f);
                invRt.anchorMax = new Vector2(1f, 0f);
                invRt.pivot = new Vector2(1f, 0f);
                invRt.sizeDelta = new Vector2(180f, 65f);
                invRt.anchoredPosition = new Vector2(-30f, 30f);
            }

            // 2. Estilizar Texto del Inventario
            TextMeshProUGUI invText = GetPrivateField<TextMeshProUGUI>(hud, "textoInventario");
            if (invText != null)
            {
                Undo.RegisterCompleteObjectUndo(invText.gameObject, "Style Inventory Text");
                invText.fontSize = 13.5f;
                invText.fontStyle = FontStyles.Bold;
                invText.color = Color.white;
                invText.alignment = TextAlignmentOptions.Center;

                RectTransform textRt = invText.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(10f, 5f);
                textRt.offsetMax = new Vector2(-10f, -25f);
            }

            // 3. Estilizar Texto de Monedas (Oro)
            TextMeshProUGUI goldText = GetPrivateField<TextMeshProUGUI>(hud, "textoMonedas");
            if (goldText != null)
            {
                Undo.RegisterCompleteObjectUndo(goldText.gameObject, "Style Gold Text");
                goldText.fontSize = 14f;
                goldText.fontStyle = FontStyles.Bold;
                goldText.color = new Color(1f, 0.85f, 0.2f, 1f); // Dorado brillante
                goldText.alignment = TextAlignmentOptions.Center;

                // Añadir Shadow para resaltar
                Shadow shadow = goldText.GetComponent<Shadow>() ?? goldText.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
                shadow.effectDistance = new Vector2(1f, -1f);

                RectTransform goldRt = goldText.GetComponent<RectTransform>();
                goldRt.anchorMin = new Vector2(0f, 0f);
                goldRt.anchorMax = new Vector2(1f, 0f);
                goldRt.pivot = new Vector2(0.5f, 0f);
                goldRt.sizeDelta = new Vector2(0f, 25f);
                goldRt.anchoredPosition = new Vector2(0f, 5f);
            }

            // 4. Estilizar Panel de Tareas (Task Panel)
            GameObject taskPanelObj = GetPrivateField<GameObject>(hud, "taskPanel");
            if (taskPanelObj != null)
            {
                Undo.RegisterCompleteObjectUndo(taskPanelObj, "Style Task Panel");
                
                // Fondo para la lista de tareas
                Image taskBg = taskPanelObj.GetComponent<Image>();
                if (taskBg == null) taskBg = taskPanelObj.AddComponent<Image>();
                taskBg.color = new Color(0.06f, 0.06f, 0.08f, 0.75f); // Placa oscura translúcida

                Outline taskOutline = taskPanelObj.GetComponent<Outline>() ?? taskPanelObj.AddComponent<Outline>();
                taskOutline.effectColor = new Color(1f, 1f, 1f, 0.08f); // Borde blanco sutil
                taskOutline.effectDistance = new Vector2(1.5f, 1.5f);

                RectTransform taskRt = taskPanelObj.GetComponent<RectTransform>();
                taskRt.anchorMin = new Vector2(0f, 1f);
                taskRt.anchorMax = new Vector2(0f, 1f);
                taskRt.pivot = new Vector2(0f, 1f);
                taskRt.sizeDelta = new Vector2(300f, 320f);
                taskRt.anchoredPosition = new Vector2(30f, -30f);
            }

            // 5. Estilizar Texto de Tareas (Task Text)
            TextMeshProUGUI taskTextObj = GetPrivateField<TextMeshProUGUI>(hud, "taskText");
            if (taskTextObj != null)
            {
                Undo.RegisterCompleteObjectUndo(taskTextObj.gameObject, "Style Task Text");
                taskTextObj.fontSize = 12.5f;
                taskTextObj.characterSpacing = 0.5f;
                taskTextObj.lineSpacing = 6f; // Mayor espacio entre líneas para legibilidad

                RectTransform textRt = taskTextObj.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(15f, 15f);
                textRt.offsetMax = new Vector2(-15f, -15f);
            }

            // 6. Estilizar Texto de Proximidad (Prompts de interacción)
            TextMeshProUGUI proxText = GetPrivateField<TextMeshProUGUI>(hud, "textoProximidad");
            if (proxText != null)
            {
                Undo.RegisterCompleteObjectUndo(proxText.gameObject, "Style Proximity Text");
                proxText.fontSize = 15f;
                proxText.fontStyle = FontStyles.Bold;
                proxText.color = new Color(0.95f, 0.95f, 1f, 1f);
                proxText.alignment = TextAlignmentOptions.Center;

                // Añadir un fondo dinámico al cartel de proximidad si no existe
                Image proxBg = proxText.GetComponentInChildren<Image>();
                if (proxBg == null)
                {
                    GameObject bgObj = new GameObject("LabelBG", typeof(RectTransform), typeof(Image));
                    bgObj.transform.SetParent(proxText.transform, false);
                    bgObj.transform.SetAsFirstSibling();
                    
                    proxBg = bgObj.GetComponent<Image>();
                    proxBg.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);
                    
                    Outline proxOutline = bgObj.AddComponent<Outline>();
                    proxOutline.effectColor = new Color(1f, 1f, 1f, 0.1f);
                    proxOutline.effectDistance = new Vector2(1f, 1f);

                    RectTransform bgRt = bgObj.GetComponent<RectTransform>();
                    bgRt.anchorMin = Vector2.zero;
                    bgRt.anchorMax = Vector2.one;
                    bgRt.sizeDelta = new Vector2(40f, 12f); // Márgenes adicionales
                    bgRt.anchoredPosition = Vector2.zero;
                }

                RectTransform proxRt = proxText.GetComponent<RectTransform>();
                proxRt.anchorMin = new Vector2(0.5f, 0f);
                proxRt.anchorMax = new Vector2(0.5f, 0f);
                proxRt.pivot = new Vector2(0.5f, 0.5f);
                proxRt.sizeDelta = new Vector2(380f, 36f);
                proxRt.anchoredPosition = new Vector2(0f, 210f); // Justo arriba de la barra de minijuegos
            }

            // 7. Estilizar Texto de Fase (Día / Noche)
            TextMeshProUGUI phaseText = GetPrivateField<TextMeshProUGUI>(hud, "textoFase");
            if (phaseText != null)
            {
                Undo.RegisterCompleteObjectUndo(phaseText.gameObject, "Style Phase Text");
                phaseText.fontSize = 15f;
                phaseText.fontStyle = FontStyles.Bold;
                phaseText.alignment = TextAlignmentOptions.Center;

                // Fondo para la etiqueta de la fase
                Image phaseBg = phaseText.GetComponentInChildren<Image>();
                if (phaseBg == null)
                {
                    GameObject bgObj = new GameObject("PhaseBG", typeof(RectTransform), typeof(Image));
                    bgObj.transform.SetParent(phaseText.transform, false);
                    bgObj.transform.SetAsFirstSibling();
                    
                    phaseBg = bgObj.GetComponent<Image>();
                    phaseBg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

                    Outline phaseOutline = bgObj.AddComponent<Outline>();
                    phaseOutline.effectColor = new Color(1f, 0.82f, 0.2f, 0.2f);
                    phaseOutline.effectDistance = new Vector2(1f, 1f);

                    RectTransform bgRt = bgObj.GetComponent<RectTransform>();
                    bgRt.anchorMin = Vector2.zero;
                    bgRt.anchorMax = Vector2.one;
                    bgRt.sizeDelta = new Vector2(30f, 10f);
                    bgRt.anchoredPosition = Vector2.zero;
                }

                RectTransform phaseRt = phaseText.GetComponent<RectTransform>();
                phaseRt.anchorMin = new Vector2(0.5f, 1f);
                phaseRt.anchorMax = new Vector2(0.5f, 1f);
                phaseRt.pivot = new Vector2(0.5f, 1f);
                phaseRt.sizeDelta = new Vector2(200f, 32f);
                phaseRt.anchoredPosition = new Vector2(0f, -30f); // Top center
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[StylePlayerHUD] ¡HUD del jugador estilizado exitosamente y escena guardada!");
        }

        // Función de reflexión para obtener variables serializadas privadas del Inspector
        private static T GetPrivateField<T>(object target, string fieldName) where T : class
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                return field.GetValue(target) as T;
            }
            return null;
        }
    }
}
