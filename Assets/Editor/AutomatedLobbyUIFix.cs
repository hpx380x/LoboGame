using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class AutomatedLobbyUIFix
{
    static AutomatedLobbyUIFix()
    {
        EditorApplication.delayCall += ExecuteModification;
    }

    private static void ExecuteModification()
    {
        string markerPath = "Assets/Editor/AutomatedLobbyUIFixRun.txt";
        if (System.IO.File.Exists(markerPath)) return;

        bool sceneDirty = false;

        // 1. Revisar si hay un EventSystem (Sin esto los botones no existen para el ratón)
        EventSystem evtSystem = Object.FindAnyObjectByType<EventSystem>();
        if (evtSystem == null)
        {
            GameObject evtObj = new GameObject("EventSystem");
            evtObj.AddComponent<EventSystem>();
            evtObj.AddComponent<StandaloneInputModule>();
            sceneDirty = true;
            Debug.Log("<color=red>[LobbyFix] ERROR CORREGIDO: Tu escena no tenía 'EventSystem'. Esto hacía imposible pulsar CUALQUIER botón.</color>");
        }

        // 2. Revisar el Canvas del Lobby y su Graphic Raycaster (el detector de clics)
        GameObject lobbyCanvasObj = GameObject.Find("Canvas"); // El canvas original de Host/Client suele llamarse así
        if (lobbyCanvasObj != null)
        {
            GraphicRaycaster raycaster = lobbyCanvasObj.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                lobbyCanvasObj.AddComponent<GraphicRaycaster>();
                sceneDirty = true;
                Debug.Log("<color=red>[LobbyFix] ERROR CORREGIDO: Al Canvas le faltaba el 'Graphic Raycaster' para oler los clics.</color>");
            }
            else if (!raycaster.enabled)
            {
                raycaster.enabled = true;
                sceneDirty = true;
                Debug.Log("<color=red>[LobbyFix] ERROR CORREGIDO: El 'Graphic Raycaster' estaba apagado.</color>");
            }

            // Asegurarnos de que el Canvas_GameManager (el botón verde) no lo tape con orden 99
            Canvas canvas = lobbyCanvasObj.GetComponent<Canvas>();
            if (canvas != null && canvas.sortingOrder < 100)
            {
                canvas.sortingOrder = 100; // Lo ponemos por encima de TODO mientras estemos en Lobby
                sceneDirty = true;
                Debug.Log("[LobbyFix] Elevada la jerarquía visual del Lobby Canvas para que nadie lo bloquee.");
            }
        }
        else
        {
            Debug.LogWarning("[LobbyFix] No pude encontrar tu objeto 'Canvas' del Lobby. Asegúrate de llamarlo 'Canvas'.");
        }

        // 3. Revisar si se rompieron las asignaciones de referencias en tu LobbyUI.cs
        LobbyUI lobbyCode = Object.FindAnyObjectByType<LobbyUI>();
        if (lobbyCode != null)
        {
            SerializedObject so = new SerializedObject(lobbyCode);
            
            SerializedProperty hostBtnProp = so.FindProperty("hostButton");
            SerializedProperty clientBtnProp = so.FindProperty("clientButton");
            SerializedProperty nmProp = so.FindProperty("networkManager");

            if (hostBtnProp != null && hostBtnProp.objectReferenceValue == null)
                Debug.LogError("<color=orange>[LobbyFix] ALERTA MANUAL: Tienes que pinchar tu objeto que tiene LobbyUI y arrastrarle el botón 'Host' en el hueco vacío del Inspector.</color>");

            if (clientBtnProp != null && clientBtnProp.objectReferenceValue == null)
                Debug.LogError("<color=orange>[LobbyFix] ALERTA MANUAL: Tienes que pinchar tu objeto que tiene LobbyUI y arrastrarle el botón 'Client' en el hueco vacío del Inspector.</color>");

            if (nmProp != null && nmProp.objectReferenceValue == null)
            {
                // Autoarrastramos el NetworkManager si se perdió
                Unity.Netcode.NetworkManager nm = Object.FindAnyObjectByType<Unity.Netcode.NetworkManager>();
                if (nm != null)
                {
                    nmProp.objectReferenceValue = nm;
                    so.ApplyModifiedProperties();
                    sceneDirty = true;
                    Debug.Log("<color=red>[LobbyFix] ERROR CORREGIDO: El LobbyUI había perdido conexión con el NetworkManager. Lo he vuelto a vincular.</color>");
                }
            }
        }

        if (sceneDirty)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("<color=green>[LobbyFix] Escaneo automático Anti-Huelga de Botones finalizado. Se han guardado medidas de emergencia.</color>");
        }

        System.IO.File.WriteAllText(markerPath, "Done");
    }
}

