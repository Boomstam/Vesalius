using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageColorAnimator : MonoBehaviour
{
    [Header("Color Settings")]
    [SerializeField] private Color targetColor = Color.white;

    [Header("Timing Settings")]
    [SerializeField] private float duration = 1f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Image _image;
    private Color _initialColor;
    private Coroutine _activeCoroutine;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    public void StartAnimation()
    {
        _initialColor = _image.color;
        
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

        _image.color = _initialColor;
    }

    private IEnumerator AnimateColor()
    {
        float elapsed = 0f;

        while (true)
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = curve.Evaluate(elapsed / duration);
                _image.color = Color.Lerp(_initialColor, targetColor, t);
                yield return null;
            }

            _image.color = targetColor;
            elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = curve.Evaluate(elapsed / duration);
                _image.color = Color.Lerp(targetColor, _initialColor, t);
                yield return null;
            }

            _image.color = _initialColor;
            elapsed = 0f;
        }
    }
}