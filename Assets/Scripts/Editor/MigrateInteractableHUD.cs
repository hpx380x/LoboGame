#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Core.Environment;
using TMPro;

/// <summary>
/// Herramienta de migracion: busca todos los UniversalQuestInteractable de la escena,
/// lee los campos huerfanos (hudObject / hudText) que Unity aun guarda en el serializado
/// y los transfiere al nuevo componente InteractableHUD.
///
/// Menu: Lobo Game -> Migrar InteractableHUD
/// </summary>
public static class MigrateInteractableHUD
{
    [MenuItem("Lobo Game/Migrar InteractableHUD en Escena Activa")]
    public static void MigrarEscenaActiva()
    {
        int migrados = 0;
        int yaOk = 0;

        var todos = Object.FindObjectsByType<UniversalQuestInteractable>(FindObjectsSortMode.None);

        foreach (var uqi in todos)
        {
            // Leer los campos huerfanos que Unity aun tiene en memoria
            var so = new SerializedObject(uqi);

            SerializedProperty propHudObj  = so.FindProperty("hudObject");
            SerializedProperty propHudText = so.FindProperty("hudText");

            GameObject viejoHudObject  = propHudObj  != null ? (GameObject)propHudObj.objectReferenceValue  : null;
            TMP_Text   viejoHudText    = propHudText != null ? (TMP_Text)propHudText.objectReferenceValue    : null;

            // Obtener o crear el componente InteractableHUD
            var hud = uqi.GetComponent<InteractableHUD>();
            bool eraNull = (hud == null);
            if (eraNull)
                hud = Undo.AddComponent<InteractableHUD>(uqi.gameObject);

            // Transferir referencias si hay datos viejos y el HUD aun no tiene referencias
            bool modificado = false;

            if (viejoHudObject != null && hud.hudObject == null)
            {
                Undo.RecordObject(hud, "Migrar hudObject");
                hud.hudObject = viejoHudObject;
                modificado = true;
            }

            if (viejoHudText != null && hud.hudText == null)
            {
                Undo.RecordObject(hud, "Migrar hudText");
                // TMP_Text y TextMeshPro son compatibles
                var tmp3D = viejoHudText as TextMeshPro;
                if (tmp3D != null) hud.hudText = tmp3D;
                modificado = true;
            }

            // Asignar customUIMessage si el HUD no tiene mensaje aun
            var soUQI = new SerializedObject(uqi);
            var propMsg = soUQI.FindProperty("customUIMessage");
            if (propMsg != null && !string.IsNullOrEmpty(propMsg.stringValue) && string.IsNullOrEmpty(hud.mensajePorDefecto))
            {
                Undo.RecordObject(hud, "Migrar mensajePorDefecto");
                hud.mensajePorDefecto = propMsg.stringValue;
                modificado = true;
            }

            // Enlazar el componente HUD en UniversalQuestInteractable
            var soUQI2 = new SerializedObject(uqi);
            var propHUD = soUQI2.FindProperty("interactableHUD");
            if (propHUD != null && propHUD.objectReferenceValue == null)
            {
                Undo.RecordObject(uqi, "Enlazar InteractableHUD");
                propHUD.objectReferenceValue = hud;
                soUQI2.ApplyModifiedProperties();
                modificado = true;
            }

            if (modificado || eraNull)
                migrados++;
            else
                yaOk++;

            EditorUtility.SetDirty(uqi.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Migracion InteractableHUD",
            $"Completado.\n\n" +
            $"  Objetos migrados: {migrados}\n" +
            $"  Ya estaban bien:  {yaOk}\n\n" +
            "Guarda la escena (Ctrl+S) para conservar los cambios.",
            "OK");

        Debug.Log($"[MigrateInteractableHUD] Migrados={migrados}  YaOk={yaOk}");
    }

    [MenuItem("Lobo Game/Migrar InteractableHUD en TODAS las Escenas del Build")]
    public static void MigrarTodasLasEscenas()
    {
        // Guardar la escena actual primero
        EditorSceneManager.SaveOpenScenes();

        int totalMigrados = 0;
        int totalEscenas  = 0;

        foreach (var escenaBuild in EditorBuildSettings.scenes)
        {
            if (!escenaBuild.enabled) continue;

            var escena = EditorSceneManager.OpenScene(escenaBuild.path, OpenSceneMode.Single);
            totalEscenas++;

            var todos = Object.FindObjectsByType<UniversalQuestInteractable>(FindObjectsSortMode.None);
            foreach (var uqi in todos)
            {
                var so = new SerializedObject(uqi);

                SerializedProperty propHudObj  = so.FindProperty("hudObject");
                SerializedProperty propHudText = so.FindProperty("hudText");

                GameObject viejoHudObject = propHudObj  != null ? (GameObject)propHudObj.objectReferenceValue  : null;
                TMP_Text   viejoHudText   = propHudText != null ? (TMP_Text)propHudText.objectReferenceValue    : null;

                var hud = uqi.GetComponent<InteractableHUD>() ?? uqi.gameObject.AddComponent<InteractableHUD>();

                if (viejoHudObject != null && hud.hudObject == null) hud.hudObject = viejoHudObject;
                if (viejoHudText   != null && hud.hudText   == null)
                {
                    var tmp3D = viejoHudText as TextMeshPro;
                    if (tmp3D != null) hud.hudText = tmp3D;
                }

                var soUQI = new SerializedObject(uqi);
                var propHUD = soUQI.FindProperty("interactableHUD");
                if (propHUD != null && propHUD.objectReferenceValue == null)
                {
                    propHUD.objectReferenceValue = hud;
                    soUQI.ApplyModifiedProperties();
                }

                EditorUtility.SetDirty(uqi.gameObject);
                totalMigrados++;
            }

            EditorSceneManager.SaveScene(escena);
        }

        EditorUtility.DisplayDialog(
            "Migracion InteractableHUD",
            $"Escenas procesadas: {totalEscenas}\n" +
            $"Objetos migrados:  {totalMigrados}\n\n" +
            "Todas las escenas han sido guardadas automaticamente.",
            "OK");
    }
}
#endif
