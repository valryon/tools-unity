#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// The super useful screenshoter window
/// </summary>
public class ScreenshotWindow : EditorWindow
{
  #region Menus

  [MenuItem("Tools/Screenshoter")]
  public static void ShowWindow()
  {
    GetWindow(typeof(ScreenshotWindow));
  }

  #endregion

  private string screenshotLocation = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);

  void OnGUI()
  {
    titleContent = new GUIContent("Screenshoter");

    EditorGUILayout.BeginVertical("Box");
    {
      EditorGUILayout.LabelField("Screenshoter", EditorStyles.boldLabel);

      resWidth = EditorGUILayout.IntField("Width", resWidth);
      resHeight = EditorGUILayout.IntField("Height", resHeight);

      EditorGUILayout.LabelField("Path");
      screenshotLocation = EditorGUILayout.TextArea(screenshotLocation);

      EditorGUILayout.BeginHorizontal();

      EditorGUILayout.Space();

      if (GUILayout.Button("Screenshot"))
      {
        TakeScreenshot();
      }

      EditorGUILayout.EndHorizontal();();
    }
    EditorGUILayout.EndVertical();
  }


  private void TakeScreenshot()
  {
    string filename = ScreenShotName("unity");
    ScreenCapture.CaptureScreenshot(filename, 1);
    Debug.Log($"Screenshot saved to {filename}");
  }

  private string ScreenShotName(string source)
  {
    return $"{screenshotLocation}/screen_{source}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
  }
}

#endif
