#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. Dibujar el inspector por defecto
        DrawDefaultInspector();

        GameManager manager = (GameManager)target;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("MENÚ DE ADMINISTRACIÓN (LOBOGAME)", EditorStyles.boldLabel);

        // Comprobamos si la partida está activa en modo Play
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("El menú de administración solo está disponible en modo de ejecución (Play Mode).", MessageType.Info);
            return;
        }

        // Comprobamos si somos el Servidor / Host
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            EditorGUILayout.HelpBox("Este menú solo funciona en el SERVIDOR o HOST de la partida.", MessageType.Warning);
            return;
        }

        // --- CONTROL DE FASES ---
        EditorGUILayout.BeginVertical("box");
        GUI.backgroundColor = new Color(0.6f, 0.8f, 1.0f); // Azul claro para fases
        EditorGUILayout.LabelField("Control de Fases de la Partida", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Forzar Día", GUILayout.Height(30)))
        {
            manager.currentPhase.Value = GamePhase.Dia;
            Debug.Log("[Admin Menu] Fase forzada a: DIA");
        }
        if (GUILayout.Button("Forzar Noche", GUILayout.Height(30)))
        {
            manager.currentPhase.Value = GamePhase.Noche;
            Debug.Log("[Admin Menu] Fase forzada a: NOCHE");
        }
        if (GUILayout.Button("Forzar Votación", GUILayout.Height(30)))
        {
            manager.currentPhase.Value = GamePhase.Votacion;
            Debug.Log("[Admin Menu] Fase forzada a: VOTACION");
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // --- GESTIÓN DE MISIONES ---
        EditorGUILayout.BeginVertical("box");
        GUI.backgroundColor = new Color(0.7f, 1.0f, 0.7f); // Verde para misiones
        EditorGUILayout.LabelField("Acciones de Misión (DEBUG)", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Asignar Misión a Jugador", GUILayout.Height(25)))
        {
            manager.DebugForceAssignQuest();
        }
        if (GUILayout.Button("Asignar Misión a TODOS", GUILayout.Height(25)))
        {
            manager.DebugForceAssignQuestToAll();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // --- CONTROL DE JUGADORES / CHEATS ---
        EditorGUILayout.BeginVertical("box");
        GUI.backgroundColor = new Color(1.0f, 0.7f, 0.7f); // Rojo/Naranja para cheats
        EditorGUILayout.LabelField("Comandos y Cheats de Servidor", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Dar 100 Oro a Todos", GUILayout.Height(25)))
        {
            DarMonedasATodos(100);
        }
        if (GUILayout.Button("Revivir a Todos", GUILayout.Height(25)))
        {
            RevivirATodos();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);
        if (GUILayout.Button("Matar a Todos (Aldeanos y Lobos)", GUILayout.Height(25)))
        {
            MatarATodos();
        }
        EditorGUILayout.EndVertical();
        
        GUI.backgroundColor = Color.white; // Restaurar color por defecto
    }

    private void DarMonedasATodos(int cantidad)
    {
        int total = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
        {
            if (client.PlayerObject != null)
            {
                var inv = client.PlayerObject.GetComponent<PlayerInventory>();
                if (inv != null)
                {
                    inv.monedas.Value += cantidad;
                    total++;
                }
            }
        }
        Debug.Log($"[Admin Menu] Se otorgaron {cantidad} monedas a {total} jugadores.");
    }

    private void RevivirATodos()
    {
        int total = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
        {
            if (client.PlayerObject != null)
            {
                var ps = client.PlayerObject.GetComponent<PlayerState>();
                if (ps != null && ps.isDead.Value)
                {
                    ps.isDead.Value = false;
                    total++;
                }
            }
        }
        Debug.Log($"[Admin Menu] Se han revivido {total} jugadores.");
    }

    private void MatarATodos()
    {
        int total = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
        {
            if (client.PlayerObject != null)
            {
                var ps = client.PlayerObject.GetComponent<PlayerState>();
                if (ps != null && !ps.isDead.Value)
                {
                    ps.isDead.Value = true;
                    total++;
                }
            }
        }
        Debug.Log($"[Admin Menu] Se han eliminado {total} jugadores.");
    }
}
#endif
