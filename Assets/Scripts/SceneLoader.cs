using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using ParrelSync;
#endif

public enum BuildType
{
    Server,
    MainEditor,
    Client
}

/// <summary>
/// Resolves the BuildType for this instance and additively loads the matching scene.
/// Must run before NetworkBootstrapper (set Script Execution Order if needed).
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string _serverScene     = "Server";
    [SerializeField] private string _mainEditorScene = "Monitor";
    [SerializeField] private string _clientScene     = "Client";

    /// <summary>Available from Awake onwards.</summary>
    public static BuildType BuildType { get; private set; }

    private void Awake()
    {
#if UNITY_EDITOR
        BuildType = ClonesManager.IsClone() ? BuildType.Client : BuildType.MainEditor;
#elif UNITY_SERVER
    BuildType = BuildType.Server;
#else
    BuildType = BuildType.Client;
#endif

        if (BuildType == BuildType.Server)
        {
            Debug.Log("[SceneLoader] Server build — skipping additive scene load.");
            return;
        }

        string sceneToLoad = BuildType == BuildType.MainEditor ? _mainEditorScene : _clientScene;

        Debug.Log($"[SceneLoader] BuildType = {BuildType} — loading additive scene '{sceneToLoad}'");
        SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
    }
}