#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Core.Environment;
using Core.QuestSystem;

/// <summary>
/// Herramienta Editor: enlaza automaticamente el campo QuestData de cada
/// UniversalQuestInteractable buscando el asset cuyo questID coincide con
/// el nombre del GameObject (o con las reglas de mapeo definidas abajo).
///
/// Menu: Lobo Game -> Auto-Link QuestData en Escena Activa
/// </summary>
public static class AutoLinkQuestData
{
    // Mapa manual: nombre del GameObject (parcial, case-insensitive) -> ruta del asset en Resources/Quests/
    private static readonly System.Collections.Generic.Dictionary<string, string> Mapa = new()
    {
        { "QuestsRecogerBruja",       "Quests/Recoger_bruja"   },
        { "QuestsAfilarAse",          "Quests/Afilado"          },
        { "QuestRegarArbol",          "Quests/traer"            },
        { "QuestsRegarBruja",         "Quests/Huerto"           },
        { "QuestsLimpiarCenmenterio", "Quests/Limpieza"         },
        { "QuestsTalar",              "Quests/Madera"           },
        { "Questvelaaltar",           "Quests/Velas"            },
        { "Encenderbrazier",          "Quests/Velas 1"          },
        { "QuestsLimpiarT",           "Quests/Limpieza 1"       },
        { "QuestRepararTorre",        "Quests/reparartorre"     },
        { "QuestPicarcueva",          "Quests/minar"            },
        { "QuestTumba",               "Quests/repararlapida"    },
        { "Mesabruja",                "Quests/Recoger_bruja"    },
    };

    [MenuItem("Lobo Game/Auto-Link QuestData en Escena Activa")]
    public static void LinkQuestData()
    {
        int linked = 0;
        int skipped = 0;
        int notFound = 0;

        var todos = Object.FindObjectsByType<UniversalQuestInteractable>(FindObjectsSortMode.None);

        foreach (var uqi in todos)
        {
            // Si ya tiene QuestData enlazado, saltar
            if (uqi.questData != null) { skipped++; continue; }

            // Buscar por nombre en el mapa
            string assetPath = null;
            foreach (var kv in Mapa)
            {
                if (uqi.gameObject.name.IndexOf(kv.Key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    assetPath = kv.Value;
                    break;
                }
            }

            if (string.IsNullOrEmpty(assetPath)) { notFound++; continue; }

            var questData = Resources.Load<QuestData>(assetPath);
            if (questData == null)
            {
                Debug.LogWarning($"[AutoLinkQuestData] No se encontro el asset en Resources/{assetPath} para '{uqi.gameObject.name}'");
                notFound++;
                continue;
            }

            Undo.RecordObject(uqi, "Auto-Link QuestData");
            uqi.questData = questData;
            EditorUtility.SetDirty(uqi);
            linked++;

            Debug.Log($"[AutoLinkQuestData] '{uqi.gameObject.name}' -> {questData.questID} (AnimType={questData.animationInteractionType})");
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Auto-Link QuestData",
            $"Completado:\n\n" +
            $"  Enlazados:      {linked}\n" +
            $"  Ya tenian uno:  {skipped}\n" +
            $"  Sin mapeo:      {notFound}\n\n" +
            "Guarda la escena (Ctrl+S) para conservar los cambios.",
            "OK");
    }
}
#endif
