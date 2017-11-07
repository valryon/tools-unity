#if UNITY_EDITOR
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// ScriptableObject helper
/// </summary>
/// <remarks>Source: http://www.jacobpennock.com/Blog/?page_id=715 </remarks>
public static class ScriptableObjectUtility
{
  public static T CreateAsset<T>() where T : ScriptableObject
  {
    return CreateAsset<T>("New " + typeof(T).ToString() + ".asset");
  }

  public static T CreateAsset<T>(string name) where T : ScriptableObject
  {
    if (name.EndsWith(".asset") == false)
    {
      name = name + ".asset";
    }

    string path = AssetDatabase.GetAssetPath(Selection.activeObject);
    if (path == "")
    {
      path = "Assets";
    }
    else if (Path.GetExtension(path) != "")
    {
      path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
    }

    return CreateAsset<T>(path, name);
  }

  public static T CreateAsset<T>(string path, string name) where T : ScriptableObject
  {
    if (name.EndsWith(".asset") == false)
    {
      name = name + ".asset";
    }

    string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + name);

    T asset = ScriptableObject.CreateInstance<T>();

    AssetDatabase.CreateAsset(asset, assetPathAndName);

    AssetDatabase.SaveAssets();
    EditorUtility.FocusProjectWindow();
    Selection.activeObject = asset;

    return asset;
  }
}
#endif