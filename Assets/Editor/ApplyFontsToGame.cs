using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

namespace Core.EditorTools
{
    public class ApplyFontsToGame : EditorWindow
    {
        [MenuItem("Tools/Aplicar Fuentes a todo el Juego")]
        public static void ApplyFonts()
        {
            // 1. Buscar fuentes de manera dinámica en el proyecto
            TMP_FontAsset cinzelTMP = null;
            TMP_FontAsset garamondTMP = null;
            Font cinzelTTF = null;
            Font garamondTTF = null;

            // Buscar SDF de TMP
            string[] tmpGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            foreach (string guid in tmpGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                if (name.Contains("cinzel") && cinzelTMP == null)
                {
                    cinzelTMP = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                }
                else if ((name.Contains("garamond") || name.Contains("ebgaramond")) && garamondTMP == null)
                {
                    garamondTMP = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                }
            }

            // Buscar TTF
            string[] ttfGuids = AssetDatabase.FindAssets("t:Font");
            foreach (string guid in ttfGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                if (name.Contains("cinzel") && cinzelTTF == null)
                {
                    cinzelTTF = AssetDatabase.LoadAssetAtPath<Font>(path);
                }
                else if ((name.Contains("garamond") || name.Contains("ebgaramond")) && garamondTTF == null)
                {
                    garamondTTF = AssetDatabase.LoadAssetAtPath<Font>(path);
                }
            }

            if (cinzelTMP == null || garamondTMP == null)
            {
                Debug.LogError("[ApplyFonts] No se encontraron los assets SDF de TextMeshPro en el proyecto.\n" +
                               "Para usar estas fuentes en TextMeshPro, debes generar los archivos SDF:\n" +
                               "1. En el menú superior de Unity, ve a: Window > TextMeshPro > Font Asset Creator.\n" +
                               "2. En 'Source Font File', arrastra una de las fuentes .ttf que importaste (Cinzel o EBGaramond).\n" +
                               "3. Haz clic en 'Generate Font Atlas' y luego en 'Save'.\n" +
                               "4. Guarda el archivo resultante en cualquier lugar dentro de Assets/ con un nombre que contenga la palabra 'Cinzel' o 'EBGaramond' (ej: 'Cinzel SDF' o 'EBGaramond SDF').\n" +
                               "5. Haz lo mismo para la otra fuente y vuelve a ejecutar 'Tools > Aplicar Fuentes a todo el Juego'.");
                return;
            }

            string[] scenesToModify = {
                "Assets/Scenes/Scene_Gameplay.unity",
                "Assets/Scenes/Scene_Menu.unity"
            };

            string activeScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

            // Preguntar al usuario si desea guardar la escena actual antes de proceder
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[ApplyFonts] Operación cancelada por el usuario.");
                return;
            }

            int totalTMPModified = 0;
            int totalTextModified = 0;

            foreach (string scenePath in scenesToModify)
            {
                if (AssetDatabase.LoadMainAssetAtPath(scenePath) == null)
                {
                    Debug.LogWarning($"[ApplyFonts] No se encontró la escena en: {scenePath}");
                    continue;
                }

                // Abrir escena
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Debug.Log($"[ApplyFonts] Procesando escena: {scene.name}");

                // 2. Modificar componentes TextMeshProUGUI (UI)
                TextMeshProUGUI[] tmpsUI = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
                foreach (var tmp in tmpsUI)
                {
                    // Evitar modificar prefabs que no estén en escena
                    if (tmp.gameObject.scene != scene) continue;

                    Undo.RegisterCompleteObjectUndo(tmp, "Apply Font");
                    
                    if (IsHeaderOrTitle(tmp.gameObject))
                    {
                        tmp.font = cinzelTMP;
                    }
                    else
                    {
                        tmp.font = garamondTMP;
                    }
                    totalTMPModified++;
                    EditorUtility.SetDirty(tmp);
                }

                // 3. Modificar componentes TextMeshPro (3D en el mundo, como carteles)
                TextMeshPro[] tmps3D = Resources.FindObjectsOfTypeAll<TextMeshPro>();
                foreach (var tmp in tmps3D)
                {
                    if (tmp.gameObject.scene != scene) continue;

                    Undo.RegisterCompleteObjectUndo(tmp, "Apply Font");
                    
                    if (IsHeaderOrTitle(tmp.gameObject))
                    {
                        tmp.font = cinzelTMP;
                    }
                    else
                    {
                        tmp.font = garamondTMP;
                    }
                    totalTMPModified++;
                    EditorUtility.SetDirty(tmp);
                }

                // 4. Modificar componentes UI Text (Legacy, como BotonMolde)
                Text[] texts = Resources.FindObjectsOfTypeAll<Text>();
                foreach (var txt in texts)
                {
                    if (txt.gameObject.scene != scene) continue;

                    Undo.RegisterCompleteObjectUndo(txt, "Apply Font");
                    
                    if (IsHeaderOrTitle(txt.gameObject) && cinzelTTF != null)
                    {
                        txt.font = cinzelTTF;
                    }
                    else if (garamondTTF != null)
                    {
                        txt.font = garamondTTF;
                    }
                    totalTextModified++;
                    EditorUtility.SetDirty(txt);
                }

                // Guardar escena modificada
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            // Volver a cargar la escena en la que estaba el usuario al inicio
            if (!string.IsNullOrEmpty(activeScenePath))
            {
                EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
            }

            Debug.Log($"[ApplyFonts] ¡Fuentes aplicadas con éxito en todas las escenas del juego!\n" +
                      $"- TextMeshPro modificados: {totalTMPModified}\n" +
                      $"- Textos Legacy modificados: {totalTextModified}");
        }

        private static bool IsHeaderOrTitle(GameObject obj)
        {
            string name = obj.name.ToLower();
            
            // Si el objeto o su padre tiene nombres clave de título o encabezado, se usa Cinzel
            if (name.Contains("title") || 
                name.Contains("header") || 
                name.Contains("titulo") || 
                name.Contains("fase") || 
                name.Contains("rol") || 
                name.Contains("asamblea") || 
                name.Contains("votacion") || 
                name.Contains("tienda") || 
                name.Contains("comerciante"))
            {
                return true;
            }

            if (obj.transform.parent != null)
            {
                string parentName = obj.transform.parent.name.ToLower();
                if (parentName.Contains("header") || parentName.Contains("title") || parentName.Contains("asamblea"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
