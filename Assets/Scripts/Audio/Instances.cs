using UnityEngine;

/// <summary>
/// Lazy-loaded static accessors for scene singletons.
/// BuildType is provided by SceneLoader, not here.
/// Add entries as new managers are introduced.
/// </summary>
public static class Instances
{
    private static AudioManager    _audioManager;
    private static NetworkedMonitor _networkedMonitor;

    public static ColorOverlay ColorOverlay { get; set; }

    public static AudioManager AudioManager
    {
        get
        {
            if (_audioManager == null)
                _audioManager = Object.FindObjectOfType<AudioManager>();

            if (_audioManager == null)
                throw new System.Exception("[Instances] AudioManager not found in scene.");

            return _audioManager;
        }
    }

    public static NetworkedMonitor NetworkedMonitor
    {
        get
        {
            if (_networkedMonitor == null)
                _networkedMonitor = Object.FindObjectOfType<NetworkedMonitor>();

            if (_networkedMonitor == null)
                throw new System.Exception("[Instances] NetworkedMonitor not found in scene.");

            return _networkedMonitor;
        }
    }
}
