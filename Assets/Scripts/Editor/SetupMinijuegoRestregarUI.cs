#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UI.Minigames;
using Core.Environment;
using Core.Enums;
using TMPro;

/// <summary>
/// Script Editor para construir y configurar automáticamente la UI del minijuego Restregar (A/D) en la escena actual.
/// Menu: Lobo Game -> Crear y Configurar Minijuego Restregar UI
/// </summary>
public static class SetupMinijuegoRestregarUI
{
    [MenuItem("Lobo Game/Crear y Configurar Minijuego Restregar UI")]
    public static void ConfigurarTodo()
    {
        // 1. Buscar o crear el Canvas principal
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("GameplayCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Undo.RegisterCreatedObjectUndo(canvasObj, "Crear Canvas");
        }

        // 2. Buscar si ya existe el panel del minijuego
        Transform minijuegoTransform = canvas.transform.Find("MinijuegoRestregarPanel");
        GameObject panelObj;

        if (minijuegoTransform != null)
        {
            panelObj = minijuegoTransform.gameObject;
        }
        else
        {
            panelObj = new GameObject("MinijuegoRestregarPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panelObj.transform.SetParent(canvas.transform, false);
            Undo.RegisterCreatedObjectUndo(panelObj, "Crear Panel Minijuego Restregar");

            var panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(550f, 320f);

            var img = panelObj.GetComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.08f, 0.08f, 0.12f, 0.88f); // Fondo oscuro
        }

        var minijuegoComp = panelObj.GetComponent<MinijuegoRestregarUI>() ?? Undo.AddComponent<MinijuegoRestregarUI>(panelObj);

        // 3. Crear Barra de Suciedad (Verde/Marrón)
        UnityEngine.UI.Image imgBarSuciedad = CreateProgressBar(panelObj, "BarraSuciedad", new Vector2(0f, 60f), new Vector2(450f, 35f), new Color(0.6f, 0.4f, 0.2f, 1f), new Color(0.2f, 0.8f, 0.3f, 1f));
        
        // 4. Crear Barra de Desgaste (Roja/Naranja)
        UnityEngine.UI.Image imgBarDesgaste = CreateProgressBar(panelObj, "BarraDesgaste", new Vector2(0f, -10f), new Vector2(450f, 25f), new Color(0.2f, 0.2f, 0.2f, 1f), new Color(1f, 0.3f, 0.1f, 1f));

        // 5. Textos
        TextMeshProUGUI txtInstruccion = GetOrCreateText(panelObj, "TxtInstruccion", new Vector2(0f, 115f), "¡Limpia alternando [A] y [D] con ritmo!", 22, Color.cyan);
        TextMeshProUGUI txtSuciedad = GetOrCreateText(panelObj, "TxtSuciedad", new Vector2(0f, 60f), "Suciedad: 100%", 20, Color.white);
        TextMeshProUGUI txtDesgaste = GetOrCreateText(panelObj, "TxtDesgaste", new Vector2(0f, -10f), "Desgaste: 0%", 18, Color.white);
        TextMeshProUGUI txtFeedback = GetOrCreateText(panelObj, "TxtFeedback", new Vector2(0f, -75f), "", 24, Color.yellow);

        // 6. Asignar Referencias en SerializedObject
        SerializedObject so = new SerializedObject(minijuegoComp);
        so.FindProperty("mainPanel").objectReferenceValue = panelObj;
        so.FindProperty("imgBarraSuciedad").objectReferenceValue = imgBarSuciedad;
        so.FindProperty("imgBarraDesgaste").objectReferenceValue = imgBarDesgaste;
        so.FindProperty("txtSuciedad").objectReferenceValue = txtSuciedad;
        so.FindProperty("txtDesgaste").objectReferenceValue = txtDesgaste;
        so.FindProperty("txtFeedback").objectReferenceValue = txtFeedback;
        so.FindProperty("txtInstruccion").objectReferenceValue = txtInstruccion;
        so.ApplyModifiedProperties();

        // 7. Buscar e interactuable de la escena `QuestsLimpiarCenmenterio` / `QuestTumba` y cambiar su mecánica a Restregar
        var interactables = Object.FindObjectsByType<UniversalQuestInteractable>(FindObjectsSortMode.None);
        int interactablesConfigurados = 0;

        foreach (var uqi in interactables)
        {
            string n = uqi.gameObject.name.ToLower();
            if (n.Contains("limpiar") || n.Contains("cenmenterio") || n.Contains("tumba") || n.Contains("lapida"))
            {
                Undo.RecordObject(uqi, "Cambiar Mecanica a Restregar");
                uqi.mecanica = MecanicaInteraccion.Restregar;
                uqi.nombrePropiedadShader = "_Cleanliness";
                EditorUtility.SetDirty(uqi);
                interactablesConfigurados++;
            }
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Minijuego Restregar Configurado",
            $"¡Completado con éxito!\n\n" +
            $"  • UI Overlay con Barras de Suciedad y Desgaste generado.\n" +
            $"  • Interactuables de Limpieza/Cementerio ajustados a 'Restregar': {interactablesConfigurados}\n\n" +
            "Guarda la escena (Ctrl+S) para conservar los cambios.",
            "OK");
    }

    private static UnityEngine.UI.Image CreateProgressBar(GameObject parent, string name, Vector2 pos, Vector2 size, Color bgColor, Color fillColor)
    {
        Transform t = parent.transform.Find(name);
        GameObject bgObj;
        if (t == null)
        {
            bgObj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            bgObj.transform.SetParent(parent.transform, false);
        }
        else bgObj = t.gameObject;

        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchoredPosition = pos;
        bgRect.sizeDelta = size;
        var bgImg = bgObj.GetComponent<UnityEngine.UI.Image>();
        bgImg.color = bgColor;

        Transform fillTrans = bgObj.transform.Find("Fill");
        GameObject fillObj;
        if (fillTrans == null)
        {
            fillObj = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            fillObj.transform.SetParent(bgObj.transform, false);
        }
        else fillObj = fillTrans.gameObject;

        var fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        var fillImg = fillObj.GetComponent<UnityEngine.UI.Image>();
        fillImg.color = fillColor;
        fillImg.type = UnityEngine.UI.Image.Type.Filled;
        fillImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        fillImg.fillOrigin = 0; // Izquierda a derecha

        return fillImg;
    }

    private static TextMeshProUGUI GetOrCreateText(GameObject parent, string name, Vector2 pos, string defaultText, int fontSize, Color color)
    {
        Transform t = parent.transform.Find(name);
        GameObject go;
        if (t == null)
        {
            go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform, false);
        }
        else go = t.gameObject;

        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(500f, 40f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        return tmp;
    }
}
#endif
