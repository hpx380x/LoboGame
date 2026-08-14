using UnityEngine;
using UnityEditor;

public class ImportPolytopeURPPackage : Editor
{
    private const string PACKAGE_PATH = "Assets/Polytope Studio/Lowpoly_Environments/URP/PT_Nature_Free_URP_17.unitypackage";

    [MenuItem("Tools/Import Polytope URP Package (Automatic)")]
    public static void ImportPackage()
    {
        if (System.IO.File.Exists(PACKAGE_PATH))
        {
            Debug.Log("[ImportPolytopeURPPackage] Importando el paquete de URP oficial de Polytope Studio...");
            AssetDatabase.ImportPackage(PACKAGE_PATH, false);
            Debug.Log("[ImportPolytopeURPPackage] ¡Paquete importado correctamente!");
        }
        else
        {
            Debug.LogWarning($"[ImportPolytopeURPPackage] No se encontró el archivo .unitypackage en: {PACKAGE_PATH}");
        }
    }
}
