using UnityEngine;
using UnityEditor;
using TMPro;

public class SetupMissionsHUD
{
    [MenuItem("Tools/Recrear Canvas de Misiones")]
    public static void RecreateMissionsHUD()
    {
        // 1. Buscar el Pergamino 3D en la escena
        GameObject pergamino = GameObject.Find("Pergamino");
        if (pergamino == null)
        {
            Debug.LogError("No se encontró el objeto 'Pergamino'.");
            return;
        }

        // 2. Crear nueva jerarquía de Canvas
        GameObject root = new GameObject("MissionsScroll_Root", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(pergamino.transform, false);
        
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(400, 500);
        rootRect.localScale = new Vector3(0.005f, 0.005f, 0.005f);
        rootRect.localPosition = new Vector3(0, 0, -0.1f); // Un poco por delante
        rootRect.localRotation = Quaternion.identity;

        // 3. Crear el Texto TMP
        GameObject textObj = new GameObject("TaskText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(root.transform, false);
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = "Misiones de Prueba...\n- Tarea 1\n- Tarea 2";
        tmp.fontSize = 24;
        tmp.color = Color.black; // Tinta negra para el pergamino
        tmp.alignment = TextAlignmentOptions.TopLeft;

        // 4. Enlazar con GameplayUI si existe
        GameplayUI gameplayUI = Object.FindAnyObjectByType<GameplayUI>();
        if (gameplayUI != null)
        {
            SerializedObject serializedUI = new SerializedObject(gameplayUI);
            serializedUI.FindProperty("taskPanel").objectReferenceValue = root;
            serializedUI.FindProperty("taskText").objectReferenceValue = tmp;
            serializedUI.ApplyModifiedProperties();
            Debug.Log("GameplayUI actualizado automáticamente con el nuevo Canvas.");
        }

        Selection.activeGameObject = root;
        Undo.RegisterCreatedObjectUndo(root, "Recrear Canvas Misiones");
        Debug.Log("Canvas de Misiones recreado y colocado en el Pergamino!");
    }
}
