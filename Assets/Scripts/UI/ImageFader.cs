using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ImageFader : MonoBehaviour
{
    public Image[] images;
    public Sprite[] alternateImages;

    public bool alternateMode;

    public float fadeVal;

    private Sprite[] _originalSprites;
    private Image[]  _activeImages;
    private bool _lastAlternateMode;
    private float _lastFadeVal;

    private int CurrentNumImages => _activeImages?.Length ?? 0;

    private void Awake()
    {
        if (images != null)
            _originalSprites = images.Select(img => img.sprite).ToArray();
    }

    private void Start()
    {
        InitImageArray(ref images);
        _activeImages = images;
        _lastAlternateMode = alternateMode;
    }

    private void Update()
    {
        bool modeChanged = _lastAlternateMode != alternateMode;

        if (modeChanged)
        {
            _lastAlternateMode = alternateMode;
            SwapSprites(alternateMode);
        }

        if (modeChanged || !Mathf.Approximately(_lastFadeVal, fadeVal))
        {
            _lastFadeVal = fadeVal;
            SetFadeVal(fadeVal);
        }
    }

    public void SetFadeVal(float fadeVal)
    {
        if (CurrentNumImages == 0)
        {
            Debug.LogWarning("No images in active set, can't fade");
            return;
        }
        if (CurrentNumImages == 1)
        {
            Debug.LogWarning("Only 1 image in active set, can't fade");
            return;
        }

        float percentagePerSource = 1f / (float)(CurrentNumImages - 1);

        int startSample = Mathf.FloorToInt(fadeVal / percentagePerSource);
        float remainder = fadeVal - (percentagePerSource * startSample);
        float remainderPercentage = remainder / percentagePerSource;

        for (int i = 0; i < CurrentNumImages; i++)
        {
            Image image = _activeImages[i];

            float alpha = 0;

            if (i == startSample)
                alpha = 1 - remainderPercentage;
            if (i == startSample + 1)
                alpha = remainderPercentage;

            Color c = image.color;
            image.color = new Color(c.r, c.g, c.b, alpha);
        }
    }

    private void SwapSprites(bool useAlternate)
    {
        if (_originalSprites == null)
        {
            Debug.LogWarning("ImageFader: cache was empty on SwapSprites, caching now.");
            _originalSprites = images.Select(img => img.sprite).ToArray();
        }

        Sprite[] source = useAlternate ? alternateImages : _originalSprites;

        if (source == null)
        {
            Debug.LogWarning("ImageFader: no sprites to swap to.");
            return;
        }

        for (int i = 0; i < images.Length; i++)
        {
            if (i >= source.Length)
            {
                // This image is outside the active set — hide it
                Color c = images[i].color;
                images[i].color = new Color(c.r, c.g, c.b, 0f);
                break;
            }

            images[i].sprite = source[i];
        }

        _activeImages = useAlternate ? images.Take(source.Length).ToArray() : images;
    }

    private void InitImageArray(ref Image[] imageArray)
    {
        if (imageArray == null || imageArray.Length < 3)
            return;

        if (imageArray[2].sprite == null)
        {
            imageArray[2].gameObject.SetActive(false);
            imageArray = imageArray.Take(2).ToArray();
        }
    }
}