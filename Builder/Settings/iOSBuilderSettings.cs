using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Builder
{
    [CreateAssetMenu(fileName = "iOSBuilderSettings", menuName = "Flat Eye/Builder/iOS Settings",
        order = 10000)]
    public class IOSBuilderSettings : BuilderSettings
    {
        public int buildNumber;

        public override Task OnPreBuild()
        {
            PlayerSettings.iOS.buildNumber = buildNumber.ToString();
            return Task.CompletedTask;
        }

        public override void DrawCustomSettings()
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUILayout.LabelField("iOS", EditorStyles.boldLabel);

                buildNumber = EditorGUILayout.IntField("Build number", buildNumber);
            }
            EditorGUILayout.EndVertical();
        }
    }
}