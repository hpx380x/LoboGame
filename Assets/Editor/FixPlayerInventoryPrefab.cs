using UnityEditor;
using UnityEngine;
using Core.Enums;

public class FixPlayerInventoryPrefab
{
    [MenuItem("Tools/Fix Player Inventory Prefab")]
    public static void FixPrefab()
    {
        string prefabPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabRoot != null)
        {
            PlayerInventory inventory = prefabRoot.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                Transform hand = prefabRoot.transform.Find("Skeleton/Hips/Spine/Chest/UpperChest/Right_Shoulder/Right_UpperArm/Right_LowerArm/Right_Hand");
                if(hand == null) {
                    Debug.LogError("No se encontró la mano del jugador.");
                    return;
                }

                SerializedObject so = new SerializedObject(inventory);
                SerializedProperty listProp = so.FindProperty("modelosVisuales");
                
                // Limpiar lista
                listProp.ClearArray();
                
                // Mapeos deseados
                var mappings = new (TipoObjeto tipo, string nombreObjeto)[] {
                    (TipoObjeto.Antorcha, "Antorcha"),
                    (TipoObjeto.DagaCazador, "Daga"),
                    (TipoObjeto.PocionVelocidad, "PocionVelocidad"),
                    (TipoObjeto.PocionMuerte, "PocionMuerte"),
                    (TipoObjeto.PocionVida, "PocionRevivir"),
                    (TipoObjeto.BombaApestosa, "BombaApestosa"),
                    (TipoObjeto.LupaHuella, "Lupa"),
                    (TipoObjeto.ManzanaOro, "Manzanadeoro")
                };

                int index = 0;
                foreach (var map in mappings)
                {
                    Transform t = hand.Find(map.nombreObjeto);
                    if (t != null)
                    {
                        listProp.InsertArrayElementAtIndex(index);
                        SerializedProperty pMapping = listProp.GetArrayElementAtIndex(index);
                        pMapping.FindPropertyRelative("tipo").enumValueIndex = (int)map.tipo;
                        pMapping.FindPropertyRelative("modelo").objectReferenceValue = t.gameObject;
                        
                        // Asegurar que estén apagados por defecto
                        t.gameObject.SetActive(false);
                        index++;
                    }
                }
                
                so.ApplyModifiedProperties();
                PrefabUtility.SavePrefabAsset(prefabRoot);
                Debug.Log($"Prefab PlayerArmature actualizado. Se han enlazado {index} objetos al inventario.");
            }
            else
            {
                Debug.LogError("No se encontró PlayerInventory en PlayerArmature.");
            }
        }
    }
}
