using System.Collections;
using TMPro;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class NetworkedMonitor : NetworkBehaviour
{
    private ViewManager    _viewManager;
    private TMP_InputField _partInputField;

    private readonly SyncVar<int> _currentPart = new SyncVar<int>(-1);

    // ── FishNet callbacks ─────────────────────────────────────────────────────

    public override void OnStartClient()
    {
        base.OnStartClient();

        _currentPart.OnChange += OnCurrentPartChanged;

        if (SceneLoader.BuildType == BuildType.Monitor)
            StartCoroutine(FindAndSubscribeInputFieldCoroutine());
        else
            StartCoroutine(FindViewManagerCoroutine());
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        _currentPart.OnChange -= OnCurrentPartChanged;

        UnsubscribeInputField();
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

    // ── Input handler ─────────────────────────────────────────────────────────

    private void OnPartInputChanged(string value)
    {
        if (!int.TryParse(value, out int part))
            return;

        Debug.Log($"[NetworkedMonitor] Monitor requesting part {part}.");
        RequestSetPartServerRpc(part);
    }

    // ── RPC ───────────────────────────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    private void RequestSetPartServerRpc(int part)
    {
        Debug.Log($"[NetworkedMonitor] Server setting _currentPart to {part}.");
        _currentPart.Value = part;
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
}