// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor.Build.Reporting;

public class BuilderEditor : EditorWindow
{
  private class TargetConfig
  {
    public BuildTarget Target;
    public string Path;

    public TargetConfig(BuildTarget target, string path)
    {
      Target = target;
      Path = path;
    }
  }

  #region UI

  private const string TITLE = "Build";
  private const int SIZE_X = 320;
  private const int SIZE_Y = 380;

  private Vector2 _scrollPosition;

  private Color defaultBGColor;
  private Color defaultColor;

  private int _versionMajor, _versionMinor, _versionPatch;
  private string _commitHash;

  private List<TargetConfig> _targetConfig;

  [MenuItem("Tools/" + TITLE, false, -5000)]
  private static void ShowWindow()
  {
    GetWindow(typeof(BuilderEditor), true, TITLE);
  }

  void OnEnable()
  {
    _versionMajor = BuildVersion.Instance.data.major;
    _versionMinor = BuildVersion.Instance.data.minor;
    _versionPatch = BuildVersion.Instance.data.patch + 1;

    minSize = new Vector2(SIZE_X, SIZE_Y);
    maxSize = new Vector2(SIZE_X, 2 * SIZE_Y);

    int targetCount = EditorPrefs.GetInt("Build.Target.Count", 0);

    _targetConfig = new List<TargetConfig>();
    for (int i = 0; i < targetCount; ++i)
    {
      _targetConfig.Add(new TargetConfig((BuildTarget) EditorPrefs.GetInt("Build.Target" + i), EditorPrefs.GetString("Build.Path" + i)));
    }

    RefreshCommit();
  }

