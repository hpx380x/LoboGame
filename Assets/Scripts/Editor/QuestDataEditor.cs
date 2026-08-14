#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Core.QuestSystem;

// Este Editor Personalizado esquiva el error de compatibilidad de Odin Inspector
// con Unity 6 al dibujar listas (CollectionDrawer) usando el inspector clásico de Unity.
[CustomEditor(typeof(QuestData))]
public class QuestDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Forzamos el dibujado 100% nativo de Unity, bloqueando cualquier infiltración de Odin
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("questID"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("nombreMision"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("descripcionMision"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pasos"), true); // El 'true' dibuja la lista
        EditorGUILayout.PropertyField(serializedObject.FindProperty("objetoRecompensa"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("oroRecompensa"));
        
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("esMisionBasica"));
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Configuración de Interacción Modular", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("animationInteractionType"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("toolVisualName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("interactionDuration"));
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
