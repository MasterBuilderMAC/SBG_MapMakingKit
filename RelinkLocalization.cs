#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class RelinkLocalization : EditorWindow
{
    [MenuItem("Tools/Relink Localization")]
    public static void ShowWindow()
    {
        GetWindow<RelinkLocalization>("Relink Localization");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Relink All LocalizeStringEvents in Scene"))
        {
            var (stringMap, dropdownMap) = LoadMaps();
            if (stringMap != null)
                RelinkAll(stringMap, dropdownMap);
        }
    }

    static string GetTxtPath()
    {
        var guids = AssetDatabase.FindAssets("LogOutput");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("LogOutput.log"))
            {
                Debug.Log($"Found log at: {path}");
                return Path.GetFullPath(path);
            }
        }
        return null;
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        Transform current = t.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    static (Dictionary<string, (string table, string key)> strings,
            Dictionary<string, List<(string table, string key)>> dropdowns) LoadMaps()
    {
        var txtPath = GetTxtPath();
        if (txtPath == null)
        {
            Debug.LogError("log not found anywhere in project");
            return (null, null);
        }

        var lines = File.ReadAllLines(txtPath);
        Debug.Log($"Read {lines.Length} lines from log");

        var stringMap   = new Dictionary<string, (string table, string key)>();
        var dropdownMap = new Dictionary<string, List<(string table, string key)>>();

        // Format: TableReference(guid - UI)
        var stringRegexGuid = new Regex(
            @"\[STRING\] Path: (.+?) \| Table: TableReference\(([a-f0-9\-]+) - (\w+)\) \| Key: TableEntryReference\((\d+)\)"
        );
        // Format: TableReference(UI)
        var stringRegexSimple = new Regex(
            @"\[STRING\] Path: (.+?) \| Table: TableReference\((\w+)\) \| Key: TableEntryReference\((\d+)\)"
        );
        var dropdownHeaderRegex = new Regex(
            @"\[DROPDOWN\] Path: (.+?) \| Count: (\d+)"
        );
        var dropdownEntryRegexGuid = new Regex(
            @"\[(\d+)\] Table: TableReference\(([a-f0-9\-]+) - (\w+)\) \| Key: TableEntryReference\((\d+)\)"
        );
        var dropdownEntryRegexSimple = new Regex(
            @"\[(\d+)\] Table: TableReference\((\w+)\) \| Key: TableEntryReference\((\d+)\)"
        );

        string currentDropdownPath = null;

        foreach (var line in lines)
        {
            // Try guid string format first
            var m = stringRegexGuid.Match(line);
            if (m.Success)
            {
                currentDropdownPath = null;
                string path  = m.Groups[1].Value.Trim();
                string table = m.Groups[3].Value.Trim();
                string key   = m.Groups[4].Value.Trim();
                if (table != "Empty" && key != "Empty" && !stringMap.ContainsKey(path))
                    stringMap[path] = (table, key);
                continue;
            }

            // Try simple string format
            m = stringRegexSimple.Match(line);
            if (m.Success)
            {
                currentDropdownPath = null;
                string path  = m.Groups[1].Value.Trim();
                string table = m.Groups[2].Value.Trim();
                string key   = m.Groups[3].Value.Trim();
                if (table != "Empty" && key != "Empty" && !stringMap.ContainsKey(path))
                    stringMap[path] = (table, key);
                continue;
            }

            // Dropdown header
            m = dropdownHeaderRegex.Match(line);
            if (m.Success)
            {
                currentDropdownPath = m.Groups[1].Value.Trim();
                if (!dropdownMap.ContainsKey(currentDropdownPath))
                    dropdownMap[currentDropdownPath] = new List<(string, string)>();
                continue;
            }

            // Dropdown option - guid format
            if (currentDropdownPath != null)
            {
                m = dropdownEntryRegexGuid.Match(line);
                if (m.Success)
                {
                    string table = m.Groups[3].Value.Trim();
                    string key   = m.Groups[4].Value.Trim();
                    if (table != "Empty" && key != "Empty")
                        dropdownMap[currentDropdownPath].Add((table, key));
                    continue;
                }

                // Dropdown option - simple format
                m = dropdownEntryRegexSimple.Match(line);
                if (m.Success)
                {
                    string table = m.Groups[2].Value.Trim();
                    string key   = m.Groups[3].Value.Trim();
                    if (table != "Empty" && key != "Empty")
                        dropdownMap[currentDropdownPath].Add((table, key));
                }
            }
        }

        Debug.Log($"Loaded {stringMap.Count} string mappings and {dropdownMap.Count} dropdown mappings");
        return (stringMap, dropdownMap);
    }

    static void RelinkAll(
    Dictionary<string, (string table, string key)> stringMap,
    Dictionary<string, List<(string table, string key)>> dropdownMap)
    {
        int fixedCount = 0;
        int missing = 0;

        // Relink LocalizeStringEvent - always overwrite
        foreach (var lse in Resources.FindObjectsOfTypeAll<LocalizeStringEvent>())
        {
            if (!lse.gameObject.scene.isLoaded) continue;

            string path = GetPath(lse.transform);

            if (stringMap.TryGetValue(path, out var pair))
            {
                Undo.RecordObject(lse, "Relink Localization");
                lse.StringReference = new LocalizedString(pair.table, pair.key);
                EditorUtility.SetDirty(lse);
                fixedCount++;
                Debug.Log($"Fixed string: {path} -> {pair.table}/{pair.key}");
            }
            else
            {
                // Log everything not in map regardless of whether it's empty
                Debug.LogWarning($"No mapping found for path: {path}");
                missing++;
            }
        }

        // Relink DropdownOption - always overwrite
        // Find LocalizeDropdown type via reflection since we don't have a direct reference
        var allMonos = Resources.FindObjectsOfTypeAll<MonoBehaviour>();

        foreach (var mono in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
        {
            if (mono == null) continue;
            if (!mono.gameObject.scene.isLoaded) continue;
            if (mono.GetType().Name != "LocalizeDropdown") continue;

            // Go up two levels: Dropdown -> Option Contents -> Invert X
            var grandParent = mono.transform.parent?.parent;
            if (grandParent == null) continue;

            string path = GetPath(grandParent);
            Debug.Log($"LocalizeDropdown grandparent path: {path} | In map: {dropdownMap.ContainsKey(path)}");

            if (!dropdownMap.TryGetValue(path, out var optionsList)) continue;

            var dropdownOptionsField = mono.GetType()
                .GetField("dropdownOptions", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (dropdownOptionsField == null) { Debug.LogError($"No dropdownOptions on {path}"); continue; }

            var localizedStringType = typeof(LocalizedString);
            var baseType = localizedStringType.BaseType;
            var tableRefField = baseType?.GetField("m_TableReference",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var entryRefField = baseType?.GetField("m_TableEntryReference",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);


            if (tableRefField == null || entryRefField == null)
            {
                Debug.LogError($"Could not find base fields on LocalizedString for {path}");
                continue;
            }

            var newList = (System.Collections.IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(localizedStringType)
            );

            foreach (var (table, key) in optionsList)
            {
                // Use the public constructor directly instead of reflection
                var ls = new LocalizedString(table, long.Parse(key));
                newList.Add(ls);
                Debug.Log($"  Added option: {table}/{key}");
            }

            Undo.RecordObject(mono, "Relink Dropdown Localization");
            dropdownOptionsField.SetValue(mono, newList);
            EditorUtility.SetDirty(mono);
            fixedCount++;
            Debug.Log($"Fixed dropdown: {path} ({optionsList.Count} options)");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Done — Fixed: {fixedCount} | No mapping: {missing}");
    }
}
#endif