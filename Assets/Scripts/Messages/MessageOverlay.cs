using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Client-side overlay that displays a targeted message for a server-specified duration.
/// Keep this on an always-active parent object; only the panel should be toggled.
/// </summary>
public class MessageOverlay : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _messageText;

    private Coroutine _hideCoroutine;

    private void Awake()
    {
        if (_panel != null)
            _panel.SetActive(false);
    }

    public void ShowMessage(string word, float duration)
    {
        if (_panel == null || _messageText == null)
        {
            Debug.LogWarning("[MessageOverlay] Panel or message text is not assigned.");
            return;
        }

        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);

        _messageText.text = word;
        _panel.SetActive(true);
        _hideCoroutine = StartCoroutine(HideAfterDelay(duration));

        Debug.Log($"[MessageOverlay] Showing '{word}' for {duration}s.");
    }

    public void HideMessage()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }

        if (_panel != null)
            _panel.SetActive(false);

        Debug.Log("[MessageOverlay] Hidden.");
    }

    private IEnumerator HideAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        HideMessage();
    }
}
