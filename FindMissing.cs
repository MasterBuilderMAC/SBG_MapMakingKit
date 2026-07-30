#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class FindMissingScripts
{
    [MenuItem("Build/Find Missing Scripts (Open Scene)")]
    static void Find()
    {
        var report = new List<string>();
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();

        foreach (var root in scene.GetRootGameObjects())
            ScanGameObject(root, scene.path, report);

        if (report.Count == 0)
            Debug.Log($"No missing scripts found in scene '{scene.name}'.");
        else
            Debug.Log($"Missing script scan complete. Found {report.Count} object(s) in '{scene.name}':\n" + string.Join("\n", report));
    }

    static void ScanGameObject(GameObject go, string scenePath, List<string> report)
    {
        var components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                report.Add($"[{scenePath}] GameObject '{GetHierarchyPath(go)}' has a missing script at component index {i}");
            }
        }

        foreach (Transform child in go.transform)
            ScanGameObject(child.gameObject, scenePath, report);
    }

    static string GetHierarchyPath(GameObject go)
    {
        string path = go.name;
        var t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }
}
#endif