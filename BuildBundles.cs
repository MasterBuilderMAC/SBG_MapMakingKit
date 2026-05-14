using UnityEditor;
using System.IO;

public class BuildBundles
{
    [MenuItem("Build/Build AssetBundles")]
    static void Build()
    {
        string bundleDir = "Assets/Bundles";
        if (!Directory.Exists(bundleDir))
            Directory.CreateDirectory(bundleDir);

        BuildPipeline.BuildAssetBundles(
            bundleDir,
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64
        );
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