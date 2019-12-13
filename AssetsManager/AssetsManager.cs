using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AssetBundles;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;

#endif

public class AssetsManager {

    public static bool SimulateAssetBundle = false;
    public static string AssetBundleURL;
    public static bool LogAssetAccess = true;

    private static AssetsManager _instance;

    private Dictionary<string, AssetBundle> _assetBundles;
    private Dictionary<string, Task<AssetBundle>> _loadingAssetBundles;
    private bool _init;

    private AssetBundleManager _assetBundleManager;

    public static AssetsManager Instance => _instance ?? (_instance = new AssetsManager());

    private async Task Init() {
        _assetBundleManager = new AssetBundleManager();

        if (Application.isEditor && SimulateAssetBundle) {
            _assetBundleManager.UseSimulatedUri();
        } else {
            _assetBundleManager.SetBaseUri(AssetBundleURL);
        }

        await _assetBundleManager.Initialize();
        _assetBundles = new Dictionary<string, AssetBundle>();
        _loadingAssetBundles = new Dictionary<string, Task<AssetBundle>>();
        _init = true;
    }

    public async Task<AssetBundle> GetBundle(FixedAssetBundles assetBundleName) {
        return await GetBundle(assetBundleName.ToString());
    }

    /// <summary>
    /// Return an AssetBundle using its name
    /// </summary>
    /// <param name="assetBundleName">The name of the asset bundle to load</param>
    /// <returns></returns>
    /// <exception cref="Exception">Will throw an exception if the bundle does not exist</exception>
    public async Task<AssetBundle> GetBundle(string assetBundleName) {
        if (!_init) await Init();

        var lowerName = assetBundleName.ToLower();

        if (_loadingAssetBundles.ContainsKey(lowerName)) {
            return await _loadingAssetBundles[lowerName];
        }

        if (!_assetBundles.ContainsKey(lowerName)) {
            var loadingTask = _assetBundleManager.GetBundle(lowerName);
            _loadingAssetBundles.Add(lowerName, loadingTask);

            var bundle = await loadingTask;
            _loadingAssetBundles.Remove(lowerName);

            if (bundle == null) {
                throw new Exception($"Failed to load AssetBundle {lowerName} !");
            }

            // We check again for concurrency reasons
            _assetBundles.Add(lowerName, bundle);
        }

        return _assetBundles[lowerName];
    }

    /// <summary>
    /// Load an asset bundle but don't use the result
    /// </summary>
    /// <param name="assetBundleName"></param>
    /// <returns></returns>
    public async Task PreloadBundle(FixedAssetBundles assetBundleName) {
        if (SimulateAssetBundle) return;
        await GetBundle(assetBundleName);
    }

    public static async Task<IObservable<float>> LoadLevelAsync(string assetBundleName, string levelName, LoadSceneMode loadSceneMode) {
#if UNITY_EDITOR
        if (SimulateAssetBundle) {
            if (LogAssetAccess) {
                Debug.Log("=> Scene (Simu) " + levelName, LogColor, true);
            }

            LoadLevelEditor(assetBundleName, levelName, loadSceneMode);

            // Create fake observable
            return Observable.TimerFrame(1).Select(x => (float) x);
        }
#endif
        if (LogAssetAccess) {
            Debug.Log("=> Scene (AB) " + levelName, LogColor, true);
        }

        return await Instance.LoadLevelAsyncInternal(assetBundleName, levelName, loadSceneMode);
    }

#if UNITY_EDITOR
    public static void LoadLevelEditor(string assetBundleName, string levelName, LoadSceneMode loadSceneMode) {

        var path = "Assets/Scenes/" + levelName + ".unity";
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(loadSceneMode));

        if (LogAssetAccess) {
            Debug.Log("OK Scene (Simu) " + path, LogColor, true);
        }
    }
#endif

    private async Task<IObservable<float>> LoadLevelAsyncInternal(string assetBundleName, string levelName, LoadSceneMode loadSceneMode) {
        if (!_init) await Init();

        var observable = (await Instance._assetBundleManager.LoadLevelAsync(assetBundleName, levelName, loadSceneMode))
            .AsObservable()
            .Select(operation => operation.progress)
            .Last();
        return observable;
    }

    public static async Task<T> LoadAsset<T>(FixedAssetBundles assetBundleName, string assetName) where T : UnityEngine.Object {
        var debugName = GetDebugName<T>(assetBundleName.ToString(), assetName);
        if (LogAssetAccess) {
            Debug.Log("=> " + debugName, LogColor, true);
        }

#if UNITY_EDITOR
        if (SimulateAssetBundle) {
            return LoadAssetEditorAsync<T>(assetBundleName.ToString(), assetName);
        }
#endif
        var result = await Instance.LoadAssetInternal<T>(assetBundleName.ToString(), assetName);

        return result;
    }

#if UNITY_EDITOR
    public static T LoadAssetEditorAsync<T>(string assetBundleName, string assetName, bool log = true) where T : UnityEngine.Object {
        assetBundleName = assetBundleName.ToLower();
        var debugName = GetDebugName<T>(assetBundleName, assetName);
        string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(assetBundleName, assetName);
        if (assetPaths.Length == 0) {
            if (log) {
                Debug.LogError("! Asset not found " + debugName, LogColor);
            }

            return null;
        }

        if (assetPaths.Length > 1) {
            Debug.LogWarning("Multiple assets found name=[" + assetName + "] bundle=[" + assetBundleName + "]. Only the first one will be used.");
        }

        if (LogAssetAccess) {
            if (log) {
                Debug.Log("OK (Simu)" + debugName, LogColor, true);
            }
        }

        var target = AssetDatabase.LoadAssetAtPath<T>(assetPaths[0]);
        return target;
    }
#endif
    private async Task<T> LoadAssetInternal<T>(string assetBundleName, string assetName) where T : UnityEngine.Object {
        var debugName = GetDebugName<T>(assetBundleName, assetName);
        if (!_init) await Init();

        try {
            var bundle = await GetBundle(assetBundleName).TimeoutAfter(120);
            var asset = bundle.LoadAsset<T>(assetName);
            if (asset == null) Debug.LogError("Failed to load Asset {0} from {1} ({2})", logTag: Logger.LogTag.Assets, args: new object[] {assetName, assetBundleName, typeof(T)});

            if (LogAssetAccess) {
                Debug.Log("OK (AB) " + debugName, LogColor, true);
            }

            return asset;
        } catch (Exception e) {
            Debug.LogError("Couldn't load asset [" + assetName + "] in bundle [" + assetBundleName + "]");
            Debug.LogException(e);
            throw;
        }
    }

    private static string GetDebugName<T>(string assetBundleName, string assetName) {
        return "[" + assetName + "]<" + typeof(T) + "> (" + assetBundleName + ")";
    }
}