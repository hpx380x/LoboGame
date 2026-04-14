#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

/// <summary>
/// Herramienta de Editor para detectar y ELIMINAR automaticamente:
/// - Transiciones con destino null
/// - Estados con AnimationClip eliminado (motion null) — CAUSA PRINCIPAL del crash
/// Soluciona el error "GenerateConnectionKey NullReferenceException".
/// </summary>
public class FixBrokenAnimatorTransitions : EditorWindow
{
    private List<string> _log = new List<string>();

    [MenuItem("Tools/Herramientas Lobo/Reparar Animator Roto (Escaner Completo)")]
    public static void ShowWindow()
    {
        GetWindow<FixBrokenAnimatorTransitions>("Reparar Animator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Escaner y Reparador de Animators Rotos", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Escanea TODOS los Animator Controllers del proyecto y elimina:\n" +
            "- Estados con clip de animacion NULL (causa principal del crash)\n" +
            "- Transiciones con destino NULL\n\n" +
            "ADVERTENCIA: Los estados eliminados no se pueden recuperar. Guarda tu proyecto antes.", 
            MessageType.Warning);
        EditorGUILayout.Space();

        if (GUILayout.Button("ESCANEAR Y REPARAR TODO EL PROYECTO", GUILayout.Height(50)))
        {
            _log.Clear();
            ScanAll();
        }

        EditorGUILayout.Space();

        if (_log.Count > 0)
        {
            GUILayout.Label("Resultado:", EditorStyles.boldLabel);
            // Scrollview para ver todos los resultados
            foreach (var line in _log)
            {
                EditorGUILayout.HelpBox(line, MessageType.Info);
            }
        }
    }

    private void ScanAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimatorController");
        int total = 0;

        _log.Add($"Escaneando {guids.Length} controladores...");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl == null) continue;

            int issues = ScanController(ctrl);
            if (issues > 0)
                _log.Add($"[{ctrl.name}] Total: {issues} problema(s) reparado(s). Ruta: {path}");

            total += issues;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (total == 0)
            _log.Add("No se encontraron problemas adicionales. El Animator deberia funcionar correctamente.");
        else
            _log.Add($"Reparacion completa: {total} problema(s) corregido(s). Cierra y vuelve a abrir el Animator para verificar.");
    }

    private int ScanController(AnimatorController ctrl)
    {
        int issues = 0;
        foreach (var layer in ctrl.layers)
            issues += ScanStateMachine(layer.stateMachine, ctrl);
        return issues;
    }

    private int ScanStateMachine(AnimatorStateMachine sm, AnimatorController ctrl)
    {
        int issues = 0;

        // 1. Eliminar transiciones desde AnyState con destino null
        var anyTrans = new List<AnimatorStateTransition>(sm.anyStateTransitions);
        foreach (var t in anyTrans)
        {
            if (t == null || (t.destinationState == null && t.destinationStateMachine == null))
            {
                sm.RemoveAnyStateTransition(t);
                _log.Add($"  [{ctrl.name}] Eliminada: transicion rota desde AnyState.");
                issues++;
            }
        }

        // 2. Eliminar estados con motion (clip) NULL — ESTA ES LA CAUSA PRINCIPAL
        var statesToRemove = new List<AnimatorState>();
        foreach (var childState in sm.states)
        {
            if (childState.state == null) continue;

            if (childState.state.motion == null)
            {
                statesToRemove.Add(childState.state);
                _log.Add($"  [{ctrl.name}] Eliminado: estado '{childState.state.name}' (clip de animacion NULL).");
                issues++;
            }
            else
            {
                // 3. Eliminar transiciones del estado con destino null
                var stateTrans = new List<AnimatorStateTransition>(childState.state.transitions);
                foreach (var t in stateTrans)
                {
                    if (t == null || (t.destinationState == null && t.destinationStateMachine == null && !t.isExit))
                    {
                        childState.state.RemoveTransition(t);
                        _log.Add($"  [{ctrl.name}] Eliminada: transicion rota desde '{childState.state.name}'.");
                        issues++;
                    }
                }
            }
        }

        // Eliminar los estados marcados (hacerlo fuera del foreach para evitar modificar la coleccion)
        foreach (var state in statesToRemove)
            sm.RemoveState(state);

        // 4. Recursivo: submaquinas
        foreach (var sub in sm.stateMachines)
        {
            if (sub.stateMachine != null)
                issues += ScanStateMachine(sub.stateMachine, ctrl);
        }

        return issues;
    }
}
#endif
