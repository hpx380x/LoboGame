using UnityEngine;
using UnityEditor;
using Core.QuestSystem;

public static class CheckQuestInteractables
{
    [MenuItem("LoboGame/Check Interactables")]
    public static void Check()
    {
        var interactables = Object.FindObjectsByType<Core.Environment.UniversalQuestInteractable>(FindObjectsInactive.Exclude);
        if (interactables.Length == 0)
        {
            Debug.LogWarning("No se encontraron UniversalQuestInteractable en la escena actual.");
            return;
        }
        foreach (var i in interactables)
        {
            Debug.Log($"Interactable: {i.gameObject.name}, Material: {i.materialAsignado}, AnimType: {i.tipoAnimacionRecogida}");
        }
    }
}
