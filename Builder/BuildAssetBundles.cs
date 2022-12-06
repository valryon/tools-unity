#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

public static class BuildAssetBundles
{
  private const string BASE_PATH = "Builds/📦 ";
  private const string BUILD_PATH = BASE_PATH + "Asset Bundles/Build";

  
#if ENABLE_AB
  [MenuItem(BUILD_PATH, priority = 10)]
  public static void BuildAB()
  {
    Build();
  }
#endif

  public static void Build(BuildTarget target = BuildTarget.NoTarget)
  {
#if ENABLE_AB
        target = target == BuildTarget.NoTarget ? EditorUserBuildSettings.activeBuildTarget : target;
        string path = Application.streamingAssetsPath + "/" + AssetBundles.Utility.GetPlatformForAssetBundles(target);
        Debug.Log("Build AssetBundles platform=[" + target + "] path=[" + path + "]");

        if (Directory.Exists(path) == false)
        {
            Directory.CreateDirectory(path);
        }

        var manifest =
 BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.ChunkBasedCompression, target);
        if (manifest == null)
        {
            Debug.LogError("Error in AB build");
            return;
        }

        // Rename appropriately
        string validName = AssetBundles.Utility.GetPlatformForAssetBundles(target);
        foreach (var f in Directory.GetFiles(path))
        {
            if (Path.GetFileName(f).Contains("StreamingAssets"))
            {
                var ext = Path.GetExtension(f);
                if (ext != ".meta")
                {
                    var newFilename = Path.Combine(Path.GetDirectoryName(f), validName + ext);
                    File.Copy(f, newFilename, true);
                    File.Delete(f);
                }
            }
        }
#else
    Debug.LogError("Asset bundles disabled in this project.");
#endif
  }
}
#endif