using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace Builder
{
    [CreateAssetMenu(fileName = "SteamSequenceSettings", menuName = "Flat Eye/Builder/Steam Sequence", order = 0)]
    public class SteamSequenceSettings : BuilderSequenceSettings
    {
        public long appID = 1358840;
        public string vdfFolder;
        public string branch;
        public SteamDepotSettings[] depots;

#if UNITY_EDITOR
        public string SteamSDK
        {
            get => EditorPrefs.GetString("FlatEye.Steam.SDK");
            set => EditorPrefs.SetString("FlatEye.Steam.SDK", value);
        }

        public string SteamUser
        {
            get => EditorPrefs.GetString("FlatEye.Steam.User");
            set => EditorPrefs.SetString("FlatEye.Steam.User", value);
        }

        public string SteamPassword
        {
            get => EditorPrefs.GetString("FlatEye.Steam.Password");
            set => EditorPrefs.SetString("FlatEye.Steam.Password", value);
        }

        public string SteamBranch
        {
            // get => EditorPrefs.GetString("FlatEye.Steam.Branch");
            // set => EditorPrefs.SetString("FlatEye.Steam.Branch", value);
            get => branch;
            set => branch = value;
        }
#endif

        public override void DrawCustomSettings()
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUILayout.LabelField("Steam", EditorStyles.boldLabel);

                var newID = EditorGUILayout.LongField("App ID", appID);
                if (newID != appID)
                {
                    appID = newID;
                    depots = null;
                }

                string steamSdkPath = SteamSDK;
                string newSdkPath = BuilderUtils.BrowseField("ContentBuilder path", steamSdkPath, null);
                if (newSdkPath != steamSdkPath)
                {
                    SteamSDK = newSdkPath;
                }

                EditorGUILayout.BeginHorizontal();
                {
                    string steamUser = SteamUser;
                    string newLogin = EditorGUILayout.TextField("Steam login", steamUser);
                    if (newLogin != steamUser)
                    {
                        SteamUser = newLogin;
                    }

                    string steamPassword = SteamPassword;
                    string newPassword = EditorGUILayout.PasswordField(steamPassword);
                    if (newPassword != steamPassword)
                    {
                        SteamPassword = newPassword;
                    }
                }
                EditorGUILayout.EndHorizontal();

                string branch = SteamBranch;
                string newBranch = EditorGUILayout.TextField("Set on branch", branch);
                if (newBranch != branch)
                {
                    SteamBranch = newBranch;
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Depots", EditorStyles.miniBoldLabel);
                if (depots == null || depots.Length != sequence.Count)
                {
                    depots = new SteamDepotSettings[sequence.Count];
                    for (int i = 0; i < sequence.Count; i++)
                    {
                        depots[i].ID = appID + i + 1;
                    }
                }

                for (int i = 0; i < sequence.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        sequence[i] =
                            (BuilderSettings) EditorGUILayout.ObjectField(sequence[i], typeof(BuilderSettings),
                                false);
                        depots[i].ID
                            = EditorGUILayout.LongField("Depot ID", depots[i].ID);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                vdfFolder = BuilderUtils.BrowseField("VDF folder", vdfFolder, null);

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.Space();

                    var bgColor = GUI.backgroundColor;

                    GUI.backgroundColor = Color.gray;
                    if (GUILayout.Button("Generate VDFs", GUILayout.Width(125),
                        GUILayout.Height(25)))
                    {
                        try
                        {
                            string path = GenerateVDFs();
                            Debug.Log(path);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }

                    GUI.backgroundColor = Color.gray;
                    if (GUILayout.Button("Upload", GUILayout.Width(125),
                        GUILayout.Height(25)))
                    {
                        try
                        {
                            OnPostSequence();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }

                    GUI.backgroundColor = bgColor;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(this);
            }
        }

        public override Task OnPostSequence()
        {
            string appVdf = GenerateVDFs();

            // Upload with steamcmd
            string cmd = Path.Combine(SteamSDK, "builder", "steamcmd.exe");
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            startInfo.FileName = cmd;
            // startInfo.Arguments = $"+login {SteamUser} \"{SteamPassword}\" +run_app_build \"{appVdf}\""; // DEBUG
            startInfo.Arguments = $"+login {SteamUser} \"{SteamPassword}\" +run_app_build \"{appVdf}\" +quit";
            process.StartInfo = startInfo;

            Debug.Log($"Starting process {process.StartInfo.FileName} {process.StartInfo.Arguments}");

            process.Start();

            return base.OnPostSequence();
        }

        public string GenerateVDFs()
        {
            // One for each depot
            List<string> depotsVdfPaths = new List<string>();
            for (int i = 0; i < depots.Length; i++)
            {
                var depot = depots[i];
                var settings = sequence[i];
                var path = GenerateDepotVDF(depot, settings);
                depotsVdfPaths.Add(path);
            }

            // One for the app
            var pathFinal = GenerateAppVDF(depotsVdfPaths);
            return pathFinal;
        }

        private string GetVDFPath(string filename)
        {
            filename += Path.GetExtension(filename) != ".vdf" ? ".vdf" : "";
            string basePath = Path.IsPathRooted(vdfFolder) ? vdfFolder : Path.Combine(Application.dataPath, vdfFolder);
            return Path.GetFullPath(Path.Combine(basePath, filename));
        }

        private string GenerateDepotVDF(SteamDepotSettings depot, BuilderSettings settings)
        {
            // depot_ID.vdf
            // "DepotBuildConfig"
            // {
            //     "DepotID" "1358842"
            //     "contentroot" "F:\projects\flat-eye\builds\osx"
            //     "FileMapping"
            //     {
            //         "LocalPath" "*"
            //         "DepotPath" "."
            //         "recursive" "1"
            //     }
            //     "FileExclusion" "*.pdb"
            // }
            string path = GetVDFPath("depot_" + depot.ID);

            if (File.Exists(path)) File.Delete(path);

            string content = "\"DepotBuildConfig\"\n" +
                             "{\n" +
                             "\t\"DepotID\" \"" + depot.ID + "\"\n" +
                             "\t\"contentroot\" \"" + settings.GetExecutableFolder() + "\"\n" +
                             "\t\"FileMapping\"\n" +
                             "\t{\n" +
                             "\t\t\"LocalPath\" \"*\"\n" +
                             "\t\t\"DepotPath\" \".\"\n" +
                             "\t\t\"recursive\" \"1\"\n" +
                             "\t}\n" +
                             "\t\"FileExclusion\" \"*.pdb\"\n" +
                             "}";

            File.WriteAllText(path, content);

            return path;
        }

        private string GenerateAppVDF(List<string> depotsPath)
        {
            //app_ID.cdf
            // "appbuild"
            // {
            //     "appid" "1358840"
            //     "desc" ""
            //     "buildoutput" "F:\dev\Steamworks\tools\ContentBuilder\output"
            //     "contentroot" ""
            //     "setlive" ""
            //     "preview" "1"
            //     "local"	""
            //     "depots"
            //     {
            //         "1358841"	"F:\dev\Steamworks\tools\ContentBuilder\scripts\depot_1358841.vdf"
            //         "1358842"	"F:\dev\Steamworks\tools\ContentBuilder\scripts\depot_1358842.vdf"
            //     }
            // }
            string path = GetVDFPath("app_" + appID);
            if (File.Exists(path)) File.Delete(path);

            string content = "\"appbuild\"\n" +
                             "{\n" +
                             "\t\"appid\" \"" + appID + "\"\n" +
                             "\t\"desc\" \"\"\n" +
                             "\t\"buildoutput\" \"" + Path.GetFullPath(Path.Combine(SteamSDK, "output")) + "\"\n" +
                             "\t\"contentroot\" \"\"\n" +
                             "\t\"setlive\" \"" + SteamBranch + "\"\n" +
                             "\t\"preview\" \"0\"\n" +
                             "\t\"local\" \"\"\n" +
                             "\t\"depots\"\n" +
                             "\t{\n";

            for (int i = 0; i < depots.Length; i++)
            {
                var depot = depots[i];
                string depotPath = depotsPath[i];
                content += "\t\t\"" + depot.ID + "\"\t\"" + depotPath + "\"\n";
            }

            content += "\t}\n";
            content += "}";

            File.WriteAllText(path, content);

            return path;
        }
    }

    [Serializable]
    public struct SteamDepotSettings
    {
        public long ID;
    }
}