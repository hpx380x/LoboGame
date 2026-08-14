using UnityEngine;
using UnityEditor;
using Core.Environment;
using TMPro;

public class AutoGenerateQuestHUD
{
    [MenuItem("Tools/Generar HUD para Misiones")]
    public static void GenerateHUDs()
    {
        // Buscar todos los interactuables en la escena
        UniversalQuestInteractable[] interactables = Object.FindObjectsByType<UniversalQuestInteractable>(FindObjectsInactive.Include);
        int generados = 0;

        foreach (var interactable in interactables)
        {
            // Si ya tiene un HUD asignado, lo saltamos
            if (interactable.hudObject != null) continue;

            // Crear el objeto de texto
            GameObject hudObj = new GameObject("HUD_Mision");
            hudObj.transform.SetParent(interactable.transform);
            
            // Posicionar arriba del objeto
            hudObj.transform.localPosition = new Vector3(0, 2.5f, 0);
            
            // Añadir TextMeshPro
            TextMeshPro textMesh = hudObj.AddComponent<TextMeshPro>();
            textMesh.text = "Misión";
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.fontSize = 5;
            
            // Ajustar el RectTransform
            RectTransform rt = hudObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(5, 2);
            
            // Enlazar al script usando Undo para que se guarde
            Undo.RegisterCreatedObjectUndo(hudObj, "Crear HUD Mision");
            Undo.RecordObject(interactable, "Asignar HUD a Mision");
            
            interactable.hudObject = hudObj;
            interactable.hudText = textMesh;
            
            EditorUtility.SetDirty(interactable);
            generados++;
        }

        if (generados > 0)
        {
            Debug.Log($"[Auto-HUD] ¡Éxito! Se han generado y enlazado {generados} HUDs flotantes.");
        }
        else
        {
            Debug.Log("[Auto-HUD] No se encontró ningún interactuable que necesite HUD, o todos ya lo tienen.");
        }
    }
}
