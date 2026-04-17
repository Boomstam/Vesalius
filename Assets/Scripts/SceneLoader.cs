using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using ParrelSync;
#endif

public enum BuildType
{
    Monitor,
    Client
}

/// <summary>
/// Detects the build type at startup and loads the appropriate additive scene.
/// Place on any GameObject that is loaded early (e.g. the Monitor/bootstrap scene).
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [Tooltip("Scene to load additively when running as Monitor (server / host).")]
    [SerializeField] private string _monitorScene = "Monitor";

    [Tooltip("Scene to load additively when running as Client (ParrelSync clone or client build).")]
    [SerializeField] private string _clientScene = "Client";

    /// <summary>
    /// The build type resolved for this instance. Available from Awake onwards.
    /// </summary>
    public static BuildType BuildType { get; private set; }

    private void Awake()
    {
        bool isClone = false;

#if UNITY_EDITOR
        isClone = ClonesManager.IsClone();
#endif

        BuildType = isClone ? BuildType.Client : BuildType.Monitor;

        string sceneToLoad = BuildType == BuildType.Client ? _clientScene : _monitorScene;

        Debug.Log($"[SceneLoader] BuildType = {BuildType} — loading additive scene '{sceneToLoad}'");
        SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
    }
}