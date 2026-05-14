using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

namespace CustomMaps
{

    // Here are some basic resources on code style and naming conventions to help
    // you in your first CSharp plugin!
    // https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
    // https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names
    // https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-namespaces

    // This BepInAutoPlugin attribute comes from the Hamunii.BepInEx.AutoPlugin
    // NuGet package, and it will generate the BepInPlugin attribute for you!
    // For more info, see https://github.com/Hamunii/BepInEx.AutoPlugin
    [BepInAutoPlugin]
    public partial class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; } = null!;
        public static Plugin? Instance { get; private set; }

        //for the menu to have the custom tab with names
        public static CourseData CustomCourse { get; set; } = null!;
        public static Dictionary<string, string> CustomLocalizedStrings = new Dictionary<string, string>();


        //for loading custom scenes
        public static List<string> ScenePaths = new List<string>();
        public static List<string> SceneGuids = new List<string>();
        

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            var harmony = new Harmony("com.github.MasterBuilderMAC.LocalizationRipper");

            SceneManager.sceneLoaded += Patch.OnSceneLoaded;

            harmony.PatchAll();
            Log.LogInfo($"Plugin {Name} is loaded!");
        }

        

    }

    public static class Patch
    {
        public static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            bool isCustomScene = Plugin.ScenePaths.Any(p => p.Contains(scene.name));
            bool isDrivingRange = scene.name.Equals("Driving range");
            Plugin.Log.LogDebug("Scene Loaded: " + scene.name);

            //make sure its base game
            if (!isCustomScene)
            {
                Plugin.Instance.StartCoroutine(DumpAfterLoad());

            }
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

        static IEnumerator DumpAfterLoad()
        {
            yield return new WaitForSeconds(1f);

            // Dump LocalizeStringEvent
            foreach (var lse in GameObject.FindObjectsByType<LocalizeStringEvent>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (lse == null) continue;

                string path = GetPath(lse.transform);

                if (lse.StringReference == null)
                {
                    Plugin.Log.LogWarning($"[STRING] Path: {path} | StringReference is NULL");
                    continue;
                }

                Plugin.Log.LogInfo($"[STRING] Path: {path} | Table: {lse.StringReference.TableReference} | Key: {lse.StringReference.TableEntryReference}");
            }

            // Dump DropdownObject via reflection
            foreach (var mono in GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var type = mono.GetType();
                if (type.Name != "DropdownOption") continue;

                string path = GetPath(mono.transform);

                var localizeField = type.GetField("localized", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (localizeField == null) continue;

                var localizeDropdown = localizeField.GetValue(mono);
                if (localizeDropdown == null) continue;

                var optionsField = localizeDropdown.GetType()
                    .GetField("dropdownOptions", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (optionsField == null) continue;

                var options = optionsField.GetValue(localizeDropdown) as System.Collections.IList;
                if (options == null) continue;

                Plugin.Log.LogInfo($"[DROPDOWN] Path: {path} | Count: {options.Count}");
                for (int i = 0; i < options.Count; i++)
                {
                    var option = options[i];
                    if (option == null) { Plugin.Log.LogWarning($"Null entry [{i}]"); continue; }
                    var baseType = option.GetType().BaseType;
                    if (baseType == null) { Plugin.Log.LogWarning($"  [{i}] No base type on {option.GetType().Name}"); continue; }
                    var tableRef = baseType?.GetField("m_TableReference", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(option);
                    var entryRef = baseType?.GetField("m_TableEntryReference", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(option);
                    Plugin.Log.LogInfo($"  [{i}] Table: {tableRef} | Key: {entryRef}");
                }
            }
        }
    }
}
