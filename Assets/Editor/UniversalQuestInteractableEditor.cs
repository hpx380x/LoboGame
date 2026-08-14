using UnityEditor;
using UnityEngine;
using Core.Environment;
using Core.Enums;

[CustomEditor(typeof(UniversalQuestInteractable))]
public class UniversalQuestInteractableEditor : Editor
{
    private SerializedProperty modoInteraccion;
    private SerializedProperty interaccionesRequeridas;
    private SerializedProperty nombreVisualManual;
    private SerializedProperty barraProgresoUI;
    private SerializedProperty questData;
    private SerializedProperty customUIMessage;
    private SerializedProperty materialAsignado;
    private SerializedProperty cantidadMaterialOtorgado;
    private SerializedProperty zonaUbicacion;
    private SerializedProperty tipoAnimacionRecogida;
    private SerializedProperty toolVisualNameLegacy;
    
    // Novedad: HUD 3D Manual
    private SerializedProperty hudObject;
    private SerializedProperty hudText;

    private SerializedProperty efectosVisualesAlCompletar;

    private SerializedProperty mecanica;
    private SerializedProperty pulsacionesRequeridas;
    private SerializedProperty esCajaRecogible;

    private SerializedProperty materialRequeridoParaEntrega;
    private SerializedProperty cantidadRequeridaParaEntrega;

    private void OnEnable()
    {
        modoInteraccion = serializedObject.FindProperty("modoInteraccion");
        mecanica = serializedObject.FindProperty("mecanica");
        pulsacionesRequeridas = serializedObject.FindProperty("pulsacionesRequeridas");
        esCajaRecogible = serializedObject.FindProperty("esCajaRecogible");
        
        interaccionesRequeridas = serializedObject.FindProperty("interaccionesRequeridas");
        nombreVisualManual = serializedObject.FindProperty("nombreVisualManual");
        barraProgresoUI = serializedObject.FindProperty("barraProgresoUI");
        questData = serializedObject.FindProperty("questData");
        customUIMessage = serializedObject.FindProperty("customUIMessage");
        materialAsignado = serializedObject.FindProperty("materialAsignado");
        cantidadMaterialOtorgado = serializedObject.FindProperty("cantidadMaterialOtorgado");
        zonaUbicacion = serializedObject.FindProperty("zonaUbicacion");
        tipoAnimacionRecogida = serializedObject.FindProperty("tipoAnimacionRecogida");
        toolVisualNameLegacy = serializedObject.FindProperty("toolVisualNameLegacy");
        
        hudObject = serializedObject.FindProperty("hudObject");
        hudText = serializedObject.FindProperty("hudText");
        
        // Novedad: Efectos Visuales
        efectosVisualesAlCompletar = serializedObject.FindProperty("efectosVisualesAlCompletar");

        materialRequeridoParaEntrega = serializedObject.FindProperty("materialRequeridoParaEntrega");
        cantidadRequeridaParaEntrega = serializedObject.FindProperty("cantidadRequeridaParaEntrega");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Configuración Principal", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(modoInteraccion);
        EditorGUILayout.PropertyField(mecanica);
        
        EditorGUILayout.Space(5);
        
        if (mecanica.enumValueIndex == (int)MecanicaInteraccion.AporrearBoton)
        {
            EditorGUILayout.LabelField("Configuración Aporrear", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(pulsacionesRequeridas);
        }
        else if (mecanica.enumValueIndex == (int)MecanicaInteraccion.Transportar)
        {
            EditorGUILayout.LabelField("Configuración Transportar", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(esCajaRecogible);
        }
        else if (mecanica.enumValueIndex == (int)MecanicaInteraccion.MantenerPulsado)
        {
            EditorGUILayout.LabelField("Múltiples Etapas", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(interaccionesRequeridas);
        }
        else if (mecanica.enumValueIndex == (int)MecanicaInteraccion.PuntoEntrega)
        {
            EditorGUILayout.LabelField("Configuración Punto de Entrega", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Deja el Material Requerido en 'Ninguno' para usar cajas físicas. De lo contrario, se consumirá el material seleccionado de la mochila.", MessageType.Info);
            EditorGUILayout.PropertyField(materialRequeridoParaEntrega);
            EditorGUILayout.PropertyField(cantidadRequeridaParaEntrega);
        }
        else if (mecanica.enumValueIndex == (int)MecanicaInteraccion.Restregar)
        {
            EditorGUILayout.LabelField("Configuración Shader de Limpieza / Restregar", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nombrePropiedadShader"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rendererObjetivoLimpieza"));
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Visual Seguro", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(nombreVisualManual);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("HUD 3D y Progreso", EditorStyles.boldLabel);
        if (mecanica.enumValueIndex != (int)MecanicaInteraccion.Transportar && mecanica.enumValueIndex != (int)MecanicaInteraccion.PuntoEntrega)
        {
            EditorGUILayout.PropertyField(barraProgresoUI);
        }
        EditorGUILayout.PropertyField(hudObject);
        EditorGUILayout.PropertyField(hudText);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Efectos Visuales (Ej: Velas)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(efectosVisualesAlCompletar, true);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Datos de la Misión", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(questData);
        EditorGUILayout.PropertyField(customUIMessage);

        EditorGUILayout.Space(10);
        
        // MODO RECOLECCIÓN
        if (modoInteraccion.enumValueIndex == (int)Core.Enums.InteractionMode.Recoleccion)
        {
            EditorGUILayout.LabelField("Sistema de Materiales (Recolección)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(materialAsignado);
            EditorGUILayout.PropertyField(cantidadMaterialOtorgado);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }

        // RETROCOMPATIBILIDAD (Ignorado si hay QuestData)
        if (questData.objectReferenceValue == null)
        {
            EditorGUILayout.LabelField("Retrocompatibilidad (Legacy Mode)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("Estas variables se usan si QuestData está vacío.", MessageType.Info);
            EditorGUILayout.PropertyField(zonaUbicacion);
            EditorGUILayout.PropertyField(tipoAnimacionRecogida);
            EditorGUILayout.PropertyField(toolVisualNameLegacy);
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
