using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Builder
{
    [CreateAssetMenu(fileName = "BuilderSettings", menuName = "Flat Eye/Builder/Settings", order = 10000)]
    public class BuilderSettings : ScriptableObject
    {
        public BuildTarget target;
        public BuildOptions buildOptions;
        public string outputFolder;
        public string executableName;
        public bool buildAB = true;

        public virtual void DrawCustomSettings() { }

        public string GetExecutablePath()
        {
            if (string.IsNullOrEmpty(outputFolder) || string.IsNullOrEmpty(executableName)) return string.Empty;
            if (string.IsNullOrEmpty(outputFolder) == false && Path.IsPathRooted(outputFolder) == false)
            {
                return Path.GetFullPath(Path.Combine(Application.dataPath,
                    outputFolder, executableName));
            }

            return Path.Combine(outputFolder, executableName);
        }

        public string GetExecutableFolder()
        {
            return Path.GetDirectoryName(GetExecutablePath());
        }

        public virtual Task OnPreBuild()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnPostBuild()
        {
            return Task.CompletedTask;
        }
    }
}