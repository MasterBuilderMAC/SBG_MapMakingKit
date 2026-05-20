#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class FixPrefabShaders : EditorWindow
{
    [MenuItem("Tools/Fix Unassigned Shaders on Prefabs")]
    public static void FixShaders()
    {
        var shader = Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogError("Could not find Standard shader");
            return;
        }

        int fixedCount = 0;
        int prefabCount = 0;

        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/GameObject" });
        Debug.Log($"Found {guids.Length} prefabs in Assets/GameObject/");

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            prefabCount++;

            bool prefabModified = false;

            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterials == null) continue;
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null) continue;
                    if (mat.shader == null ||
                        mat.shader.name == "Hidden/InternalErrorShader" ||
                        string.IsNullOrEmpty(mat.shader.name))
                    {
                        Undo.RecordObject(mat, "Fix Shader");
                        mat.shader = shader;
                        EditorUtility.SetDirty(mat);
                        fixedCount++;
                        prefabModified = true;
                        Debug.Log($"Fixed shader on material '{mat.name}' in prefab '{path}'");
                    }
                }
            }

            if (prefabModified)
                EditorUtility.SetDirty(prefab);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Done — Checked {prefabCount} prefabs, fixed {fixedCount} materials");
    }

    [MenuItem("Tools/Fix Unassigned Shaders in Open Scene")]
    public static void FixShadersInScene()
    {
        var shader = Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogError("Could not find Standard shader");
            return;
        }

        int fixedCount = 0;
        int objectCount = 0;

        foreach (var renderer in GameObject.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (renderer.sharedMaterials == null) continue;
            objectCount++;

            var mats = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                if (mats[i].shader == null ||
                    mats[i].shader.name == "Hidden/InternalErrorShader" ||
                    string.IsNullOrEmpty(mats[i].shader.name))
                {
                    Undo.RecordObject(renderer, "Fix Shader");
                    mats[i].shader = shader;
                    EditorUtility.SetDirty(mats[i]);
                    fixedCount++;
                    changed = true;
                    Debug.Log($"Fixed shader on '{mats[i].name}' on GameObject '{renderer.gameObject.name}'");
                }
            }

            if (changed)
            {
                renderer.sharedMaterials = mats;
                EditorUtility.SetDirty(renderer);
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        AssetDatabase.SaveAssets();
        Debug.Log($"Done — Checked {objectCount} renderers, fixed {fixedCount} materials in open scene");
    }
}
#endif
