// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.
using System.IO;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ThumbnailsEditorWindow : OdinEditorWindow {
    private const string TITLE = "Item Screenshoter";

    private const int THUMBNAIL_WIDTH = 512;
    private const int THUMBNAIL_HEIGHT = 256;

    private GameObject _item, _createdItem;
    private RenderTexture _rt;
    private UnityEngine.SceneManagement.Scene _tempScene;
    private string _path;
    private Camera _cam;

    [MenuItem("Tools/" + TITLE, false, 20)]
    public static void OpenWindow() {
        Show(null);
    }

    /// <summary>
    /// Open the tool on a given GameObject from an external source
    /// </summary>
    /// <param name="item"></param>
    public static void Show(GameObject item) {
        var w = (ThumbnailsEditorWindow) GetWindow(typeof(ThumbnailsEditorWindow), false, TITLE);
        w.SetItem(item);
        w.Show();
    }

    private void SetItem(GameObject item) {
        _item = item;
        ResetScene();
    }

    protected override void OnEnable() {
        base.OnEnable();
        maxSize = new Vector2(300, 350);
        minSize = maxSize;
    }

    protected override void OnGUI() {
        var i = (GameObject) EditorGUILayout.ObjectField("Selected item", _item, typeof(GameObject), false);
        if (i != _item) {
            SetItem(i);
            return;
        }

        if (_item == null) {
            EditorGUILayout.LabelField("No item selected");
            return;
        }

        _path = EditorGUILayout.TextField(_path);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Adjust the item and the camera in the scene.");
        EditorGUILayout.LabelField("Then create the thumbnail.");

        if (_rt != null) {
            EditorGUILayout.LabelField(new GUIContent(_rt), GUILayout.Width(256), GUILayout.Height(128));
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Create thumbnail", GUILayout.Width(200), GUILayout.Height(50))) {
                CreateThumbnail();
                return;
            }

            EditorGUILayout.Space();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Magic Crop", GUILayout.Width(100), GUILayout.Height(20))) {
                MagicCrop();
                return;
            }

            if (GUILayout.Button("Reset scene", GUILayout.Width(100), GUILayout.Height(20))) {
                ResetScene();
                return;
            }

            EditorGUILayout.Space();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void ResetScene() {
        if (_item == null) return;

        // Create or clean new temp scene
        var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

        if (_tempScene.IsValid()) {
            EditorSceneManager.CloseScene(_tempScene, true);
        }

        _tempScene = s;

        // Add the item
        _createdItem = Instantiate(_item);
        _createdItem.transform.position = Vector3.zero;
        _createdItem.transform.rotation = Quaternion.identity;

        if (_rt != null) {
            DestroyImmediate(_rt);
        }

        _rt = new RenderTexture(THUMBNAIL_WIDTH, THUMBNAIL_HEIGHT, 24);

        // Add a camera
        var camGo = new GameObject("Camera");
        _cam = camGo.AddComponent<Camera>();
        _cam.orthographic = true;
        _cam.tag = "MainCamera";
        _cam.backgroundColor = new Color(0, 0, 0, 0);
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.targetTexture = _rt;

        MagicCrop();

        // Light
        var lightGo = new GameObject("Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;

        Repaint();
    }

    private void CreateThumbnail() {
        if (_rt == null) return;

        Debug.Log("Generating thumbnail for [" + _item.name + "] at " + _path);

        if (File.Exists(_path)) {
            AssetDatabase.DeleteAsset(_path);
            File.Delete(_path);
        }

        // RenderTexture to PNG
        RenderTexture currentActiveRt = RenderTexture.active;
        RenderTexture.active = _rt;
        Texture2D tex = new Texture2D(_rt.width, _rt.height);
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

        var bytes = tex.EncodeToPNG();
        File.WriteAllBytes(_path, bytes);
        DestroyImmediate(tex);

        RenderTexture.active = currentActiveRt;

        AssetDatabase.ImportAsset(_path, ImportAssetOptions.ForceUpdate);
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(_path);
        Selection.activeObject = t;
    }

    private void MagicCrop() {
        var renderer = _createdItem.GetComponentInChildren<Renderer>();
        if (renderer == null) return;
        var rect = new Rect(renderer.bounds.min.x, renderer.bounds.min.y, renderer.bounds.size.x, renderer.bounds.size.y);

        // Margins
        rect = rect.Expand(0.25f);

        _cam.transform.position = CalculateCameraPosition(rect);
        _cam.orthographicSize = CalculateOrthographicSize(_cam, rect);
    }

    // Source: https://answers.unity.com/questions/1231701/fitting-bounds-into-orthographic-2d-camera.html
    // Slightly modified :)

    /// <summary>
    /// Calculates a camera position given the a bounding box containing all the targets.
    /// </summary>
    /// <param name="boundingBox">A Rect bounding box containg all targets.</param>
    /// <returns>A Vector3 in the center of the bounding box.</returns>
    private Vector3 CalculateCameraPosition(Rect boundingBox) {
        return new Vector3(boundingBox.center.x, boundingBox.center.y, -10f);
    }

    /// <summary>
    /// Calculates a new orthographic size for the camera based on the target bounding box.
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="boundingBox">A Rect bounding box containg all targets.</param>
    /// <returns>A float for the orthographic size.</returns>
    private float CalculateOrthographicSize(Camera camera, Rect boundingBox) {
        float orthographicSize;
        if (boundingBox.width > boundingBox.height) {
            orthographicSize = Mathf.Abs(boundingBox.width) / camera.aspect / 2f;
        } else {
            orthographicSize = Mathf.Abs(boundingBox.height) / 2f;
        }

        return orthographicSize;
    }
}