using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class NetworkedMonitor : NetworkBehaviour
{
    private ViewManager    _viewManager;
    private TMP_InputField _partInputField;
    private Toggle         _completeAnatomyModeToggle;
    private Toggle         _slidersToggle;                          // ← new

    private readonly SyncVar<int>  _currentPart          = new SyncVar<int>(-1);
    private readonly SyncVar<bool> _completeAnatomyMode  = new SyncVar<bool>(false);
    private readonly SyncVar<bool> _slidersEnabled       = new SyncVar<bool>(false); // ← new

    // ── FishNet callbacks ─────────────────────────────────────────────────────

    public override void OnStartClient()
    {
        base.OnStartClient();

        _currentPart.OnChange         += OnCurrentPartChanged;
        _completeAnatomyMode.OnChange += OnCompleteAnatomyModeChanged;
        _slidersEnabled.OnChange      += OnSlidersEnabledChanged;   // ← new

        if (SceneLoader.BuildType == BuildType.Monitor)
        {
            StartCoroutine(FindAndSubscribeInputFieldCoroutine());
            StartCoroutine(FindAndSubscribeToggleCoroutine());
            StartCoroutine(FindAndSubscribeSlidersToggleCoroutine()); // ← new
        }
        else
            StartCoroutine(FindViewManagerCoroutine());
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        _currentPart.OnChange         -= OnCurrentPartChanged;
        _completeAnatomyMode.OnChange -= OnCompleteAnatomyModeChanged;
        _slidersEnabled.OnChange      -= OnSlidersEnabledChanged;   // ← new

        UnsubscribeInputField();
        UnsubscribeToggle();
        UnsubscribeSlidersToggle();                                  // ← new
    }

    // ── Find coroutines ───────────────────────────────────────────────────────

    private IEnumerator FindViewManagerCoroutine()
    {
        Debug.Log("[NetworkedMonitor] Searching for ViewManager…");

        while (true)
        {
            _viewManager = FindObjectOfType<ViewManager>();
            if (_viewManager != null)
            {
                Debug.Log("[NetworkedMonitor] ViewManager found.");
                yield break;
            }

            Debug.LogWarning("[NetworkedMonitor] ViewManager not found. Retrying in 1 s…");
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator FindAndSubscribeInputFieldCoroutine()
    {
        Debug.Log("[NetworkedMonitor] Searching for 'Part Number' input field…");

        while (true)
        {
            GameObject go = GameObject.Find("Part Number");
            if (go != null)
            {
                _partInputField = go.GetComponent<TMP_InputField>();
                if (_partInputField != null)
                {
                    _partInputField.onValueChanged.AddListener(OnPartInputChanged);
                    Debug.Log("[NetworkedMonitor] Subscribed to 'Part Number' input field.");
                    yield break;
                }
                else
                {
                    Debug.LogWarning("[NetworkedMonitor] 'Part Number' found but has no TMP_InputField. Retrying in 1 s…");
                }
            }
            else
            {
                Debug.LogWarning("[NetworkedMonitor] 'Part Number' not found. Retrying in 1 s…");
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void UnsubscribeInputField()
    {
        if (_partInputField != null)
        {
            _partInputField.onValueChanged.RemoveListener(OnPartInputChanged);
            _partInputField = null;
        }
    }

    private IEnumerator FindAndSubscribeToggleCoroutine()
    {
        Debug.Log("[NetworkedMonitor] Searching for 'Complete Anatomy Mode Toggle'…");

        while (true)
        {
            GameObject go = GameObject.Find("Complete Anatomy Mode Toggle");
            if (go != null)
            {
                _completeAnatomyModeToggle = go.GetComponent<Toggle>();
                if (_completeAnatomyModeToggle != null)
                {
                    _completeAnatomyModeToggle.onValueChanged.AddListener(OnCompleteAnatomyModeToggleChanged);
                    Debug.Log("[NetworkedMonitor] Subscribed to 'Complete Anatomy Mode Toggle'.");
                    yield break;
                }
                else
                {
                    Debug.LogWarning("[NetworkedMonitor] 'Complete Anatomy Mode Toggle' found but has no Toggle component. Retrying in 1 s…");
                }
            }
            else
            {
                Debug.LogWarning("[NetworkedMonitor] 'Complete Anatomy Mode Toggle' not found. Retrying in 1 s…");
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void UnsubscribeToggle()
    {
        if (_completeAnatomyModeToggle != null)
        {
            _completeAnatomyModeToggle.onValueChanged.RemoveListener(OnCompleteAnatomyModeToggleChanged);
            _completeAnatomyModeToggle = null;
        }
    }

    // ── Sliders Toggle find / unsubscribe ─────────────────────────────────────

    private IEnumerator FindAndSubscribeSlidersToggleCoroutine()
    {
        Debug.Log("[NetworkedMonitor] Searching for 'Sliders Toggle'…");

        while (true)
        {
            GameObject go = GameObject.Find("Sliders Toggle");
            if (go != null)
            {
                _slidersToggle = go.GetComponent<Toggle>();
                if (_slidersToggle != null)
                {
                    _slidersToggle.onValueChanged.AddListener(OnSlidersToggleChanged);
                    Debug.Log("[NetworkedMonitor] Subscribed to 'Sliders Toggle'.");
                    yield break;
                }
                else
                {
                    Debug.LogWarning("[NetworkedMonitor] 'Sliders Toggle' found but has no Toggle component. Retrying in 1 s…");
                }
            }
            else
            {
                Debug.LogWarning("[NetworkedMonitor] 'Sliders Toggle' not found. Retrying in 1 s…");
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void UnsubscribeSlidersToggle()
    {
        if (_slidersToggle != null)
        {
            _slidersToggle.onValueChanged.RemoveListener(OnSlidersToggleChanged);
            _slidersToggle = null;
        }
    }

    // ── Input handlers ────────────────────────────────────────────────────────

    private void OnPartInputChanged(string value)
    {
        if (!int.TryParse(value, out int part))
            return;

        Debug.Log($"[NetworkedMonitor] Monitor requesting part {part}.");
        RequestSetPartServerRpc(part);
    }

    private void OnCompleteAnatomyModeToggleChanged(bool enabled)
    {
        Debug.Log($"[NetworkedMonitor] Monitor requesting completeAnatomyMode {enabled}.");
        RequestSetCompleteAnatomyModeServerRpc(enabled);
    }

    private void OnSlidersToggleChanged(bool enabled)
    {
        Debug.Log($"[NetworkedMonitor] Monitor requesting slidersEnabled {enabled}.");
        RequestSetSlidersEnabledServerRpc(enabled);
    }

    // ── RPC ───────────────────────────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    private void RequestSetPartServerRpc(int part)
    {
        Debug.Log($"[NetworkedMonitor] Server setting _currentPart to {part}.");
        _currentPart.Value = part;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSetCompleteAnatomyModeServerRpc(bool enabled)
    {
        Debug.Log($"[NetworkedMonitor] Server setting _completeAnatomyMode to {enabled}.");
        _completeAnatomyMode.Value = enabled;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSetSlidersEnabledServerRpc(bool enabled)
    {
        Debug.Log($"[NetworkedMonitor] Server setting _slidersEnabled to {enabled}.");
        _slidersEnabled.Value = enabled;
    }

    // ── SyncVar callbacks ─────────────────────────────────────────────────────

    private void OnCurrentPartChanged(int prev, int next, bool asServer)
    {
        Debug.Log($"[NetworkedMonitor] Part changed {prev} → {next} (asServer={asServer}).");

        if (_viewManager != null)
            _viewManager.SetPart(next);
        else if (SceneLoader.BuildType != BuildType.Monitor)
            Debug.LogWarning("[NetworkedMonitor] ViewManager not yet resolved — part change dropped.");
    }

    private void OnCompleteAnatomyModeChanged(bool prev, bool next, bool asServer)
    {
        Debug.Log($"[NetworkedMonitor] CompleteAnatomyMode changed {prev} → {next} (asServer={asServer}).");

        if (_viewManager != null)
            _viewManager.SetCompleteAnatomyMode(next);
        else if (SceneLoader.BuildType != BuildType.Monitor)
            Debug.LogWarning("[NetworkedMonitor] ViewManager not yet resolved — completeAnatomyMode change dropped.");
    }

    private void OnSlidersEnabledChanged(bool prev, bool next, bool asServer)
    {
        Debug.Log($"[NetworkedMonitor] SlidersEnabled changed {prev} → {next} (asServer={asServer}).");

        if (SceneLoader.BuildType != BuildType.Client)
            return;

        GameObject sliders = GameObject.Find("Sliders");
        if (sliders != null)
        {
            sliders.SetActive(next);
            Debug.Log($"[NetworkedMonitor] 'Sliders' GameObject set active → {next}.");
        }
        else
        {
            Debug.LogWarning("[NetworkedMonitor] 'Sliders' GameObject not found in scene.");
        }
    }

    // Leave these here for last minute changes
    [ServerRpc(RequireOwnership = false)]
    private void BackupServerRPC(string data)
    {
        BackupClientRPC(data);
    }

    [ObserversRpc]
    private void BackupClientRPC(string data)
    {
        
    }
}