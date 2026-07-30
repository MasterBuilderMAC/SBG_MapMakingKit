#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public class BuildBundles
{
    // Captured automatically at compile time — the absolute path to this .cs file.
    private static string ScriptDirectory([CallerFilePath] string path = "")
        => Path.GetDirectoryName(path);

    private static string FixerToolPath
        => Path.Combine(ScriptDirectory(), "BundleNamespaceFixer.exe");

    [MenuItem("Build/Build AssetBundles")]
    static void Build()
    {
        string bundleDir = "Assets/Bundles";
        if (!Directory.Exists(bundleDir))
            Directory.CreateDirectory(bundleDir);

        var manifest = BuildPipeline.BuildAssetBundles(
            bundleDir,
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64
        );

        if (manifest == null)
        {
            UnityEngine.Debug.LogError("AssetBundle build failed, skipping namespace fix-up.");
            return;
        }

        string[] bundleNames = manifest.GetAllAssetBundles();
        var bundlePaths = new System.Collections.Generic.List<string>();
        foreach (var name in bundleNames)
            bundlePaths.Add(Path.Combine(bundleDir, name));

        RunNamespaceFixer(bundlePaths);
    }

    static void RunNamespaceFixer(System.Collections.Generic.List<string> bundlePaths)
    {
        if (bundlePaths.Count == 0) return;

        if (!File.Exists(FixerToolPath))
        {
            UnityEngine.Debug.LogError($"BundleNamespaceFixer.exe not found at: {FixerToolPath}");
            return;
        }

        string args = string.Join(" ", bundlePaths.ConvertAll(p => $"\"{Path.GetFullPath(p)}\""));

        var psi = new ProcessStartInfo
        {
            FileName = FixerToolPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi);
        process.WaitForExit();

        UnityEngine.Debug.Log(process.StandardOutput.ReadToEnd());

        string err = process.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(err))
            UnityEngine.Debug.LogError(err);

        if (process.ExitCode != 0)
            UnityEngine.Debug.LogError("BundleNamespaceFixer exited with an error.");
        else
            UnityEngine.Debug.Log("Bundle namespace fix-up complete.");
    }

    [MenuItem("Build/Clear All AssetBundle Names")]
    static void ClearAllBundleNames()
    {
        string[] allBundleNames = AssetDatabase.GetAllAssetBundleNames();
        foreach (string name in allBundleNames)
        {
            AssetDatabase.RemoveAssetBundleName(name, true);
        }
        AssetDatabase.Refresh();
    }
}
#endif
