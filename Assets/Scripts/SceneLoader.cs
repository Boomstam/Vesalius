using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using ParrelSync;
#endif

public enum BuildType
{
    Server,
    Monitor,
    Client
}

public enum TransportType
{
    Tugboat,
    Bayou
}

/// <summary>
/// Resolves BuildType and TransportType for this instance, then additively loads the matching scene.
/// Must run before NetworkBootstrapper (set Script Execution Order if needed).
///
/// BuildType  → what role this instance plays (server / monitor / client)
/// TransportType → which transport the network layer should use (Tugboat / Bayou)
///
/// In the editor, clone argument parity drives TransportType:
///   even index (0, 2, 4 …) → Tugboat   (native clients)
///   odd  index (1, 3, 5 …) → Bayou     (WebGL clients — Step 2 onwards)
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string _serverScene  = "Server";
    [SerializeField] private string _monitorScene = "Monitor";
    [SerializeField] private string _clientScene  = "Client";

    /// <summary>Available from Awake onwards.</summary>
    public static BuildType    BuildType     { get; private set; }

    /// <summary>Available from Awake onwards.</summary>
    public static TransportType TransportType { get; private set; }

    private void Awake()
    {
#if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            int.TryParse(ClonesManager.GetArgument(), out int cloneIndex);
            BuildType     = BuildType.Client;
            TransportType = (cloneIndex % 2 == 0) ? TransportType.Tugboat : TransportType.Bayou;
        }
        else
        {
            BuildType     = BuildType.Monitor;
            TransportType = TransportType.Tugboat;
        }
#elif UNITY_SERVER
    BuildType     = BuildType.Server;
    TransportType = TransportType.Tugboat;
#elif UNITY_WEBGL
    BuildType     = BuildType.Client;
    TransportType = TransportType.Bayou;
#else
    BuildType     = BuildType.Client;
    TransportType = TransportType.Tugboat;
#endif

        Debug.Log($"[SceneLoader] BuildType={BuildType}  TransportType={TransportType}");

        if (BuildType == BuildType.Server)
        {
            Debug.Log("[SceneLoader] Server build — skipping additive scene load.");
            return;
        }

        string sceneToLoad = BuildType == BuildType.Monitor ? _monitorScene : _clientScene;
        Debug.Log($"[SceneLoader] Loading additive scene '{sceneToLoad}'");
        SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
    }
}