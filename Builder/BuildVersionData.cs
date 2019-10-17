// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.
using UnityEngine;

[CreateAssetMenu(fileName = "BuildVersion", menuName = "Tools/Build Version")]
public class BuildVersionData : ScriptableObject
{
    [SerializeField]
    public int major;
    
    [SerializeField]
    public int minor;
    
    [SerializeField]
    public int patch;
    
    [SerializeField]
    public string commitHash;

    public override string ToString()
    {
        return $"{major}.{minor}.{patch}";
    }

    public string ToStringWithCommit()
    {
        return ToString() + " " + commitHash;
    }
}