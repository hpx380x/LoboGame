#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Herramienta de pintado de Prefabs sobre superficies 3D.
/// Funciona como el terrain painter de Unity pero para GameObjects.
/// Acceso: Tools > Herramientas Lobo > Prefab Painter
/// </summary>
public class PrefabPainterTool : EditorWindow
{
    // ============================
    //  MODELO DE DATOS
    // ============================
    [System.Serializable]
    private class PrefabEntry
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float weight = 1f;
        public bool enabled = true;
        public Texture2D preview;
    }

    // ============================
    //  ESTADO DE LA HERRAMIENTA
    // ============================
    private List<PrefabEntry> _palette = new List<PrefabEntry>();
    private bool _paintActive = false;

    // Pincel
    private float _brushRadius    = 2f;
    private int   _brushDensity   = 3;   // prefabs por pincelada
    private float _minSpacing     = 0.5f;// distancia minima entre instancias

    // Placement
    private bool  _alignToNormal  = true;
    private float _normalBlend    = 1f;  // 0=Y global, 1=normal superficie
    private float _yOffset        = 0f;
    private float _minScale       = 0.8f;
    private float _maxScale       = 1.2f;
    private float _minRotY        = 0f;
    private float _maxRotY        = 360f;
    private float _slopeLimit     = 60f; // no pintar en pendientes mayores

    // Filtros
    private LayerMask _paintMask  = -1; // All layers
    private Transform _parentContainer;

    // UI state
    private Vector2 _scroll;
    private bool _showBrushSettings  = true;
    private bool _showPlaceSettings  = true;
    private bool _showPalette        = true;
    private bool _eraseMode          = false;
    private float _eraseRadius       = 2f;
    private bool _eraseOnlyPalette   = true;

    // Internas
    private Vector3 _lastPaintPos    = Vector3.positiveInfinity;
    private static GUIStyle _headerStyle;
    private static GUIStyle _entryStyle;

    // ============================
    //  MENU
    // ============================
    [MenuItem("Tools/Herramientas Lobo/Prefab Painter %&p")]
    public static void ShowWindow()
    {
        var w = GetWindow<PrefabPainterTool>("🖌 Prefab Painter");
        w.minSize = new Vector2(300, 400);
    }

    // ============================
    //  LIFECYCLE
    // ============================
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        _paintActive = false;
    }

    // ============================
    //  VENTANA PRINCIPAL
    // ============================
    private void OnGUI()
    {
        InitStyles();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawHeader();
        DrawPaletteSection();
        DrawBrushSection();
        DrawPlacementSection();
        DrawContainerSection();
        DrawActivateButton();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("🖌  PREFAB PAINTER", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        Color prev = GUI.color;
        GUI.color = _paintActive ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.5f, 0.5f);
        GUILayout.Label(_paintActive ? "● ACTIVO" : "○ Inactivo", EditorStyles.boldLabel);
        GUI.color = prev;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);
    }

    private void DrawPaletteSection()
    {
        _showPalette = EditorGUILayout.BeginFoldoutHeaderGroup(_showPalette, "🎨 Paleta de Prefabs");
        if (_showPalette)
        {
            // Drop zone
            var dropRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "Arrastra Prefabs aquí", EditorStyles.helpBox);
            HandleDrop(dropRect);

            EditorGUILayout.Space(2);

            // Lista de prefabs
            for (int i = 0; i < _palette.Count; i++)
            {
                DrawPaletteEntry(i);
            }

            if (_palette.Count == 0)
            {
                EditorGUILayout.HelpBox("Arrastra prefabs desde el Project a la zona de arriba.", MessageType.Info);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawPaletteEntry(int i)
    {
        var entry = _palette[i];
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        // Preview thumbnail
        if (entry.preview == null && entry.prefab != null)
        {
            entry.preview = AssetPreview.GetAssetPreview(entry.prefab);
        }
        Rect previewRect = GUILayoutUtility.GetRect(40, 40, GUILayout.Width(40), GUILayout.Height(40));
        if (entry.preview != null)
            GUI.DrawTexture(previewRect, entry.preview, ScaleMode.ScaleToFit);
        else
            GUI.Box(previewRect, "?");

        EditorGUILayout.BeginVertical();
        entry.prefab   = (GameObject)EditorGUILayout.ObjectField(entry.prefab, typeof(GameObject), false);
        entry.enabled  = EditorGUILayout.Toggle("Activo", entry.enabled);
        entry.weight   = EditorGUILayout.Slider("Peso", entry.weight, 0f, 1f);
        EditorGUILayout.EndVertical();

        if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(40)))
        {
            _palette.RemoveAt(i);
            return;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawBrushSection()
    {
        EditorGUILayout.Space(4);
        _showBrushSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showBrushSettings, "🖌 Pincel");
        if (_showBrushSettings)
        {
            _eraseMode    = EditorGUILayout.Toggle("Modo Borrar (Shift)", _eraseMode);
            if (_eraseMode)
            {
                _eraseRadius = EditorGUILayout.Slider("Radio Borrador", _eraseRadius, 0.1f, 20f);
                _eraseOnlyPalette = EditorGUILayout.Toggle("Sólo borrar refs de la Paleta", _eraseOnlyPalette);
            }
            else
            {
                _brushRadius  = EditorGUILayout.Slider("Radio", _brushRadius, 0.1f, 30f);
                _brushDensity = EditorGUILayout.IntSlider("Densidad (prefabs/click)", _brushDensity, 1, 20);
                _minSpacing   = EditorGUILayout.Slider("Espaciado mínimo", _minSpacing, 0f, 5f);
                _slopeLimit   = EditorGUILayout.Slider("Límite de pendiente (°)", _slopeLimit, 0f, 90f);
            }

            EditorGUILayout.Space(2);
            _paintMask = EditorGUILayout.MaskField("Layers de pintura", _paintMask, GetLayerNames());
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawPlacementSection()
    {
        EditorGUILayout.Space(4);
        _showPlaceSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showPlaceSettings, "📐 Colocación");
        if (_showPlaceSettings)
        {
            _alignToNormal = EditorGUILayout.Toggle("Alinear a Normal", _alignToNormal);
            if (_alignToNormal)
                _normalBlend = EditorGUILayout.Slider("Mezcla Normal/Vertical", _normalBlend, 0f, 1f);
            _yOffset = EditorGUILayout.FloatField("Offset Y", _yOffset);

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Rotación Y aleatoria", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min", GUILayout.Width(25));
            _minRotY = EditorGUILayout.FloatField(_minRotY, GUILayout.Width(50));
            EditorGUILayout.LabelField("Max", GUILayout.Width(25));
            _maxRotY = EditorGUILayout.FloatField(_maxRotY, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Escala aleatoria (uniforme)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min", GUILayout.Width(25));
            _minScale = EditorGUILayout.FloatField(_minScale, GUILayout.Width(50));
            EditorGUILayout.LabelField("Max", GUILayout.Width(25));
            _maxScale = EditorGUILayout.FloatField(_maxScale, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawContainerSection()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("📁 Contenedor padre (opcional)", EditorStyles.boldLabel);
        _parentContainer = (Transform)EditorGUILayout.ObjectField(_parentContainer, typeof(Transform), true);
        if (_parentContainer == null)
        {
            EditorGUILayout.HelpBox("Sin contenedor: los prefabs se crean en raíz de la jerarquía.", MessageType.None);
        }
    }

    private void DrawActivateButton()
    {
        EditorGUILayout.Space(8);
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = _paintActive ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.9f, 0.9f, 0.4f);
        if (GUILayout.Button(_paintActive ? "🛑  DESACTIVAR PINCEL" : "▶  ACTIVAR PINCEL", GUILayout.Height(40)))
        {
            _paintActive = !_paintActive;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = prev;
        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Con el pincel ACTIVO:\n" +
            "  • Click/Drag izquierdo → Pintar\n" +
            "  • Shift + Click → Borrar\n" +
            "  • Rueda del ratón → Cambiar tamaño del pincel\n" +
            "  • Ctrl+Z → Deshacer",
            MessageType.Info);
    }

    // ============================
    //  ESCENA 3D
    // ============================
    private void OnSceneGUI(SceneView sceneView)
    {
        if (!_paintActive) return;

        Event e = Event.current;

        // Capturar rueda para cambiar tamaño del pincel
        if (e.type == EventType.ScrollWheel && e.control)
        {
            _brushRadius = Mathf.Clamp(_brushRadius - e.delta.y * 0.1f, 0.1f, 30f);
            Repaint();
            e.Use();
        }

        // Raycast al mundo
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, _paintMask))
        {
            DrawBrushCircle(hit.point, hit.normal, false);
            return;
        }

        bool isErase = e.shift || _eraseMode;
        float radius = isErase ? _eraseRadius : _brushRadius;

        // Dibujar pincel visual
        DrawBrushCircle(hit.point, hit.normal, isErase);

        // Prevenir seleccion mientras pintamos
        if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
        {
            if (e.button == 0)
            {
                GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);

                try
                {
                    if (isErase)
                        EraseInRadius(hit.point, radius);
                    else
                        PaintAtPoint(hit.point, hit.normal);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[PrefabPainterTool] Error interceptado: {ex.Message}");
                    GUIUtility.hotControl = 0; // Liberar control en caso de excepción para no romper el GUILayout
                }

                e.Use();
            }
        }

        if (e.type == EventType.MouseUp && e.button == 0)
        {
            GUIUtility.hotControl = 0;
            _lastPaintPos = Vector3.positiveInfinity;
            e.Use();
        }

        sceneView.Repaint();
    }

    private void DrawBrushCircle(Vector3 center, Vector3 normal, bool erase)
    {
        Handles.color = erase ? new Color(1f, 0.3f, 0.3f, 0.6f) : new Color(0.3f, 0.8f, 1f, 0.6f);
        float r = erase ? _eraseRadius : _brushRadius;
        Handles.DrawWireDisc(center, normal, r);
        Handles.color = erase ? new Color(1f, 0.3f, 0.3f, 0.1f) : new Color(0.3f, 0.8f, 1f, 0.1f);
        Handles.DrawSolidDisc(center, normal, r);
    }

    // ============================
    //  LÓGICA DE PINTURA
    // ============================
    private void PaintAtPoint(Vector3 center, Vector3 surfaceNormal)
    {
        if (_palette.Count == 0) return;

        // Comprobamos la pendiente
        float slope = Vector3.Angle(Vector3.up, surfaceNormal);
        if (slope > _slopeLimit) return;

        for (int i = 0; i < _brushDensity; i++)
        {
            // Posicion aleatoria dentro del circulo del pincel
            Vector2 rnd2D = Random.insideUnitCircle * _brushRadius;
            Vector3 spawnTest = center + new Vector3(rnd2D.x, 5f, rnd2D.y);

            if (!Physics.Raycast(spawnTest, Vector3.down, out RaycastHit spawnHit, 20f, _paintMask))
                continue;

            Vector3 spawnPos = spawnHit.point + Vector3.up * _yOffset;
            float slopeLocal = Vector3.Angle(Vector3.up, spawnHit.normal);
            if (slopeLocal > _slopeLimit) continue;

            // Espaciado minimo
            if (_minSpacing > 0f && Vector3.Distance(spawnPos, _lastPaintPos) < _minSpacing)
                continue;

            // Elegir prefab por peso
            GameObject prefabElegido = PickRandomPrefab();
            if (prefabElegido == null) continue;

            // Rotacion
            Quaternion baseRot = _alignToNormal
                ? Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, spawnHit.normal), _normalBlend)
                : Quaternion.identity;
            float rotY = Random.Range(_minRotY, _maxRotY);
            Quaternion finalRot = baseRot * Quaternion.Euler(0, rotY, 0);

            // Escala
            float scale = Random.Range(_minScale, _maxScale);

            // Instanciar asegurando validación de prefab para evitar NullReferenceException
            GameObject inst = null;
            if (PrefabUtility.IsPartOfPrefabAsset(prefabElegido))
            {
                inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabElegido);
            }
            else
            {
                // Fallback: si echaron un objeto de la jerarquía, usamos un Instantiate común
                inst = (GameObject)Object.Instantiate(prefabElegido);
            }

            if (inst == null) continue; // Protección extra si algo falló

            inst.transform.SetPositionAndRotation(spawnPos, finalRot);
            inst.transform.localScale = prefabElegido.transform.localScale * scale;

            if (_parentContainer != null)
                inst.transform.SetParent(_parentContainer, true);

            Undo.RegisterCreatedObjectUndo(inst, "Prefab Paint");
            _lastPaintPos = spawnPos;
        }
    }

    private void EraseInRadius(Vector3 center, float radius)
    {
        // Buscar todos los GameObjects con Renderer en el radio
        Collider[] colliders = Physics.OverlapSphere(center, radius);
        foreach (var col in colliders)
        {
            GameObject go = col.gameObject;
            // Solo borrar si es una instancia de prefab
            if (PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.Connected ||
                PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset)
            {
                // Si tiene parent, borramos el root
                GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
                if (root != null)
                {
                    // [NUEVO] Filtrar por paleta si est\u00e1 activado
                    if (_eraseOnlyPalette)
                    {
                        string instanceAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
                        bool matchFound = false;
                        
                        foreach (var e in _palette)
                        {
                            if (e.prefab != null)
                            {
                                string paletteAssetPath = AssetDatabase.GetAssetPath(e.prefab);
                                // Comparamos por ruta de disco, que es 100% infalible en Unity, 
                                // y SIN requerir que e.enabled sea true (as\u00ed pueden borrar prefabs apagados)
                                if (!string.IsNullOrEmpty(paletteAssetPath) && paletteAssetPath == instanceAssetPath)
                                {
                                    matchFound = true;
                                    break;
                                }
                            }
                        }
                        if (!matchFound) continue; // Si no est\u00e1 en la paleta, lo ignoramos
                    }

                    Undo.DestroyObjectImmediate(root);
                }
            }
        }
    }

    private GameObject PickRandomPrefab()
    {
        var enabled = new List<PrefabEntry>();
        float totalWeight = 0f;
        foreach (var e in _palette)
            if (e.enabled && e.prefab != null) { enabled.Add(e); totalWeight += e.weight; }

        if (enabled.Count == 0 || totalWeight <= 0f) return null;

        float rnd = Random.value * totalWeight;
        float acc = 0f;
        foreach (var e in enabled)
        {
            acc += e.weight;
            if (rnd <= acc) return e.prefab;
        }
        return enabled[enabled.Count - 1].prefab;
    }

    // ============================
    //  DRAG & DROP
    // ============================
    private void HandleDrop(Rect dropRect)
    {
        Event e = Event.current;
        if (!dropRect.Contains(e.mousePosition)) return;

        if (e.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            e.Use();
        }
        else if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject go)
                    _palette.Add(new PrefabEntry { prefab = go, weight = 1f, enabled = true });
            }
            e.Use();
        }
    }

    // ============================
    //  UTILIDADES
    // ============================
    private string[] GetLayerNames()
    {
        var names = new List<string>();
        for (int i = 0; i < 32; i++)
            names.Add(LayerMask.LayerToName(i));
        return names.ToArray();
    }

    private void InitStyles()
    {
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        }
    }
}
#endif
