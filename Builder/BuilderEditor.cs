using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Builder
{
    public class BuilderEditor : EditorWindow
    {
        #region Builder Editor

        [MenuItem("Builds/💻 Builder", priority = 50)]
        private static void ShowWindow()
        {
            var window = GetWindow<BuilderEditor>();
            window.titleContent = new GUIContent("Builder");
            window.Show();
        }

        private Vector2 scroll;
        private Color bgColor, color;
        private Dictionary<BuilderSettings, bool> foldSettings = new Dictionary<BuilderSettings, bool>();

        private List<BuilderSequenceSettings> allSequences;
        private BuilderSequenceSettings selectedSequence;
        private ReorderableList settingsList;

        private void OnEnable()
        {
            Refresh();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            bgColor = GUI.backgroundColor;
            color = GUI.color;

            EditorGUILayout.BeginVertical("Box", GUILayout.ExpandHeight(true));
            {
                DrawSequence();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUI.BeginDisabledGroup(selectedSequence == null);
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.Space();

                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("BUILD\n" + selectedSequence.name.Replace("BuilderSequence_", ""),
                            GUILayout.Width(250), GUILayout.Height(50)))
                    {
                        BuildSequence(selectedSequence);
                    }

                    EditorGUILayout.Space();

                    GUI.backgroundColor = bgColor;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndVertical();


            EditorGUILayout.EndScrollView();
        }

        private void DrawSequence()
        {
            EditorGUILayout.BeginHorizontal();
            {
                int selectedIndex = allSequences.IndexOf(selectedSequence);
                var i = EditorGUILayout.Popup("Build Sequence", selectedIndex,
                    allSequences.Select(s => s.name).ToArray());
                if (i != selectedIndex)
                {
                    selectedSequence = allSequences[i];
                    settingsList = null;
                }

                if (GUILayout.Button(EditorGUIUtility.FindTexture("TreeEditor.Refresh"), GUILayout.Width(30)))
                {
                    Refresh();
                }

                if (GUILayout.Button(EditorGUIUtility.FindTexture("ScriptableObject Icon"),
                        GUILayout.Width(30)))
                {
                    if (selectedSequence != null) Selection.activeObject = selectedSequence;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            if (selectedSequence != null)
            {
                EditorGUILayout.BeginVertical("Box");
                {
                    EditorGUILayout.LabelField("Build order", EditorStyles.boldLabel);

                    // Draw settings
                    if (settingsList == null)
                    {
                        settingsList = new ReorderableList(selectedSequence.sequence, typeof(BuilderSettings), true,
                            false,
                            true, true);
                        settingsList.onAddCallback = list => { selectedSequence.sequence.Add(null); };
                        settingsList.onRemoveCallback = list =>
                        {
                            if (selectedSequence.sequence.Count > settingsList.index && settingsList.index >= 0)
                            {
                                var item = selectedSequence.sequence[settingsList.index];
                                selectedSequence.sequence.Remove(item);
                            }
                        };
                        settingsList.drawElementCallback = (rect, index, active, focused) =>
                        {
                            BuilderSettings item = null;
                            if (selectedSequence.sequence.Count > index && index >= 0)
                            {
                                item = selectedSequence.sequence[index];
                            }

                            const int LABEL_WIDTH = 125;
                            EditorGUI.LabelField(new Rect(rect.x, rect.y, LABEL_WIDTH, rect.height),
                                item != null ? item.target.ToString() : string.Empty);
                            BuilderSettings newItem =
                                (BuilderSettings)EditorGUI.ObjectField(
                                    new Rect(rect.x + LABEL_WIDTH, rect.y, rect.width - LABEL_WIDTH, rect.height),
                                    item, typeof(BuilderSettings), false);
                            if (item != newItem)
                            {
                                selectedSequence.sequence[index] = newItem;
                            }
                        };
                    }

                    settingsList.DoLayoutList();

                    // Display all sequence with foldable
                    foreach (var s in selectedSequence.sequence)
                    {
                        if (s == null) continue;
                        bool display = false;
                        if (foldSettings.ContainsKey(s)) display = foldSettings[s];

                        display = EditorGUILayout.Foldout(display, s.name);
                        foldSettings[s] = display;

                        if (display)
                        {
                            EditorGUILayout.BeginVertical("Box");
                            {
                                DrawSettings(s);
                            }
                            EditorGUILayout.EndVertical();
                        }
                    }
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("Box");
                {
                    EditorGUILayout.LabelField("Sequence settings", EditorStyles.boldLabel);
                    selectedSequence.version =
                        EditorGUILayout.TextField("Game version", selectedSequence.version);

                    selectedSequence.devBuild = EditorGUILayout.Toggle("Development build", selectedSequence.devBuild);
                    GUI.color = selectedSequence.devBuild ? Color.green : Color.magenta;
                    EditorGUILayout.LabelField(selectedSequence.devBuild
                        ? "DEBUG - includes Bug report & console"
                        : "RELEASE /!\\ - removes bug reporter & console");

                    GUI.color = Color.Lerp(Color.yellow, Color.white, 0.5f);
                    
                    GUI.color = color;
                    
                    selectedSequence.openURL =
                        EditorGUILayout.TextField("Post build URL", selectedSequence.openURL);

                    selectedSequence.flags =
                        EditorGUILayout.TextField("Flags", selectedSequence.flags);
                }
                EditorGUILayout.EndVertical();

                selectedSequence.DrawCustomSettings();

                EditorGUILayout.Space();
            }
        }

        public void Refresh()
        {
            allSequences = BuilderUtils.FindAllAssets<BuilderSequenceSettings>().ToList();
            if (selectedSequence == null) selectedSequence = allSequences.FirstOrDefault();
        }

        private async void DrawSettings(BuilderSettings s)
        {
            if (s != null)
            {
                s.target =
                    (BuildTarget)EditorGUILayout.EnumPopup("Target", s.target);
                s.buildOptions =
                    (BuildOptions)EditorGUILayout.EnumFlagsField("Build options",
                        s.buildOptions);
                s.buildAB =
                    EditorGUILayout.Toggle("Build AssetBundles", s.buildAB);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Destination", EditorStyles.miniBoldLabel);
                s.outputFolder =
                    BuilderUtils.BrowseField("Output path", s.outputFolder, null);
                s.executableName =
                    EditorGUILayout.TextField("Executable name", s.executableName);

                EditorGUILayout.LabelField(s.GetExecutablePath(), EditorStyles.miniLabel);

                EditorGUILayout.Space();

                s.DrawCustomSettings();

                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.Space();

                    GUI.backgroundColor = Color.gray;
                    if (GUILayout.Button("Build " + s.target, GUILayout.Width(250),
                            GUILayout.Height(25)))
                    {
                        await Build(selectedSequence, s);
                    }

                    GUI.backgroundColor = bgColor;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("Select or create a build configuration to continue.");
            }
        }

        #endregion

        #region Builds

        public static async void BuildSequence(BuilderSequenceSettings sequence)
        {
            var currentTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var currentTarget = EditorUserBuildSettings.selectedStandaloneTarget;

            // Instantiate!
            var builderSequence = Instantiate(sequence);

            Debug.Log("🚀 Starting build sequence " + (builderSequence.sequence.Count) + " builds.");

            Debug.Log("Pre builds...");
            await builderSequence.OnPreSequence();
            bool abort = false;
            foreach (var settings in builderSequence.sequence)
            {
                if (settings == null)
                {
                    Debug.LogError("Null BuilderSettings found in sequence. Skipping.");
                    continue;
                }

                bool success = await Build(builderSequence, settings);

                if (success == false && builderSequence.continueIfError == false)
                {
                    Debug.LogError("Stopping build sequence.");
                    EditorUtility.DisplayDialog("Build failed.", "Sorry, build failed. Check the console for details.",
                        "OK");
                    abort = true;
                    break;
                }
            }

            if (abort == false)
            {
                Debug.Log("Post builds...");
                await builderSequence.OnPostSequence();


                if (string.IsNullOrEmpty(sequence.openURL) == false)
                {
                    Application.OpenURL(sequence.openURL);
                }

                Debug.Log("✨ Build sequence completed");
            }

            EditorUserBuildSettings.selectedBuildTargetGroup = currentTargetGroup;
            EditorUserBuildSettings.selectedStandaloneTarget = currentTarget;
        }

        public static async Task<bool> Build(BuilderSequenceSettings sequence, BuilderSettings builderSettings)
        {
            if (builderSettings == null) return false;

            Stopwatch watch = new Stopwatch();
            watch.Start();
            Debug.Log("BUILD STARTED " + builderSettings.target);

            if (builderSettings.buildAB)
            {
                Debug.Log("-> Cleaning Streaming Assets");
                CleanStreamingAssets();

                Debug.Log("-> Building AB ");
                BuildAssetBundles.Build(builderSettings.target);
            }

            await builderSettings.OnPreBuild();

            var path = builderSettings.GetExecutablePath();
            Debug.Log("-> Building executable. devmode= " + sequence.devBuild + " path=" + path + " flags=" + sequence.flags);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.locationPathName = path;
            buildPlayerOptions.target = builderSettings.target;
            buildPlayerOptions.options = builderSettings.buildOptions;
            if (sequence.devBuild)
            {
                buildPlayerOptions.options |= BuildOptions.Development;
            }
            
            List<string> flags = new(sequence.flags.Split(";").Where(s => string.IsNullOrWhiteSpace(s) == false));
            buildPlayerOptions.extraScriptingDefines = flags.ToArray();

            buildPlayerOptions.scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            PlayerSettings.bundleVersion = sequence.version;

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            watch.Stop();
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("BUILD succeeded! " + watch.Elapsed.TotalSeconds.ToString("N2") + "s " +
                         (summary.totalSize / (1024 * 1024) / 8) + " Mo");

                DeleteBurstDebugInformationFolder(report);

                return true;
            }

            if (summary.result == BuildResult.Failed)
            {
                Debug.LogError("BUILD " + summary.result + "... :(");
            }

            return false;
        }

        private static void DeleteBurstDebugInformationFolder(BuildReport buildReport)
        {
            if (buildReport == null) return;

            string outputPath = buildReport.summary.outputPath;

            try
            {
                string applicationName = Path.GetFileNameWithoutExtension(outputPath);
                string outputFolder = Path.GetDirectoryName(outputPath);
                if (string.IsNullOrEmpty(outputFolder)) return;

                outputFolder = Path.GetFullPath(outputFolder);

                string burstDebugInformationDirectoryPath = Path.Combine(outputFolder, $"{applicationName}_BurstDebugInformation_DoNotShip");

                if (Directory.Exists(burstDebugInformationDirectoryPath))
                {
                    Debug.Log($" > Deleting Burst debug information folder at path '{burstDebugInformationDirectoryPath}'...");

                    Directory.Delete(burstDebugInformationDirectoryPath, true);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"An unexpected exception occurred while performing build cleanup: {e}");
            }
        }


#if ENABLE_AB
        [MenuItem("Builds/Clean StreamingAssets", priority = 50)]
#endif
        public static void CleanStreamingAssets()
        {
            string path = Application.streamingAssetsPath;
            DirectoryInfo di = new DirectoryInfo(path);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        #endregion


    }
}