using System.Collections;
using UnityEngine;
using TMPro;

public class TextColorAnimator : MonoBehaviour
{
    [Header("Color Settings")]
    [SerializeField] private Color targetColor = Color.white;

    [Header("Timing Settings")]
    [SerializeField] private float duration = 1f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private TMP_Text _text;
    private Color _initialColor;
    private Coroutine _activeCoroutine;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
        _initialColor = _text.color;
    }

    public void StartAnimation()
    {
        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        _activeCoroutine = StartCoroutine(AnimateColor());
    }

    public void StopAnimation()
    {
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }

        _text.color = _initialColor;
    }

    private IEnumerator AnimateColor()
    {
        float elapsed = 0f;

        while (true)
        {
            // Interpolate toward target
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = curve.Evaluate(elapsed / duration);
                _text.color = Color.Lerp(_initialColor, targetColor, t);
                yield return null;
            }

            _text.color = targetColor;
            elapsed = 0f;

            // Interpolate back to initial
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = curve.Evaluate(elapsed / duration);
                _text.color = Color.Lerp(targetColor, _initialColor, t);
                yield return null;
            }

            _text.color = _initialColor;
            elapsed = 0f;
        }
    }
}