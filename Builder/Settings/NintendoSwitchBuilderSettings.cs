using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Builder
{
    [CreateAssetMenu(fileName = "NintendoSwitchBuilderSettings", menuName = "Flat Eye/Builder/Nintendo Switch Settings",
        order = 10000)]
    public class NintendoSwitchBuilderSettings : BuilderSettings
    {
        public int releaseVersion;

        public override Task OnPreBuild()
        {
            PlayerSettings.iOS.buildNumber = releaseVersion.ToString();
            return Task.CompletedTask;
        }

        public override void DrawCustomSettings()
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUILayout.LabelField("Nintendo Switch", EditorStyles.boldLabel);

                releaseVersion = EditorGUILayout.IntField("Release version", releaseVersion);
            }
            EditorGUILayout.EndVertical();
        }
    }
}