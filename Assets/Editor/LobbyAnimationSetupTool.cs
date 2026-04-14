#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Herramienta para asignar masivamente los clips de animacion de sentado
/// a todos los componentes RandomLobbySit de la escena de una sola vez.
/// </summary>
public class LobbyAnimationSetupTool : EditorWindow
{
    private AnimationClip _baseSittingIdle;
    private AnimationClip _actionPlaceholder;
    private List<AnimationClip> _randomClips = new List<AnimationClip>();
    private Vector2 _scroll;

    [MenuItem("Tools/Herramientas Lobo/Configurar Animaciones del Lobby")]
    public static void ShowWindow()
    {
        GetWindow<LobbyAnimationSetupTool>("Setup Animaciones Lobby");
    }

    private void OnGUI()
    {
        GUILayout.Label("Configurador Masivo de Animaciones de Sentado", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Asigna los clips UNA SOLA VEZ aqui y se aplicaran automaticamente " +
            "a TODOS los personajes SM_Chr de la escena.",
            MessageType.Info);

        EditorGUILayout.Space();

        // --- Clip Base Idle ---
        GUILayout.Label("1. Clip BASE de sentado (Ranura 0 del Blend Tree):", EditorStyles.boldLabel);
        _baseSittingIdle = (AnimationClip)EditorGUILayout.ObjectField(_baseSittingIdle, typeof(AnimationClip), false);

        EditorGUILayout.Space();

        // --- Placeholder ---
        GUILayout.Label("2. Clip PLACEHOLDER (el que pusiste en Ranura 1 del Blend Tree):", EditorStyles.boldLabel);
        _actionPlaceholder = (AnimationClip)EditorGUILayout.ObjectField(_actionPlaceholder, typeof(AnimationClip), false);

        EditorGUILayout.Space();

        // --- Lista de clips aleatorios ---
        GUILayout.Label("3. Clips ALEATORIOS (todos los de la lista):", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(150));
        for (int i = 0; i < _randomClips.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            _randomClips[i] = (AnimationClip)EditorGUILayout.ObjectField(_randomClips[i], typeof(AnimationClip), false);
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                _randomClips.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("+ Añadir slot"))
            _randomClips.Add(null);

        // Boton de auto-busqueda
        if (GUILayout.Button("🔍 Auto-detectar clips de Assets/Animations/"))
            AutoDetectClips();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // --- Boton principal ---
        bool puedeAplicar = _baseSittingIdle != null && _actionPlaceholder != null && _randomClips.Count > 0;
        GUI.enabled = puedeAplicar;
        if (GUILayout.Button("APLICAR A TODOS LOS PERSONAJES DE LA ESCENA", GUILayout.Height(50)))
            AplicarATodos();
        GUI.enabled = true;

        if (!puedeAplicar)
            EditorGUILayout.HelpBox("Asigna al menos: Base Idle, Placeholder y 1 clip aleatorio.", MessageType.Warning);
    }

    private void AutoDetectClips()
    {
        // Busca automaticamente los FBX de sentado en Assets/Animations
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Animations" });

        _randomClips.Clear();
        _baseSittingIdle = null;
        _actionPlaceholder = null;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // Los FBX tienen sub-assets, hay que cargarlos correctamente
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    string nombre = clip.name.ToLower();

                    if ((nombre.Contains("sitting idle") || nombre.Contains("sitting idle (2)")) && _baseSittingIdle == null)
                    {
                        _baseSittingIdle = clip;
                        _actionPlaceholder = clip; // Ponemos el mismo como placeholder inicial
                    }
                    else
                    {
                        _randomClips.Add(clip);
                    }
                }
            }
        }

        if (_baseSittingIdle != null)
            Debug.Log($"[LobbySetup] Auto-detectados: 1 base idle + {_randomClips.Count} clips aleatorios.");
        else
            Debug.LogWarning("[LobbySetup] No se encontro ningun 'Sitting Idle' en Assets/Animations/.");
    }

    private void AplicarATodos()
    {
        // Limpia la lista de nulls
        _randomClips.RemoveAll(c => c == null);

        var componentes = FindObjectsByType<RandomLobbySit>(FindObjectsSortMode.None);
        int count = 0;

        foreach (var comp in componentes)
        {
            Undo.RecordObject(comp, "Asignar clips RandomLobbySit");

            // Usamos SerializedObject para acceder a campos privados serializados
            var so = new SerializedObject(comp);
            so.FindProperty("baseSittingIdle").objectReferenceValue = _baseSittingIdle;
            so.FindProperty("actionSlotPlaceholder").objectReferenceValue = _actionPlaceholder;

            var listaProp = so.FindProperty("randomSittingClips");
            listaProp.ClearArray();
            for (int i = 0; i < _randomClips.Count; i++)
            {
                listaProp.InsertArrayElementAtIndex(i);
                listaProp.GetArrayElementAtIndex(i).objectReferenceValue = _randomClips[i];
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(comp);
            count++;
        }

        Debug.Log($"[LobbySetup] Clips asignados a {count} componentes RandomLobbySit en la escena.");
        EditorGUILayout.HelpBox($"Listo: {count} personajes configurados.", MessageType.Info);
    }
}
#endif
