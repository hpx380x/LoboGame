using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

namespace Core.EditorTools
{
    public class StyleVotingPanel : EditorWindow
    {
        [MenuItem("Tools/Estilizar Panel de Votación")]
        public static void StylePanel()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name != "Scene_Gameplay")
            {
                Debug.LogWarning("[StyleVotingPanel] Abre 'Scene_Gameplay' primero.");
                return;
            }

            GameObject canvasObj = GameObject.Find("Canvas_Gameplay");
            if (canvasObj == null)
            {
                Debug.LogError("[StyleVotingPanel] No se encontró el Canvas_Gameplay en la escena.");
                return;
            }

            Transform panelVotacionTrans = FindDeepChild(canvasObj.transform, "PanelVotacion");
            if (panelVotacionTrans == null)
            {
                Debug.LogError("[StyleVotingPanel] No se encontró el GameObject 'PanelVotacion' dentro del Canvas.");
                return;
            }

            GameObject panelVotacion = panelVotacionTrans.gameObject;

            Undo.RegisterCompleteObjectUndo(panelVotacion, "Style Voting UI");

            // 1. Estilizar Fondo Oscuro Completo (PanelVotacion)
            Image bgPanel = panelVotacion.GetComponent<Image>();
            if (bgPanel != null)
            {
                // Un fondo oscuro místico translúcido que cubra toda la pantalla
                bgPanel.color = new Color(0.03f, 0.03f, 0.05f, 0.94f);
            }

            // 2. Estilizar ContenedorBotones (Ventana de la Asamblea)
            Transform contenedor = panelVotacion.transform.Find("ContenedorBotones");
            if (contenedor != null)
            {
                Undo.RegisterCompleteObjectUndo(contenedor.gameObject, "Style Contenedor");
                
                RectTransform contRt = contenedor.GetComponent<RectTransform>();
                contRt.anchorMin = new Vector2(0.5f, 0.5f);
                contRt.anchorMax = new Vector2(0.5f, 0.5f);
                contRt.pivot = new Vector2(0.5f, 0.5f);
                contRt.sizeDelta = new Vector2(580f, 520f); // Tamaño de ventana elegante
                contRt.anchoredPosition = Vector2.zero;

                Image contImg = contenedor.GetComponent<Image>();
                if (contImg != null)
                {
                    contImg.color = new Color(0.08f, 0.08f, 0.11f, 0.98f); // Ventana oscura
                }

                // Borde de ventana místico rojo/dorado para asamblea de expulsión
                Outline contOutline = contenedor.GetComponent<Outline>();
                if (contOutline == null) contOutline = contenedor.gameObject.AddComponent<Outline>();
                contOutline.effectColor = new Color(0.9f, 0.25f, 0.15f, 0.45f); // Rojo-naranja fuego
                contOutline.effectDistance = new Vector2(3f, 3f);

                // Configurar Layout Group
                VerticalLayoutGroup vlg = contenedor.GetComponent<VerticalLayoutGroup>();
                if (vlg != null)
                {
                    vlg.padding = new RectOffset(40, 40, 100, 40); // Espacio arriba para el título
                    vlg.spacing = 12f;
                    vlg.childAlignment = TextAnchor.UpperCenter;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = false;
                    vlg.childForceExpandWidth = false;
                    vlg.childForceExpandHeight = false;
                }

                // 3. Crear/Estilizar Cabecera de Asamblea (Título y Subtítulo)
                Transform headerTrans = contenedor.Find("AsambleaHeader");
                GameObject headerObj;
                if (headerTrans == null)
                {
                    headerObj = new GameObject("AsambleaHeader", typeof(RectTransform));
                    headerObj.transform.SetParent(contenedor, false);
                }
                else
                {
                    headerObj = headerTrans.gameObject;
                }

                // Evitar que el VerticalLayoutGroup controle las dimensiones de este panel de cabecera
                LayoutElement layoutElement = headerObj.GetComponent<LayoutElement>();
                if (layoutElement == null) layoutElement = headerObj.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;

                // Posición absoluta en la parte superior del contenedor, fuera de las garras del VerticalLayoutGroup
                RectTransform headerRt = headerObj.GetComponent<RectTransform>();
                headerRt.anchorMin = new Vector2(0f, 1f);
                headerRt.anchorMax = new Vector2(1f, 1f);
                headerRt.pivot = new Vector2(0.5f, 1f);
                headerRt.sizeDelta = new Vector2(-40f, 80f);
                headerRt.anchoredPosition = new Vector2(0f, -15f);

                // Título
                Transform titleTrans = headerObj.transform.Find("Title");
                TextMeshProUGUI titleText;
                if (titleTrans == null)
                {
                    GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
                    titleObj.transform.SetParent(headerObj.transform, false);
                    titleText = titleObj.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    titleText = titleTrans.GetComponent<TextMeshProUGUI>();
                }
                titleText.text = "ASAMBLEA DE DESTIERRO";
                titleText.fontSize = 22f;
                titleText.fontStyle = FontStyles.Bold;
                titleText.color = new Color(0.95f, 0.3f, 0.2f, 1f); // Rojo fuego/alarma
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.characterSpacing = 2f;

                RectTransform titleRt = titleText.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0f, 1f);
                titleRt.anchorMax = new Vector2(1f, 1f);
                titleRt.pivot = new Vector2(0.5f, 1f);
                titleRt.sizeDelta = new Vector2(0f, 30f);
                titleRt.anchoredPosition = new Vector2(0f, 0f);

