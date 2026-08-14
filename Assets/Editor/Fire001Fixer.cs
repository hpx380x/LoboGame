#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class Fire001Fixer
{
    [InitializeOnLoadMethod]
    private static void FixFire001Prefabs()
    {
        string[] guids = AssetDatabase.FindAssets("Fire001 t:Prefab");
        if (guids == null || guids.Length == 0)
        {
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bool modified = false;
            ParticleSystem[] particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                if (!main.loop)
                {
                    main.loop = true;
                    modified = true;
                }
                if (main.stopAction != ParticleSystemStopAction.None)
                {
                    main.stopAction = ParticleSystemStopAction.None;
                    modified = true;
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Fire001 Fixer] Prefab Fire001 corregido con éxito en {path}. Se activó Looping y se eliminó Stop Action.");
            }
        }
    }
}
#endif
