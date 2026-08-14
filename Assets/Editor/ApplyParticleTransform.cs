using UnityEngine;
using UnityEditor;

public class ApplyParticleTransform
{
    [MenuItem("Tools/Fix Watering Particles Transform")]
    public static void FixTransform()
    {
        // Try to find it in the current scene or selected object
        GameObject[] particles = GameObject.FindGameObjectsWithTag("Untagged");
        GameObject target = null;
        foreach (var p in Resources.FindObjectsOfTypeAll<ParticleSystem>())
        {
            if (p.gameObject.name.ToLower().Contains("water"))
            {
                target = p.gameObject;
                break;
            }
        }
        
        if (target != null)
        {
            Undo.RecordObject(target.transform, "Apply copied transform");
            target.transform.position = new Vector3(193.52391052246095f, 21.63947868347168f, 443.3198547363281f);
            target.transform.rotation = new Quaternion(-0.4847829341888428f, -0.01791217178106308f, -0.12723958492279054f, -0.8651448488235474f);
            target.transform.localScale = new Vector3(1f, 1f, 1f);
            Debug.Log("Transform aplicado a: " + target.name);
        }
        else
        {
            Debug.LogError("No se encontró el sistema de partículas del agua.");
        }
    }
}