                // Subtítulo
                Transform subTrans = headerObj.transform.Find("Subtitle");
                TextMeshProUGUI subText;
                if (subTrans == null)
                {
                    GameObject subObj = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                    subObj.transform.SetParent(headerObj.transform, false);
                    subText = subObj.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    subText = subTrans.GetComponent<TextMeshProUGUI>();
                }
                subText.text = "¿Quién es el Hombre Lobo? Elige a quién desterrar de la aldea.";
                subText.fontSize = 11.5f;
                subText.color = new Color(0.65f, 0.65f, 0.75f, 1f);
                subText.alignment = TextAlignmentOptions.Center;

                RectTransform subRt = subText.GetComponent<RectTransform>();
                subRt.anchorMin = new Vector2(0f, 0f);
                subRt.anchorMax = new Vector2(1f, 0f);
                subRt.pivot = new Vector2(0.5f, 0f);
                subRt.sizeDelta = new Vector2(0f, 20f);
                subRt.anchoredPosition = new Vector2(0f, 10f);

                // 4. Estilizar Botón Molde (BotonMolde)
                Transform moldTrans = contenedor.Find("BotonMolde");
                if (moldTrans != null)
                {
                    Undo.RegisterCompleteObjectUndo(moldTrans.gameObject, "Style Boton");
                    RectTransform moldRt = moldTrans.GetComponent<RectTransform>();
                    moldRt.sizeDelta = new Vector2(500f, 45f); // Botones anchos elegantes

                    Image btnImg = moldTrans.GetComponent<Image>();
                    if (btnImg != null)
                    {
                        btnImg.color = new Color(0.13f, 0.13f, 0.17f, 1f); // Gris premium
                    }

                    // Outline sutil en el botón
                    Outline btnOutline = moldTrans.GetComponent<Outline>();
                    if (btnOutline == null) btnOutline = moldTrans.gameObject.AddComponent<Outline>();
                    btnOutline.effectColor = new Color(1f, 1f, 1f, 0.05f);
                    btnOutline.effectDistance = new Vector2(1f, 1f);

                    // Transiciones de color del botón
                    Button btn = moldTrans.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.transition = Selectable.Transition.ColorTint;
                        ColorBlock cb = btn.colors;
                        cb.normalColor = new Color(1f, 1f, 1f, 1f);
                        cb.highlightedColor = new Color(1.5f, 1.5f, 1.5f, 1f); // Feedback al pasar el mouse
                        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                        cb.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
                        btn.colors = cb;
                    }

                    // Estilizar Texto del Botón (Text Legacy)
                    Transform textTrans = moldTrans.Find("Text (Legacy)");
                    if (textTrans != null)
                    {
                        Text t = textTrans.GetComponent<Text>();
                        if (t != null)
                        {
                            t.color = Color.white;
                            t.fontSize = 14;
                            t.alignment = TextAnchor.MiddleCenter;
                            t.fontStyle = FontStyle.Bold;
                        }

                        // Forzar anclaje completo del texto en el botón
                        RectTransform textRt = textTrans.GetComponent<RectTransform>();
                        textRt.anchorMin = Vector2.zero;
                        textRt.anchorMax = Vector2.one;
                        textRt.offsetMin = Vector2.zero;
                        textRt.offsetMax = Vector2.zero;
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[StyleVotingPanel] ¡Panel de Votación estilizado con éxito y escena guardada!");
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                Transform found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