  void OnGUI()
  {
    defaultBGColor = GUI.backgroundColor;
    defaultColor = GUI.contentColor;

    titleContent = new GUIContent(TITLE);
    _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

    EditorGUILayout.BeginHorizontal();
    {
      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Build", EditorStyles.boldLabel, GUILayout.Width(120));
      EditorGUILayout.Space();
    }
    EditorGUILayout.EndHorizontal();

    // Versions
    EditorGUILayout.BeginVertical("Box");
    {
      EditorGUILayout.BeginHorizontal();
      {
        // New
        EditorGUILayout.BeginVertical();
        {
          EditorGUILayout.LabelField("New version", EditorStyles.boldLabel, GUILayout.Width(120));
          if (BuildVersion.Instance != null)
          {
            EditorGUILayout.BeginHorizontal();
            {
              DrawVersionField(ref _versionMajor, BuildVersion.Instance.data.major);
              EditorGUILayout.LabelField(".", GUILayout.Width(7));
              DrawVersionField(ref _versionMinor, BuildVersion.Instance.data.minor);
              EditorGUILayout.LabelField(".", GUILayout.Width(7));
              DrawVersionField(ref _versionPatch, BuildVersion.Instance.data.patch);
            }
            EditorGUILayout.EndHorizontal();
          }

          EditorGUILayout.BeginHorizontal();
          {
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
              _versionMajor++;
              _versionMinor = 0;
              _versionPatch = 0;
            }

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
              _versionMajor--;
            }

            GUILayout.Space(7);
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
              _versionMinor++;
              _versionPatch = 0;
            }

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
              _versionMinor--;
            }

            GUILayout.Space(7);
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
              _versionPatch++;
            }

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
              _versionPatch--;
            }
          }
          EditorGUILayout.EndHorizontal();

          EditorGUILayout.BeginHorizontal();
          {
            if (string.IsNullOrEmpty(_commitHash) == false)
            {
              GUI.contentColor = Color.yellow;
            }

            EditorGUILayout.LabelField(_commitHash, GUILayout.Width(70));

            GUI.contentColor = defaultColor;

            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
              RefreshCommit();
            }
          }
          EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        // Latest
        EditorGUILayout.BeginVertical();
        {
          EditorGUILayout.LabelField("Last version", EditorStyles.boldLabel, GUILayout.Width(120));
          EditorGUILayout.LabelField(BuildVersion.Version, GUILayout.Width(70));

          GUI.contentColor = Color.yellow;
          EditorGUILayout.LabelField(BuildVersion.Commit, GUILayout.Width(75));
          GUI.contentColor = defaultColor;
        }
        EditorGUILayout.EndVertical();
      }
      EditorGUILayout.EndHorizontal();
    }
    EditorGUILayout.EndVertical(); // Box

    // Settings
    EditorGUILayout.BeginVertical("Box");
    {
      EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel, GUILayout.Width(120));

      // Example of options
      // _steam = EditorGUILayout.ToggleLeft(STEAM_SYMBOL, _steam);
    }
    EditorGUILayout.EndHorizontal();

    // Scenes
    EditorGUILayout.BeginVertical("Box", GUILayout.ExpandWidth(true));
    {
      EditorGUILayout.LabelField("Scenes", EditorStyles.boldLabel, GUILayout.Width(120));

      foreach (var s in Builder.GetScenes())
      {
        EditorGUILayout.LabelField(s.Replace("Assets/Scenes", "..."), GUILayout.Width(250));
      }
    }
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.Space();

    EditorGUILayout.BeginVertical("Box");
    {
      EditorGUILayout.BeginHorizontal();
      {
        EditorGUILayout.LabelField("Targets", EditorStyles.boldLabel, GUILayout.Width(70));
        EditorGUILayout.Space();
        if (GUILayout.Button("Add"))
        {
          if (_targetConfig == null)
            _targetConfig = new List<TargetConfig>();
          _targetConfig.Add(new TargetConfig(BuildTarget.StandaloneWindows, ""));
        }
      }
      EditorGUILayout.EndHorizontal();

      if (_targetConfig != null)
      {
        // Target config
        for (int i = 0; i < _targetConfig.Count; ++i)
        {
          EditorGUILayout.Space();
          EditorGUILayout.BeginVertical("Box");
          {
            EditorGUILayout.BeginHorizontal();
            {
              _targetConfig[i].Target = (BuildTarget) EditorGUILayout.EnumPopup("Target " + (i + 1), _targetConfig[i].Target);
              if (GUILayout.Button("x", GUILayout.Width(18)))
              {
                _targetConfig.RemoveAt(i);
                break;
              }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Path", EditorStyles.boldLabel, GUILayout.Width(120));

            EditorGUILayout.BeginHorizontal();
            {
              if (string.IsNullOrEmpty(_targetConfig[i].Path)) GUI.backgroundColor = Color.red;
              /* _path = */
              EditorGUILayout.TextField(_targetConfig[i].Path); // Read-only text for make sure we only have folders
              GUI.backgroundColor = defaultBGColor;

              if (GUILayout.Button("...", GUILayout.Width(25)))
              {
                _targetConfig[i].Path = EditorUtility.OpenFolderPanel("Build path", "", Builder.EXECUTABLE_NAME);
              }
            }
            EditorGUILayout.EndHorizontal();
          }
          EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
      }

      EditorGUILayout.Space();
    }
    EditorGUILayout.EndVertical();

    EditorGUILayout.Space();

    EditorGUILayout.BeginHorizontal();
    {
      EditorGUILayout.Space();
      if (GUILayout.Button("BUILD", GUILayout.Width(200), GUILayout.Height(40)))
      {
        SavePrefs();
        for (int i = 0; i < _targetConfig.Count; ++i)
        {
          if (string.IsNullOrEmpty(_targetConfig[i].Path) == false)
          {
            SaveVersion();
            Builder.Build(_targetConfig[i].Target, _targetConfig[i].Path);
          }
        }
      }

      EditorGUILayout.Space();
    }
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Remember to commit BuildVersion.asset after build!", EditorStyles.miniLabel, GUILayout.Width(270));

    GUILayout.Space(10);

    GUILayout.EndScrollView();
  }

  private void DrawVersionField(ref int newVersion, int lastVersion)
  {
    if (newVersion != lastVersion) GUI.contentColor = Color.green;

    newVersion = EditorGUILayout.IntField(newVersion, GUILayout.Width(40));

    GUI.contentColor = defaultColor;
  }

  private void RefreshCommit()
  {
#if UNITY_STANDALONE_WIN
        const string git_magic = "git rev-parse HEAD";

        System.Diagnostics.Process proc = new System.Diagnostics.Process();
        proc.StartInfo.FileName = "cmd.exe";
        proc.StartInfo.Arguments = "/c " + git_magic;
        proc.StartInfo.CreateNoWindow = true;
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.RedirectStandardError = true;
        proc.Start();

        _commit_hash = proc.StandardOutput.ReadToEnd();
        if (String.IsNullOrEmpty(_commit_hash))
            Debug.LogError(proc.StandardError.ReadToEnd());
#endif
  }

  private void SavePrefs()
  {
    EditorPrefs.SetInt("Build.Target.Count", _targetConfig.Count);
    for (int i = 0; i < 100; ++i)
    {
      if (i < _targetConfig.Count)
      {
        EditorPrefs.SetString("Build.Path" + i, _targetConfig[i].Path);
        EditorPrefs.SetInt("Build.Target" + i, (int) _targetConfig[i].Target);
      }
      else
      {
        EditorPrefs.DeleteKey("Build.Path" + i);
        EditorPrefs.DeleteKey("Build.Target" + i);
      }
    }
  }

  private void SaveVersion()
  {
    if (BuildVersion.Instance != null)
    {
      BuildVersion.Instance.data.major = _versionMajor;
      BuildVersion.Instance.data.minor = _versionMinor;
      BuildVersion.Instance.data.patch = _versionPatch;
      BuildVersion.Instance.data.commitHash = _commitHash;

      EditorUtility.SetDirty(BuildVersion.Instance.data);
      AssetDatabase.SaveAssets();
    }
  }

  #endregion
}

