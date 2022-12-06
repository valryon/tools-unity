using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Builder
{
  public static class BuilderUtils
  {
    public static string BrowseField(string text, string value, string extension)
    {
      EditorGUILayout.BeginHorizontal();

      value = EditorGUILayout.TextField(text, value);
      if (GUILayout.Button("...", GUILayout.Width(50)))
      {
        string newPath = null;

        if (string.IsNullOrEmpty(extension))
        {
          newPath = EditorUtility.OpenFolderPanel("Select folder", "", "");
        }
        else
        {
          newPath = EditorUtility.OpenFilePanel("Select save file", "", extension);
        }

        if (string.IsNullOrEmpty(newPath) == false)
        {
          value = newPath;
        }
      }

      EditorGUILayout.EndHorizontal();

      return value;
    }

    public static T[] FindAllAssets<T>() where T : Object
    {
      string searchTerm = "t:" + typeof(T).Name;
      return AssetDatabase.FindAssets(searchTerm)
        .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
        .ToArray();
    }

    public static string GetPlatformForAssetBundles(BuildTarget target)
    {
      switch (target)
      {
        case BuildTarget.Android:
          return "Android";
        case BuildTarget.iOS:
          return "iOS";
        case BuildTarget.tvOS:
          return "tvOS";
        case BuildTarget.WebGL:
          return "WebGL";
        case BuildTarget.StandaloneWindows:
        case BuildTarget.StandaloneWindows64:
          return "StandaloneWindows";
#if UNITY_2017_4_OR_NEWER
        case BuildTarget.StandaloneOSX:
          return "StandaloneOSX";
#else
                case BuildTarget.StandaloneOSXIntel:
                case BuildTarget.StandaloneOSXIntel64:
                    return "StandaloneOSXIntel";
#endif
        // Add more build targets for your own.
        // If you add more targets, don't forget to add the same platforms to the function below.
        case BuildTarget.StandaloneLinux64:
          return "StandaloneLinux";
#if UNITY_SWITCH
                case BuildTarget.Switch:
                    return "Switch";
#endif
        default:
          Debug.Log("Unknown BuildTarget: Using Default Enum Name: " + target);
          return target.ToString();
      }
    }
  }
}