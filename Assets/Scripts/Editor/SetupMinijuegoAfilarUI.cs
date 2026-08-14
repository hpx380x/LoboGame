#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UI.Minigames;
using Core.Environment;
using Core.Enums;
using TMPro;

/// <summary>
/// Script Editor para construir y configurar automáticamente el Canvas UI y los componentes
/// del minijuego de afilar (Ritmo WASD) en la escena actual.
/// Menu: Lobo Game -> Crear y Configurar Minijuego Afilar UI
/// </summary>
public static class SetupMinijuegoAfilarUI
{
    [MenuItem("Lobo Game/Crear y Configurar Minijuego Afilar UI")]
    public static void ConfigurarTodo()
    {
        // 1. Buscar o crear el Canvas principal en la escena
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("GameplayCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Undo.RegisterCreatedObjectUndo(canvasObj, "Crear Canvas");
        }

        // 2. Buscar si ya existe el panel del minijuego o crearlo
        Transform minijuegoTransform = canvas.transform.Find("MinijuegoAfilarPanel");
        GameObject panelObj;

        if (minijuegoTransform != null)
        {
            panelObj = minijuegoTransform.gameObject;
        }
        else
        {
            panelObj = new GameObject("MinijuegoAfilarPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panelObj.transform.SetParent(canvas.transform, false);
            Undo.RegisterCreatedObjectUndo(panelObj, "Crear Panel Minijuego Afilar");

            var panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(600f, 700f);

            var img = panelObj.GetComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.05f, 0.05f, 0.08f, 0.85f); // Fondo oscuro semitransparente
        }

        var minijuegoComp = panelObj.GetComponent<MinijuegoAfilarUI>() ?? Undo.AddComponent<MinijuegoAfilarUI>(panelObj);

        // 3. Crear Prefab de Nota temporal si no existe
        string prefabPath = "Assets/Resources/Minigames/NotePrefab.prefab";
        GameObject notePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (notePrefab == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Minigames"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateFolder("Assets/Resources", "Minigames");
            }

            GameObject noteGo = new GameObject("NotePrefab", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var noteRect = noteGo.GetComponent<RectTransform>();
            noteRect.sizeDelta = new Vector2(90f, 60f);
            var noteImg = noteGo.GetComponent<UnityEngine.UI.Image>();
            noteImg.color = new Color(0.2f, 0.6f, 1f, 1f); // Azul brillante

            GameObject textGo = new GameObject("TxtLetter", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(noteGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "W";
            tmp.fontSize = 32;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            notePrefab = PrefabUtility.SaveAsPrefabAsset(noteGo, prefabPath);
            Object.DestroyImmediate(noteGo);
            Debug.Log($"[SetupMinijuego] Creado Prefab de Nota en: {prefabPath}");
        }

        // 4. Crear Carriles (W, A, S, D)
        RectTransform[] spawnPoints = new RectTransform[4];
        RectTransform[] hitZones = new RectTransform[4];
        string[] nombres = { "W", "A", "S", "D" };
        float startX = -195f;
        float spacingX = 130f;

        for (int i = 0; i < 4; i++)
        {
            string laneName = $"Lane_{nombres[i]}";
            Transform laneTrans = panelObj.transform.Find(laneName);
            GameObject laneObj;

            if (laneTrans == null)
            {
                laneObj = new GameObject(laneName, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                laneObj.transform.SetParent(panelObj.transform, false);

                var lRect = laneObj.GetComponent<RectTransform>();
                lRect.anchoredPosition = new Vector2(startX + (i * spacingX), 0f);
                lRect.sizeDelta = new Vector2(110f, 650f);

                var lImg = laneObj.GetComponent<UnityEngine.UI.Image>();
                lImg.color = new Color(0.15f, 0.15f, 0.2f, 0.5f);
            }
            else
            {
                laneObj = laneTrans.gameObject;
            }

            // SpawnPoint (Arriba)
            Transform spTrans = laneObj.transform.Find("SpawnPoint");
            GameObject spObj;
            if (spTrans == null)
            {
                spObj = new GameObject("SpawnPoint", typeof(RectTransform));
                spObj.transform.SetParent(laneObj.transform, false);
                var spRect = spObj.GetComponent<RectTransform>();
                spRect.anchoredPosition = new Vector2(0f, 280f);
            }
            else spObj = spTrans.gameObject;
            spawnPoints[i] = spObj.GetComponent<RectTransform>();

            // HitZone (Abajo)
            Transform hzTrans = laneObj.transform.Find("HitZone");
            GameObject hzObj;
            if (hzTrans == null)
            {
                hzObj = new GameObject("HitZone", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                hzObj.transform.SetParent(laneObj.transform, false);
                var hzRect = hzObj.GetComponent<RectTransform>();
                hzRect.anchoredPosition = new Vector2(0f, -250f);
                hzRect.sizeDelta = new Vector2(100f, 50f);
                var hzImg = hzObj.GetComponent<UnityEngine.UI.Image>();
                hzImg.color = new Color(0.2f, 0.9f, 0.3f, 0.6f); // Franja verde de acierto

                // Texto con la tecla
                GameObject keyTxtGo = new GameObject("TxtKey", typeof(RectTransform), typeof(TextMeshProUGUI));
                keyTxtGo.transform.SetParent(hzObj.transform, false);
                var ktRect = keyTxtGo.GetComponent<RectTransform>();
                ktRect.anchorMin = Vector2.zero;
                ktRect.anchorMax = Vector2.one;
                ktRect.sizeDelta = Vector2.zero;
                var ktTmp = keyTxtGo.GetComponent<TextMeshProUGUI>();
                ktTmp.text = nombres[i];
                ktTmp.fontSize = 28;
                ktTmp.alignment = TextAlignmentOptions.Center;
                ktTmp.color = Color.white;
            }
            else hzObj = hzTrans.gameObject;
            hitZones[i] = hzObj.GetComponent<RectTransform>();
        }

        // 5. Crear Textos UI de Progreso y Combo
        TextMeshProUGUI txtProgreso = GetOrCreateText(panelObj, "TxtProgreso", new Vector2(0f, 310f), "Progreso: 0/8", 26);
        TextMeshProUGUI txtCombo = GetOrCreateText(panelObj, "TxtCombo", new Vector2(200f, 310f), "", 24);
        TextMeshProUGUI txtFeedback = GetOrCreateText(panelObj, "TxtFeedback", new Vector2(0f, -310f), "", 30);

        // 6. Asignar Referencias en el Componente
        SerializedObject so = new SerializedObject(minijuegoComp);
        so.FindProperty("mainPanel").objectReferenceValue = panelObj;
        so.FindProperty("notePrefab").objectReferenceValue = notePrefab;
        so.FindProperty("txtProgreso").objectReferenceValue = txtProgreso;
        so.FindProperty("txtCombo").objectReferenceValue = txtCombo;
        so.FindProperty("txtFeedbackHit").objectReferenceValue = txtFeedback;

        SerializedProperty spArray = so.FindProperty("laneSpawnPoints");
        spArray.arraySize = 4;
        for (int i = 0; i < 4; i++) spArray.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];

        SerializedProperty hzArray = so.FindProperty("laneHitZones");
        hzArray.arraySize = 4;
        for (int i = 0; i < 4; i++) hzArray.GetArrayElementAtIndex(i).objectReferenceValue = hitZones[i];

        so.ApplyModifiedProperties();

        // 7. Buscar e interactuable de la escena `QuestsAfilarAse` y cambiar su mecánica a RitmoAfilar
        var interactables = Object.FindObjectsByType<UniversalQuestInteractable>(FindObjectsSortMode.None);
        int interactablesConfigurados = 0;

        foreach (var uqi in interactables)
        {
            if (uqi.gameObject.name.IndexOf("Afilar", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Undo.RecordObject(uqi, "Cambiar Mecanica a RitmoAfilar");
                uqi.mecanica = MecanicaInteraccion.RitmoAfilar;
                EditorUtility.SetDirty(uqi);
                interactablesConfigurados++;
            }
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Minijuego Afilar Configurado",
            $"¡Completado con éxito!\n\n" +
            $"  • Canvas UI y 4 Carriles (W,A,S,D) generados.\n" +
            $"  • Prefab de Nota creado en Assets/Resources/Minigames/\n" +
            $"  • Interactuables de Afilar ajustados a 'RitmoAfilar': {interactablesConfigurados}\n\n" +
            "Guarda la escena (Ctrl+S) para conservar los cambios.",
            "OK");
    }

    private static TextMeshProUGUI GetOrCreateText(GameObject parent, string name, Vector2 pos, string defaultText, int fontSize)
    {
        Transform t = parent.transform.Find(name);
        GameObject go;
        if (t == null)
        {
            go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(400f, 50f);
        }
        else go = t.gameObject;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }
}
#endif
