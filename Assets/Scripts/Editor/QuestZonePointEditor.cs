#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Core.QuestSystem;

[CustomEditor(typeof(QuestZonePoint))]
public class QuestZonePointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        QuestZonePoint point = (QuestZonePoint)target;

        EditorGUILayout.Space(12);
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f, 1f); // Verde resplandeciente
        if (GUILayout.Button("✨ GENERAR Y CONECTAR HUD 3D", GUILayout.Height(35)))
        {
            point.GenerarHUD3DEnEditor();
        }
        GUI.backgroundColor = Color.white;
    }
}
#endif
