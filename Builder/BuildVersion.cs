using UnityEngine;
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.
[DefaultExecutionOrder(-1500)]
public class BuildVersion : MonoBehaviour
{
    [SerializeField]
    public BuildVersionData data;

    private static BuildVersion _instance;
    public static BuildVersion Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BuildVersion>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this;

        if (data == null)
        {
            Debug.LogError("No version data found?! " + name);
        }
        else
        {
            Debug.Log(VersionCommit);
        }
    }

    public static string Version
    {
        get
        {
            if (Instance == null || Instance.data == null) return string.Empty;
            return Instance.data.ToString();
        }
    }

    public static string Commit
    {
        get
        {
            if (Instance == null || Instance.data == null) return string.Empty;
            return Instance.data.commitHash;
        }
    }

    public static string VersionCommit
    {
        get
        {
            if (Instance == null || Instance.data == null) return string.Empty;
            return Instance.data.ToStringWithCommit();
        }
    }

    
}