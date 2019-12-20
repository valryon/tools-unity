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
  private Camera targetCamera;

  private int resWidth = Screen.width;
  private int resHeight = Screen.height;

  void OnGUI()
  {
    titleContent = new GUIContent("Screenshoter");

    EditorGUILayout.LabelField("Screenshoter", EditorStyles.boldLabel);

    resWidth = EditorGUILayout.IntField("Width", resWidth);
    resHeight = EditorGUILayout.IntField("Height", resHeight);

    EditorGUILayout.LabelField("Path");
    screenshotLocation = EditorGUILayout.TextArea(screenshotLocation);


    if (targetCamera == null)
    {
      targetCamera = Camera.main;
    }

    targetCamera = (Camera) EditorGUILayout.ObjectField("Camera", targetCamera, typeof(Camera), true);


    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("Scene screenshot"))
    {
      TakeScreenshot();
    }

    if (GUILayout.Button("Camera screenshot"))
    {
      TakeRenderScreenshot();
    }

    EditorGUILayout.EndHorizontal();


    //--
    EditorGUILayout.Separator();
    //--

    EditorGUI.indentLevel--;
  }


  private void TakeScreenshot()
  {
    string filename = ScreenShotName("unity");
    ScreenCapture.CaptureScreenshot(filename, 1);
    Debug.Log($"Screenshot saved to {filename}");
  }

  private void TakeRenderScreenshot()
  {
    if (targetCamera == null) return;

    RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
    targetCamera.targetTexture = rt;
    Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
    targetCamera.Render();
    RenderTexture.active = rt;
    screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
    targetCamera.targetTexture = null;
    RenderTexture.active = null;

    if (Application.isPlaying) Destroy(rt);
    else DestroyImmediate(rt);

    byte[] bytes = screenShot.EncodeToPNG();
    string filename = ScreenShotName("camera");

    File.WriteAllBytes(filename, bytes);
    Debug.Log($"Screenshot saved to {filename}");
  }

  private string ScreenShotName(string source)
  {
    return $"{screenshotLocation}/screen_{source}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
  }
}

#endif