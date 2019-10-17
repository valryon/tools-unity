// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Get access to some properties and controls of the AnimationWindow.
/// </summary>
/// <remarks>It may breaks over time...</remarks>
public static class AnimationWindowHelper
{
  private static EditorWindow _window;

  private static BindingFlags _flags;
  private static FieldInfo _animEditor;

  private static Type _animEditorType;
  private static System.Object _animEditorObject;
  private static FieldInfo _animWindowState;

  /// <summary>
  /// Force open the Animation window
  /// </summary>
  public static void OpenWindow()
  {
    EditorApplication.ExecuteMenuItem("Window/Animation/Animation");

    GetOpenAnimationWindow();
  }

  #region Internal

  private static Type _animationWindowType;

  private static Type GetAnimationWindowType()
  {
    if (_animationWindowType == null)
    {
      _animationWindowType = Type.GetType("UnityEditor.AnimationWindow,UnityEditor");
    }

    return _animationWindowType;
  }

  public static EditorWindow GetOpenAnimationWindow()
  {
    if (_window == null)
    {
      var openAnimationWindows = Resources.FindObjectsOfTypeAll(GetAnimationWindowType());
      if (openAnimationWindows.Length > 0)
      {
        _window = openAnimationWindows[0] as EditorWindow;

        // Store all reflection info so we don't request them every time
        _flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        _animEditor = GetAnimationWindowType().GetField("m_AnimEditor", _flags);

        if (_animEditor != null)
        {
          _animEditorType = _animEditor.FieldType;
          _animEditorObject = _animEditor.GetValue(_window);
        }

        _animWindowState = _animEditorType.GetField("m_State", _flags);
      }
    }

    return _window;
  }

  #endregion

  #region Public methods

  public static void StartRecord()
  {
    InvokeMethod("set_recording", true);
    Repaint();
  }

  public static void StopRecord()
  {
    InvokeMethod("set_recording", false);
    Repaint();
  }

  public static bool IsRecording()
  {
    return AnimationMode.InAnimationMode();
  }

  public static bool GetPlaying()
  {
    return InvokeMethod<bool>("get_playing");
  }

  public static void Repaint()
  {
    InvokeMethod("Repaint");
  }

  public static void StartPlayback()
  {
    InvokeMethod("StartPlayback");
    Repaint();
  }

  public static void StopPlayback()
  {
    InvokeMethod("StopPlayback");
    Repaint();
  }

  // public Void set_currentFrame(Int32)
  public static void SetCurrentFrame(int frame)
  {
    InvokeMethod("set_currentFrame", frame);
    Repaint();
  }

  // public Int32 get_currentTime()
  public static int GetCurrentFrame()
  {
    return InvokeMethod<int>("get_currentFrame", null);
  }

  // public Single get_currentTime()
  public static float GetCurrentTime()
  {
    return InvokeMethod<float>("get_currentTime");
  }

  // public Single TimeToFrame(Single)
  public static float TimeToFrame(float time)
  {
    return InvokeMethod<float>("TimeToFrame", time);
  }

  // public Single FrameToTime(Single)
  public static float FrameToTime(float frame)
  {
    return InvokeMethod<float>("FrameToTime", frame);
  }

  // public UnityEngine.Vector2 get_timeRange()
  public static Vector2 GetTimeRange()
  {
    return InvokeMethod<Vector2>("get_timeRange");
  }


  private static object InvokeMethod(string methodName, params object[] methodParams)
  {
    if (_window != null)
    {
      // Get the State of the window. The object (with values) AND the type.
      var type = _animWindowState.FieldType;
      var target = _animWindowState.GetValue(_animEditorObject);

      // Now find the method in the state type
      var methodParamTypes = methodParams?.Select(p => p.GetType()).ToArray() ?? new Type[] { };
      var bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
      MethodInfo method = null;
      while (method == null && type != null)
      {
        method = type.GetMethod(methodName, bindingFlags, Type.DefaultBinder, methodParamTypes, null);
        type = type.BaseType;
      }

      // Invoke the method on the state object
      return method?.Invoke(target, methodParams);
    }

    return null;
  }

  private static T InvokeMethod<T>(string methodName, params object[] methodParams)
  {
    T ret = default(T);
    if (_window != null)
    {
      System.Object v = InvokeMethod(methodName, methodParams);
      if (v != null && v is T)
      {
        ret = (T) v;
      }
      else
      {
        Debug.LogError("Return value was null or from the wrong type: " + v);
      }
    }

    return ret;
  }

  #endregion

  #region Debug

  /// <summary>
  /// Debug to get new methods from Unity assembly
  /// </summary>
  public static void PrintMethods()
  {
    var w = GetOpenAnimationWindow();
    if (w != null)
    {
      var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
      var animEditor = GetAnimationWindowType().GetField("m_AnimEditor", flags);
      if (animEditor != null)
      {
        var animEditorType = animEditor.FieldType;
        var animWindowState = animEditorType.GetField("m_State", flags);
        if (animWindowState != null)
        {
          var windowStateType = animWindowState.FieldType;
          var methods = windowStateType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
          Debug.Log("Methods : " + methods.Length);
          for (int i = 0; i < methods.Length; i++)
          {
            var currentInfo = methods[i];
            var visibility = "";
            if (currentInfo.IsPublic) visibility = "public ";
            if (currentInfo.IsPrivate) visibility = "private ";
            var isStatic = currentInfo.IsStatic ? "static " : "";
            Debug.Log(visibility + isStatic + currentInfo);
          }
        }
      }
    }
  }

  #endregion
}