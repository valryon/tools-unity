// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Simple animator
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SimpleAnimator : MonoBehaviour
{
  public bool debugMode = false;
  public SimpleAnimation defaultAnimation;
  public SimpleAnimation[] clips;
  public bool destroyGameObjectWhenFinished = false;
  public float speed = 1f;

  /// <summary>
  /// Animation "name" is completed
  /// </summary>
  public event System.Action<string> AnimationCompleted;

  /// <summary>
  /// Callback that replace the destroy feature
  /// </summary>
  public event System.Action<GameObject> OnDestruction;

  private SimpleAnimation defaultAnimationSaved;
  private SpriteRenderer spriteRenderer;
  private SimpleAnimation currentAnimation;
  private bool firstInit;
  private int currentFrame;
  private float timer;
  private float previousRealtime;

  void Awake()
  {
    spriteRenderer = GetComponent<SpriteRenderer>();

    defaultAnimationSaved = defaultAnimation;
  }

  void Start()
  {
    firstInit = true;

    if (defaultAnimation != null)
    {
      // We allow clips to be empty if we just want to use the default anim
      if (clips.Length == 0)
      {
        clips = new SimpleAnimation[] { defaultAnimation };
      }
      else if (Find(defaultAnimation.name) == null)
      {
        Debug.LogError("Playing a default animation that is not registered as a clip! Not that I care...");
      }

      if (currentAnimation == null) Play(defaultAnimation, true);
    }

    firstInit = false;

    // Maybe there is nothing to animate?
    // Better disable the script
    if (clips.Length == 1 && clips[0] == defaultAnimation && defaultAnimation.frames.Length == 1)
    {
      enabled = false;
    }
  }

  void Update()
  {
    // Animator needs... an animation!
    if (currentAnimation == null) return;

    // Sometimes we are not animating anything
    if (currentAnimation.frames.Length == 1 && currentAnimation.loop) return;

    // Default animation with only one frame... do nothing
    if (currentAnimation == defaultAnimation && currentAnimation.frames.Length == 1) return;

    // Update time
    if (DisableAutoNextFrame == false)
    {
      float timeDelta = 0f;
      if (Time.timeScale > 0)
      {
        timeDelta = Time.deltaTime;
      }
      else
      {
        // Animation may continue!
        if (currentAnimation.loop)
        {
          timeDelta = Time.realtimeSinceStartup - previousRealtime;
        }
      }

      timer -= timeDelta;
      if (timer <= 0)
      {
        currentFrame++;
        timer = currentAnimation.FrameDuration / speed;
      }
    }

    // End of the animation
    if (currentFrame >= currentAnimation.frames.Length)
    {
      if (currentAnimation.loop == false)
      {
        if (destroyGameObjectWhenFinished)
        {
          if (OnDestruction != null)
          {
            OnDestruction(this.gameObject);
          }
          else
          {
            Destroy(this.gameObject);
            return;
          }
        }

        // Back to default (can be nothing)
        Play(defaultAnimation, true);

        if (AnimationCompleted != null) AnimationCompleted(currentAnimation.name);
        AnimationCompleted = null;
      }

      // Reset anyway
      Reset();
    }

    // Change sprite
    ChangeSprite();

    previousRealtime = Time.realtimeSinceStartup;
  }

  void ChangeSprite()
  {
    if (currentAnimation != null && spriteRenderer != null)
    {
      if (currentAnimation.frames.Length > currentFrame)
      {
        spriteRenderer.sprite = currentAnimation.frames[currentFrame];
      }
      else
      {
        Debug.LogError("Animation Error: " + this + "#ChangeSprite currentFrame=" + currentFrame + " total frames=" + currentAnimation.frames.Length);
      }
    }
  }

  /// <summary>
  /// Play animation from name
  /// </summary>
  /// <param name="name"></param>
  public SimpleAnimation Play(string name)
  {
    return Play(name, false, true);
  }

  /// <summary>
  /// Play animation from name
  /// </summary>
  /// <param name="name"></param>
  public SimpleAnimation Play(string name, bool reset, bool required)
  {
    return Play(name, reset, required, null);
  }

  /// <summary>
  /// Play animation from name
  /// </summary>
  /// <param name="name"></param>
  public SimpleAnimation Play(string name, bool reset, bool required, System.Action<string> animCompleted)
  {
    if (this.enabled == false) return null;

    // Find the clip in the collection
    SimpleAnimation[] anims = Find(name);

    if (anims == null || anims.Length == 0)
    {
      if (required)
      {
        Debug.LogError("Missing animation \"" + name + "\" " + this);
      }
    }
    else
    {
      this.AnimationCompleted += animCompleted;

      // Select one anim
      var anim = anims[Random.Range(0, anims.Length)];

      Play(anim, reset);
      return anim;
    }

    return null;
  }

  /// <summary>
  /// Play animation
  /// </summary>
  /// <param name="anim"></param>
  public void Play(SimpleAnimation anim, bool reset)
  {
    if (this.enabled == false) return;

    if (currentAnimation != anim)
    {
      currentAnimation = anim;
      Reset();
    }
    else if (currentAnimation == anim && reset)
    {
      Reset();
    }
    else
    {
      // Already playing the right animation
      return;
    }

    if (debugMode)
    {
      Debug.Log("ANIMATION - Playing animation \"" + currentAnimation + "\" (" + currentAnimation.Duration + " sec)");
    }

    if (currentAnimation.frames.Length == 0)
    {
      Debug.LogWarning("Empty animation (no frames): " + anim.name + " " + anim);
      currentAnimation = null;
    }
  }

  /// <summary>
  /// Reset animation parameters
  /// </summary>
  public void Reset()
  {
    currentFrame = 0;
    if (firstInit && currentAnimation.randomFirstFrame)
    {
      currentFrame = Random.Range(0, currentAnimation.frames.Length);
    }

    if (currentAnimation != null)
    {
      timer = currentAnimation.FrameDuration;
    }

    ChangeSprite();
  }

  /// <summary>
  /// Check if the animation exists for this instance
  /// </summary>
  /// <param name="name"></param>
  /// <returns></returns>
  public bool Contains(string name)
  {
    return Find(name).Length > 0;
  }

  public void SetAndPlayDefault(string name)
  {
    SetDefault(name);
    Play(name);
  }

  /// <summary>
  /// Change the default animation
  /// </summary>
  public void SetDefault(string name)
  {
    defaultAnimationSaved = defaultAnimation;

    var anims = Find(name);
    if (anims.Length > 0)
    {
      defaultAnimation = anims[Random.Range(0, anims.Length)]; ;

      if (debugMode)
      {
        Debug.Log("ANIMATION - New default: " + name + " " + defaultAnimation);
      }
    }
    else
    {
      Debug.LogError("Missing animation \"" + name + "\", can't set as default! " + this);
    }
  }

  /// <summary>
  /// Set the default animation back
  /// </summary>
  public void RestoreDefault()
  {
    defaultAnimation = defaultAnimationSaved;

    if (debugMode)
    {
      Debug.Log("ANIMATION - Restore default: " + defaultAnimation.name + " " + defaultAnimation);
    }
  }

  private SimpleAnimation[] Find(string name)
  {
    List<SimpleAnimation> anims = new List<SimpleAnimation>();

    foreach (var clip in clips)
    {
      if (name.ToLower() == clip.name.ToLower())
      {
        anims.Add(clip);
      }
    }
    return anims.ToArray();
  }

  public float TimeLeft
  {
    get
    {
      if (currentAnimation != null)
      {
        return (currentAnimation.frames.Length - currentFrame) * currentAnimation.FrameDuration;
      }

      return 0f;
    }
  }

  public SimpleAnimation Current
  {
    get
    {
      return currentAnimation;
    }
  }

  /// <summary>
  /// Get current animation frame
  /// </summary>
  public int Frame
  {
    get
    {
      return currentFrame;
    }
    set
    {
      currentFrame = value;
    }
  }

  public bool DisableAutoNextFrame
  {
    get; set;
  }

  public SpriteRenderer SpriteRenderer
  {
    get
    {
      return spriteRenderer;
    }
  }
}