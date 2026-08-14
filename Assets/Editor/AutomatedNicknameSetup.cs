using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class AutomatedNicknameSetup
{
    static AutomatedNicknameSetup()
    {
        EditorApplication.delayCall += ExecuteModification;
    }

    public static void ExecuteModification()
    {
        string markerPath = "Assets/Editor/AutomatedNicknameSetupRun.txt";
        if (System.IO.File.Exists(markerPath)) return;

        string originalScenePath = EditorSceneManager.GetActiveScene().path;
        bool needsSceneSwitch = originalScenePath != "Assets/Scenes/Scene_Menu.unity";

        if (needsSceneSwitch)
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Scene_Menu.unity", OpenSceneMode.Single);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[NicknameSetup] No se pudo abrir Scene_Menu.unity: {ex.Message}");
                return;
            }
        }

        bool sceneDirty = false;

        LobbyUI lobbyUI = Object.FindAnyObjectByType<LobbyUI>(FindObjectsInactive.Include);
        if (lobbyUI != null)
        {
            SerializedObject so = new SerializedObject(lobbyUI);
            SerializedProperty nickInputFieldProp = so.FindProperty("nicknameInputField");
            SerializedProperty mainMenuPanelProp = so.FindProperty("mainMenuPanel");

            if (nickInputFieldProp != null)
            {
                // Destruir el InputField viejo si ya existe para hacer una recreación limpia
                GameObject inputFieldGo = GameObject.Find("InputField_Nickname");
                if (inputFieldGo != null)
                {
                    Debug.Log("[NicknameSetup] Destruyendo campo InputField_Nickname anterior para recreación limpia.");
                    Object.DestroyImmediate(inputFieldGo);
                    sceneDirty = true;
                }

                GameObject parentPanel = null;
                if (mainMenuPanelProp != null && mainMenuPanelProp.objectReferenceValue != null)
                {
                    parentPanel = (GameObject)mainMenuPanelProp.objectReferenceValue;
                }
                else
                {
                    parentPanel = GameObject.Find("MainMenuPanel") ?? GameObject.Find("Canvas");
                }

                if (parentPanel != null)
                {
                    // Crear el TMP Input Field usando la utilidad nativa de TMPro
                    inputFieldGo = TMPro.TMP_DefaultControls.CreateInputField(new TMPro.TMP_DefaultControls.Resources());
                    inputFieldGo.name = "InputField_Nickname";
                    
                    RectTransform rect = inputFieldGo.GetComponent<RectTransform>();
                    rect.SetParent(parentPanel.transform, false);

                    // Posicionar encima del host button si existe
                    Button hostBtn = null;
                    SerializedProperty hostBtnProp = so.FindProperty("hostButton");
                    if (hostBtnProp != null && hostBtnProp.objectReferenceValue != null)
                    {
                        hostBtn = (Button)hostBtnProp.objectReferenceValue;
                    }

                    if (hostBtn != null)
                    {
                        RectTransform hostRect = hostBtn.GetComponent<RectTransform>();
                        Vector2 hostPos = hostRect.anchoredPosition;
                        rect.anchoredPosition = new Vector2(hostPos.x, hostPos.y + 110f); // 110 unidades arriba
                    }
                    else
                    {
                        rect.anchoredPosition = new Vector2(0f, 80f); // Fallback centrado
                    }

                    rect.sizeDelta = new Vector2(240f, 40f);

                    // Ajustar Placeholder
                    Transform placeholderTr = rect.Find("Placeholder");
                    if (placeholderTr != null)
                    {
                        TextMeshProUGUI placeholderText = placeholderTr.GetComponent<TextMeshProUGUI>();
                        if (placeholderText != null)
                        {
                            placeholderText.text = "Escribe tu nombre aquí...";
                            placeholderText.fontSize = 15;
                            placeholderText.alignment = TextAlignmentOptions.Center;
                        }
                    }

                    // Ajustar texto escrito
                    Transform textTr = rect.Find("Text Area/Text");
                    if (textTr != null)
                    {
                        TextMeshProUGUI textComp = textTr.GetComponent<TextMeshProUGUI>();
                        if (textComp != null)
                        {
                            textComp.fontSize = 16;
                            textComp.alignment = TextAlignmentOptions.Center;
                        }
                    }

                    TMP_InputField inputField = inputFieldGo.GetComponent<TMP_InputField>();
                    if (inputField != null)
                    {
                        nickInputFieldProp.objectReferenceValue = inputField;
                        so.ApplyModifiedProperties();
                        sceneDirty = true;
                        Debug.Log("<color=green>[NicknameSetup] Vinculado InputField_Nickname en el componente LobbyUI.</color>");
                    }

                    sceneDirty = true;
                    Debug.Log("<color=green>[NicknameSetup] Creado InputField_Nickname en Scene_Menu y posicionado correctamente.</color>");
                }
            }
        }

        if (sceneDirty)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        if (needsSceneSwitch && !string.IsNullOrEmpty(originalScenePath))
        {
            try
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[NicknameSetup] No se pudo restaurar la escena original ({originalScenePath}): {ex.Message}");
            }
        }

        // Escribir archivo marcador para que no se ejecute dos veces
        try
        {
            System.IO.File.WriteAllText(markerPath, "Done");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NicknameSetup] Error al escribir marcador: {ex.Message}");
        }
    }
}
