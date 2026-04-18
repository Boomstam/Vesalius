using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using ParrelSync;
#endif

public enum BuildType
{
    Monitor,       // Unity Editor (non-clone) OR standalone PC/Mac build
    Client,        // ParrelSync clone inside the Unity Editor
    MobileClient   // Android or iOS device build
}

/// <summary>
/// Detects the build type at compile-time and loads the appropriate additive scene.
/// Place on a GameObject in the Bootstrap/Loader scene which is always loaded first.
///
/// Detection rules (in priority order):
///   UNITY_EDITOR  + ParrelSync clone  → Client
///   UNITY_EDITOR  + not a clone       → Monitor
///   UNITY_ANDROID || UNITY_IOS        → MobileClient
///   Anything else (PC/Mac standalone) → Monitor
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [Tooltip("Scene to load additively when running as Monitor (server / host).")]
    [SerializeField] private string _monitorScene = "Monitor";

    [Tooltip("Scene to load additively when running as Client (ParrelSync clone in Editor).")]
    [SerializeField] private string _clientScene = "Client";

    [Tooltip("Scene to load additively when running as a Mobile Client (Android / iOS). " +
             "Defaults to the same scene as Client — change this if you want a dedicated mobile UI scene.")]
    [SerializeField] private string _mobileClientScene = "Client";

    /// <summary>
    /// The build type resolved for this instance. Available from Awake onwards.
    /// Downstream scripts (e.g. NetworkBootstrapper) read this value.
    /// </summary>
    public static BuildType BuildType { get; private set; }

    private void Awake()
    {
        // ── Compile-time platform detection ──────────────────────────────────
        // All branching is done via preprocessor directives so that dead code is
        // stripped from each platform's build. This avoids any runtime reflection
        // or symbol-lookup overhead, and ensures ParrelSync is never referenced
        // in a device build (it's Editor-only).

#if UNITY_EDITOR
        // Inside the Editor, use ParrelSync to distinguish Monitor from Client.
        bool isClone = ClonesManager.IsClone();
        BuildType = isClone ? BuildType.Client : BuildType.Monitor;

#elif UNITY_ANDROID || UNITY_IOS
        // Any device build is always a client — it never hosts a server.
        BuildType = BuildType.MobileClient;

#else
        // Standalone PC / Mac build → Monitor / server role.
        // If you later need a standalone Client build, add a dedicated
        // scripting define (e.g. STANDALONE_CLIENT) and handle it here.
        BuildType = BuildType.Monitor;
#endif

        // ── Resolve which scene to load ──────────────────────────────────────
        string sceneToLoad = BuildType switch
        {
            BuildType.Client       => _clientScene,
            BuildType.MobileClient => _mobileClientScene,
            _                      => _monitorScene   // Monitor (default)
        };

        Debug.Log($"[SceneLoader] Platform = {Application.platform} | " +
                  $"BuildType = {BuildType} | Loading scene '{sceneToLoad}'");

        SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
    }
}