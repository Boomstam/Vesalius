using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Client-side overlay that displays a targeted message for a server-specified duration.
/// Keep this on an always-active parent object; only the panel should be toggled.
/// </summary>
public class MessageOverlay : MonoBehaviour
{
    private const int OverlaySortingOrder = 100;

    public static MessageOverlay Instance { get; private set; }

    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private GameObject _backdrop;
    [Header("Flash")]
    [SerializeField] private float _flashSpeed = 4f;
    [SerializeField] [Range(0f, 1f)] private float _textMinAlpha = 0.2f;

    private Coroutine _hideCoroutine;
    private Coroutine _flashCoroutine;
    private Graphic _panelGraphic;
    private Color _messageTextBaseColor = Color.white;
    private Canvas _overlayCanvas;

    public bool IsVisible => _panel != null && _panel.activeSelf;

    private void Awake()
    {
        Instance = this;
        EnsureOverlayCanvasSorting();

        if (_backdrop == null && _panel != null)
        {
            Transform backdropTransform = _panel.transform.Find("Backdrop");
            if (backdropTransform != null)
                _backdrop = backdropTransform.gameObject;
        }

        if (_backdrop == null && _panel != null)
            _panelGraphic = _panel.GetComponent<Graphic>();

        if (_messageText != null)
            _messageTextBaseColor = _messageText.color;

        if (_panel != null)
            _panel.SetActive(false);
    }

    private void OnEnable()
    {
        EnsureOverlayCanvasSorting();
    }

    private void OnDestroy()
    {
        GlobalAudioSliderOverlay.Instance?.SetInteractionBlocked(false);

        if (Instance == this)
            Instance = null;
    }

    public void ShowMessage(string word, float duration, bool showBackdrop = true)
    {
        if (_panel == null || _messageText == null)
        {
            Debug.LogWarning("[MessageOverlay] Panel or message text is not assigned.");
            return;
        }

        EnsureOverlayCanvasSorting();
        GlobalAudioSliderOverlay.Instance?.SetInteractionBlocked(true);

        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);

        _messageText.text = word;
        SetBackdropVisible(showBackdrop);
        _panel.SetActive(true);
        StartFlashing();
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

        StopFlashing();
        GlobalAudioSliderOverlay.Instance?.SetInteractionBlocked(false);

        if (_panel != null)
            _panel.SetActive(false);

        Debug.Log("[MessageOverlay] Hidden.");
    }

    private IEnumerator HideAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        HideMessage();
    }

    private void SetBackdropVisible(bool visible)
    {
        if (_backdrop != null)
        {
            _backdrop.SetActive(visible);
            return;
        }

        if (_panelGraphic != null)
            _panelGraphic.enabled = visible;
    }

    private void StartFlashing()
    {
        StopFlashing();
        RestoreVisualState();
        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private void StopFlashing()
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }

        RestoreVisualState();
    }

    private IEnumerator FlashRoutine()
    {
        while (true)
        {
            float pulse = (Mathf.Sin(Time.time * _flashSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            float textAlpha = Mathf.Lerp(_textMinAlpha, 1f, pulse);

            if (_messageText != null)
                _messageText.color = WithAlpha(_messageTextBaseColor, _messageTextBaseColor.a * textAlpha);

            yield return null;
        }
    }

    private void RestoreVisualState()
    {
        if (_messageText != null)
            _messageText.color = _messageTextBaseColor;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private void EnsureOverlayCanvasSorting()
    {
        if (_overlayCanvas == null)
            _overlayCanvas = GetComponent<Canvas>();

        if (_overlayCanvas == null)
            _overlayCanvas = gameObject.AddComponent<Canvas>();

        _overlayCanvas.overrideSorting = true;
        _overlayCanvas.sortingOrder = OverlaySortingOrder;

        if (_panel != null && _panel.transform.parent != transform)
            _panel.transform.SetParent(transform, false);

        transform.SetAsLastSibling();
    }
}
