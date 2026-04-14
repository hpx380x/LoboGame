using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TaskPoint))]
public class TaskPointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Fuerza el uso del inspector por defecto de Unity, 
        // esquivando el error de Odin Inspector (TypeLoadException) en Unity 6
        base.OnInspectorGUI();
    }
}
