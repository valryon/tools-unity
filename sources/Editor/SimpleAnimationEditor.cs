#if UNITY_EDITOR
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pixelnest
{

  [CanEditMultipleObjects]
  [CustomEditor(typeof(SimpleAnimation))]
  public class SimpleAnimationEditor : Editor
  {
    #region Menus

    [MenuItem("Assets/Create/2D/Simple Animation")]
    public static void CreateSpriteFrameAnimation()
    {
      var anim = ScriptableObjectUtility.CreateAsset<SimpleAnimation>(Selection.activeObject.name);

      AutoFillAnimation(anim);
    }

    public static void AutoFillAnimation(SimpleAnimation anim, bool reverse = false, bool clean = true)
    {
      if (anim.allowAutoUpdate)
      {
        string path = AssetDatabase.GetAssetPath(anim.GetInstanceID());

        // Get the directory
        string dir = Path.GetDirectoryName(path);


        if (Directory.Exists(dir))
        {
          // List all sprites from the dir
          List<Sprite> sprites = new List<Sprite>();

          foreach (string file in Directory.GetFiles(dir))
          {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(file))
            {
              if (asset is Sprite)
              {
                // Add them to the anim
                sprites.Add(asset as Sprite);
              }
            }
          }

          if (reverse)
          {
            sprites.Reverse();
          }

          if (clean == false)
          {
            sprites.InsertRange(0, anim.frames);
          }

          anim.frames = sprites.ToArray();

          EditorUtility.SetDirty(anim);

          AssetDatabase.SaveAssets();
          sprites = null;
        }
      }
    }

    #endregion

    #region Inspector

    private SimpleAnimation anim;
    private bool play;
    private double elapsedTime;
    private double cooldown;
    private Sprite currentSprite;
    private int currentIndex;
    private float speedFactor;

    void OnEnable()
    {
      anim = target as SimpleAnimation;
      play = false;
      currentIndex = 0;
      speedFactor = 1f;

      elapsedTime = EditorApplication.timeSinceStartup;
      EditorApplication.update += Update;

      take = anim.frames.Length;
    }
    void OnDisable() { EditorApplication.update -= Update; }

    void Update()
    {
      if (currentSprite == null && anim.frames.Length > 0)
      {
        currentIndex = 0;
        currentSprite = anim.frames[currentIndex];
      }
      if (play)
      {
        double t = EditorApplication.timeSinceStartup - elapsedTime;
        elapsedTime = EditorApplication.timeSinceStartup;
        cooldown -= t;
        if (cooldown < 0)
        {
          cooldown = (1f / anim.imagesPerSeconds) * speedFactor;

          if (currentIndex < anim.frames.Length - 1 || anim.loop)
          {
            currentIndex++;
            if (currentIndex >= anim.frames.Length && anim.loop) currentIndex = 0;
            Repaint();
          }
        }
      }

      if (currentIndex < anim.frames.Length)
      {
        currentSprite = anim.frames[currentIndex];
      }
    }

    private int skip, take;

    public override void OnInspectorGUI()
    {
      if (anim != null)
      {
        GUILayout.Label("Settings", EditorStyles.boldLabel);

        DrawDefaultInspector();

        if (Selection.objects.Length == 1)
        {
          EditorGUILayout.Space();

          EditorGUILayout.LabelField("Cut", EditorStyles.boldLabel);
          GUILayout.BeginVertical();
          skip = EditorGUILayout.IntField("skip", skip, GUILayout.Width(240), GUILayout.ExpandWidth(false));
          take = EditorGUILayout.IntField("take", take, GUILayout.Width(240), GUILayout.ExpandWidth(false));
          if (GUILayout.Button("Cut", GUILayout.Width(35)))
          {
            anim.frames = anim.frames.Skip(skip).Take(take).ToArray();
          }
          GUILayout.EndVertical();
          EditorGUILayout.Space();

          // Magic button
          GUILayout.Label("Magic frames fill", EditorStyles.boldLabel);

          if (anim.allowAutoUpdate && GUILayout.Button("Auto-fill"))
          {
            AutoFillAnimation(anim);
            take = anim.frames.Length;
          }
          if (anim.allowAutoUpdate && GUILayout.Button("Reverse auto-fill"))
          {
            AutoFillAnimation(anim, true);
            take = anim.frames.Length;
          }
          if (anim.allowAutoUpdate && GUILayout.Button("Cumulative auto-fill"))
          {
            AutoFillAnimation(anim, false, false);
            take = anim.frames.Length;
          }
          if (anim.allowAutoUpdate && GUILayout.Button("Cumulative reverse auto-fill"))
          {
            AutoFillAnimation(anim, true, false);
            take = anim.frames.Length;
          }

          EditorGUILayout.Space();
          GUILayout.Label("Quick name", EditorStyles.boldLabel);
          GUILayout.BeginVertical();

          string[] options = { "default", "attack", "attackstart", "attackend", "load", "loaded", "unload", "destroy" };
          int count = 0;
          const int max = 4;

          GUILayout.BeginHorizontal();

          foreach (var option in options)
          {
            if (GUILayout.Button(option))
            {
              anim.name = option;
              EditorUtility.SetDirty(anim);
            }

            count++;
            if (count == max)
            {
              count = 0;
              GUILayout.EndHorizontal();
              GUILayout.BeginHorizontal();
            }
          }

          // Fill with empty spaces
          for (int i = 0; i < max - count; i++)
          {
            GUILayout.Label("");
          }

          GUILayout.EndHorizontal();

          GUILayout.EndVertical();

          EditorGUILayout.Space();
          EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

          GUILayout.BeginHorizontal();
          if (GUILayout.Button("▶️▶️ Play", GUILayout.Width(55), GUILayout.Height(30)))
          {
            play = true;
            currentIndex = 0;
            speedFactor = 1f;
          }
          if (GUILayout.Button("▶️ Slow", GUILayout.Width(55), GUILayout.Height(30)))
          {
            play = true;
            currentIndex = 0;
            speedFactor = 4f;
          }
          if (GUILayout.Button("▪️ Stop", GUILayout.Width(55), GUILayout.Height(30)))
          {
            play = false;
            currentIndex = 0;
            currentSprite = null;
          }

          GUILayout.BeginVertical();
          EditorGUILayout.LabelField("Preview: " + (play ? "on" : "off"));
          EditorGUILayout.LabelField("Frame " + (currentIndex + 1) + "/" + anim.frames.Length);
          GUILayout.EndVertical();

          GUILayout.EndHorizontal();
          if (currentSprite != null)
          {
            EditorGUI.DrawTextureTransparent(EditorGUILayout.GetControlRect(GUILayout.Width(250), GUILayout.Height(250)), currentSprite.texture, ScaleMode.ScaleToFit);
          }
        }
      }
    }

    #endregion
  }
}
#endif