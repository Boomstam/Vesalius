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

    private readonly SyncVar<int>  _currentPart          = new SyncVar<int>(-1);
    private readonly SyncVar<bool> _completeAnatomyMode  = new SyncVar<bool>(false);

    // ── FishNet callbacks ─────────────────────────────────────────────────────

    public override void OnStartClient()
    {
        base.OnStartClient();

        _currentPart.OnChange         += OnCurrentPartChanged;
        _completeAnatomyMode.OnChange += OnCompleteAnatomyModeChanged;

        if (SceneLoader.BuildType == BuildType.Monitor)
        {
            StartCoroutine(FindAndSubscribeInputFieldCoroutine());
            StartCoroutine(FindAndSubscribeToggleCoroutine());
        }
        else
            StartCoroutine(FindViewManagerCoroutine());
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        _currentPart.OnChange         -= OnCurrentPartChanged;
        _completeAnatomyMode.OnChange -= OnCompleteAnatomyModeChanged;

        UnsubscribeInputField();
        UnsubscribeToggle();
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

    // ── Input handler ─────────────────────────────────────────────────────────

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

    // ── SyncVar callback ──────────────────────────────────────────────────────

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