public static class Builder
{
  public const string EXECUTABLE_NAME = "MyGame";

  #region Command line

  public static void PerformCommandLineBuild()
  {
    // -------------------------------------------------------
    // Arguments parsing.
    // -------------------------------------------------------

    // Get arguments.
    var arguments = Environment.GetCommandLineArgs();

    var output = "";
    var target = BuildTarget.StandaloneWindows;

    // Parse.
    foreach (var arg in arguments)
    {
      // Find the output path.
      if (arg.Contains("-output="))
      {
        output = arg.Split('=')[1];

        // TODO Make sure it's a folder
      }

      // Find the platform.
      if (arg.Contains("-platform="))
      {
        target = GetTarget(arg.Split('=')[1]);
      }
    }

    // -------------------------------------------------------
    // Build.
    // -------------------------------------------------------

    if (!string.IsNullOrEmpty(output))
    {
      Build(target, output + EXECUTABLE_NAME + GetExtension(target));
    }
  }

  #endregion

  public static BuildResult Build(BuildTarget target, string path, params string[] symbols)
  {
    if (string.IsNullOrEmpty(path))
    {
      return BuildResult.Unknown;
    }

    PlayerSettings.SplashScreen.showUnityLogo = false;

    string finalPath = System.IO.Path.Combine(path, EXECUTABLE_NAME + GetExtension(target));

    BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);

    // -------------------------------------------------------
    // Symbols.
    // -------------------------------------------------------

    var baseSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

    // Add new symbols
    var compilationFlags = string.Empty;
    if (compilationFlags.EndsWith(";")) compilationFlags = compilationFlags.Remove(compilationFlags.Length - 1, 1);

    foreach (var s in symbols)
    {
      if (string.IsNullOrEmpty(s) == false)
      {
        compilationFlags += ";" + s;
      }
    }

    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, compilationFlags);

    // -------------------------------------------------------
    // Build.
    // -------------------------------------------------------

    Debug.Log("BUILDING game for " + target + " at " + finalPath);
    Debug.Log(compilationFlags);

    BuildPlayerOptions opts = new BuildPlayerOptions();
    opts.target = target;
    opts.targetGroup = group;
    opts.options = BuildOptions.ShowBuiltPlayer;
    opts.scenes = GetScenes();
    opts.locationPathName = finalPath;

    var result = BuildPipeline.BuildPlayer(opts);
    if (result.summary.result != BuildResult.Succeeded)
    {
      Debug.LogError("BUILD fail: " + result.summary.result);
    }
    else
    {
      // foreach (string symbol in symbols)
      //     if (symbol == STEAM_SYMBOL)
      //         System.IO.File.Copy("./steam_appid.txt", System.IO.Path.Combine(path, "steam_appid.txt"), true);
      Debug.Log("BUILD Success!");
    }

    // -------------------------------------------------------
    // Clean.
    // -------------------------------------------------------

    // Set back symbols
    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, baseSymbols);

    return result.summary.result;
  }

  #region Tools

  public static string[] GetScenes()
  {
    var scenes = new List<string>();

    foreach (var scene in EditorBuildSettings.scenes)
    {
      if (scene.enabled)
      {
        scenes.Add(scene.path);
      }
    }

    return scenes.ToArray();
  }

  private static BuildTarget GetTarget(string platform)
  {
    switch (platform == null ? string.Empty : platform)
    {
      case "osx": return BuildTarget.StandaloneOSX;
      case "linux": return BuildTarget.StandaloneLinux64;
      case "win": return BuildTarget.StandaloneWindows;
      case "win64": return BuildTarget.StandaloneWindows64;

      // Default is windows.
      default: return BuildTarget.StandaloneWindows;
    }
  }

  private static string GetExtension(BuildTarget target)
  {
    switch (target)
    {
      case BuildTarget.StandaloneWindows: return ".exe";
      case BuildTarget.StandaloneWindows64: return ".exe";
      case BuildTarget.StandaloneOSX: return ".app";
      case BuildTarget.StandaloneLinux64: return ".x86_64";
      case BuildTarget.StandaloneLinux: return ".x86";

      // Default is nothing.
      default: return "";
    }
  }

  #endregion
}