using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Builder
{
    [CreateAssetMenu(fileName = "AndroidBuilderSettings", menuName = "Flat Eye/Builder/Android Settings",
        order = 10000)]
    public class AndroidBuilderSettings : BuilderSettings
    {
        public bool aab;
        public int buildVersionCode;

        public override void DrawCustomSettings()
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUILayout.LabelField("Android", EditorStyles.boldLabel);

                aab = EditorGUILayout.Toggle("AAB", aab);
                buildVersionCode = EditorGUILayout.IntField("Build version code", buildVersionCode);

                EditorGUILayout.LabelField("Signing", EditorStyles.boldLabel);
                KeyStoreLocation = EditorGUILayout.TextField("KeyStore name", KeyStoreLocation);
                KeyStorePassword = EditorGUILayout.PasswordField("KeyStore pass", KeyStorePassword);
                KeyAliasName = EditorGUILayout.TextField("KeyAlias name", KeyAliasName);
                KeyAliasPassword = EditorGUILayout.PasswordField("KeyAlias pass", KeyAliasPassword);
            }
            EditorGUILayout.EndVertical();
        }

        public override Task OnPreBuild()
        {
            PlayerSettings.Android.bundleVersionCode = buildVersionCode;
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = KeyStoreLocation;
            PlayerSettings.Android.keystorePass = KeyStorePassword;
            PlayerSettings.Android.keyaliasName = KeyAliasName;
            PlayerSettings.Android.keyaliasPass = KeyAliasPassword;
            EditorUserBuildSettings.buildAppBundle = aab;
            return Task.CompletedTask;
        }

        public string KeyStoreLocation
        {
            get => EditorPrefs.GetString("Builder.Android.Keystore");
            set => EditorPrefs.SetString("Builder.Android.Keystore", value);
        }

        public string KeyStorePassword
        {
            get => EditorPrefs.GetString("Builder.Android.Keystore.Password");
            set => EditorPrefs.SetString("Builder.Android.Keystore.Password", value);
        }

        public string KeyAliasName
        {
            get => EditorPrefs.GetString("Builder.Android.KeyAlias");
            set => EditorPrefs.SetString("Builder.Android.KeyAlias", value);
        }

        public string KeyAliasPassword
        {
            get => EditorPrefs.GetString("Builder.Android.KeyAlias.Password");
            set => EditorPrefs.SetString("Builder.Android.KeyAlias.Password", value);
        }
    }
}