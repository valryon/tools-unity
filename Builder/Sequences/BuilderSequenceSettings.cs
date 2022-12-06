using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Builder
{
    [CreateAssetMenu(fileName = "BuilderSequenceSettings", menuName = "Flat Eye/Builder/Sequence", order = 0)]
    public class BuilderSequenceSettings : ScriptableObject
    {
        public List<BuilderSettings> sequence = new List<BuilderSettings>();
        public bool devBuild = true;
        public bool continueIfError = true;
        public string version;
        public string flags;
        public string openURL;

        public virtual void DrawCustomSettings()
        {
            foreach (var s in sequence)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(s, typeof(BuilderSettings), false);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        public virtual Task OnPreSequence()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnPostSequence()
        {
            return Task.CompletedTask;
        }
    }
}