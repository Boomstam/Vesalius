using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ColorOverlay : MonoBehaviour
{
    [Header("Registration")]
    [SerializeField] private bool registerAsSharedInstance = true;

    [Header("Color Cycling")]
    [SerializeField] private Image overlayImage;
    [SerializeField] private float fadeTime = 2f;
    [SerializeField] private Color[] colors;

    [Header("Heartbeat")]
    [Tooltip("Normalised 0-1 curve driving the lerp between heartbeat start and end color. Recommended keys: (0,0) (0.08,1) (0.13,0.05) (0.22,0.4) (0.35,0) (1,0).")]
    [SerializeField] private AnimationCurve heartbeatCurve;

    [Header("Master Opacity")]
    [SerializeField] private float masterFadeInTime = 2f;
    [SerializeField] private float masterFadeOutTime = 2f;

    private Color startColor;
    private Color lastBaseColor;
    private float startTime;
    private Color previousColor;
    private Color targetColor;
    private int currentColorIndex;
    private bool isFading;
    private float activationTime;

    public float StartDelay { get; set; }

    private bool masterOpacityActive;
    private float masterOpacityValue = 1f;
    private Coroutine masterFadeCoroutine;

    private bool heartbeatActive;
    private Color heartbeatStartColor;
    private Color heartbeatEndColor;
    private float heartbeatBeatTime;
    private float heartbeatCycleStart;

    public bool RegisterAsSharedInstance
    {
        get => registerAsSharedInstance;
        set => registerAsSharedInstance = value;
    }

    private void Awake()
    {
        if (overlayImage == null)
            overlayImage = GetComponentInChildren<Image>();

        if (overlayImage != null)
        {
            startColor = overlayImage.color;
            lastBaseColor = startColor;
        }

        if (heartbeatCurve == null || heartbeatCurve.length == 0)
            heartbeatCurve = BuildDefaultHeartbeatCurve();

        if (registerAsSharedInstance)
            Instances.ColorOverlay = this;
    }

    private void OnDestroy()
    {
        if (registerAsSharedInstance && Instances.ColorOverlay == this)
            Instances.ColorOverlay = null;
    }

    private void OnEnable()
    {
        activationTime = Time.time;
        isFading = false;
        currentColorIndex = 0;
        ApplyColor(startColor);
    }

    private void Update()
    {
        if (overlayImage == null)
            return;

        if (Time.time - activationTime < StartDelay)
            return;

        if (heartbeatActive)
            UpdateHeartbeat();
        else if (isFading)
            DoFade();
        else if (CanCycleColors())
            SetNewTargetColor();
    }

    private bool CanCycleColors()
    {
        return colors != null && colors.Length > 0 && fadeTime > 0f;
    }

    private void DoFade()
    {
        float t = (Time.time - startTime) / fadeTime;
        ApplyColor(Color.Lerp(previousColor, targetColor, t));

        if (t >= 1f)
            isFading = false;
    }

    private void SetNewTargetColor()
    {
        previousColor = lastBaseColor;
        startTime = Time.time;
        currentColorIndex = (currentColorIndex + 1) % colors.Length;
        targetColor = colors[currentColorIndex];
        isFading = true;
    }

    public void StartHeartbeat(Color beatStartColor, Color beatEndColor, float beatTime)
    {
        heartbeatStartColor = beatStartColor;
        heartbeatEndColor = beatEndColor;
        heartbeatBeatTime = Mathf.Max(beatTime, 0.01f);
        heartbeatCycleStart = Time.time;
        heartbeatActive = true;
    }

    public void StartHeartbeat(object[] payload)
    {
        if (payload == null || payload.Length < 3) return;
        StartHeartbeat((Color)payload[0], (Color)payload[1], (float)payload[2]);
    }

    public void StopHeartbeat()
    {
        heartbeatActive = false;
        currentColorIndex = 0;
        isFading = false;
        activationTime = Time.time;
        ApplyColor(startColor);
    }

    private void UpdateHeartbeat()
    {
        float elapsed = (Time.time - heartbeatCycleStart) % heartbeatBeatTime;
        float t = elapsed / heartbeatBeatTime;
        ApplyColor(Color.Lerp(heartbeatStartColor, heartbeatEndColor, heartbeatCurve.Evaluate(t)));
    }

    public void SetMasterOpacityActive(bool active)
    {
        masterOpacityActive = active;

        if (!active)
            StopMasterFade();

        ApplyColor(lastBaseColor);
    }

    public void SetMasterOpacity(float value)
    {
        StopMasterFade();
        masterOpacityValue = Mathf.Clamp01(value);
        ApplyColor(lastBaseColor);
    }

    public void TriggerMasterFadeIn()
    {
        if (!masterOpacityActive) return;
        StopMasterFade();
        masterFadeCoroutine = StartCoroutine(DoMasterFade(true));
    }

    public void TriggerMasterFadeOut()
    {
        if (!masterOpacityActive) return;
        StopMasterFade();
        masterFadeCoroutine = StartCoroutine(DoMasterFade(false));
    }

    private void StopMasterFade()
    {
        if (masterFadeCoroutine == null) return;
        StopCoroutine(masterFadeCoroutine);
        masterFadeCoroutine = null;
    }

    private IEnumerator DoMasterFade(bool fadeIn)
    {
        float duration = Mathf.Max(fadeIn ? masterFadeInTime : masterFadeOutTime, 0.01f);
        float startVal = masterOpacityValue;
        float targetVal = fadeIn ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            masterOpacityValue = Mathf.Lerp(startVal, targetVal, elapsed / duration);
            ApplyColor(lastBaseColor);
            elapsed += Time.deltaTime;
            yield return null;
        }

        masterOpacityValue = targetVal;
        ApplyColor(lastBaseColor);
        masterFadeCoroutine = null;
    }

    private void ApplyColor(Color color)
    {
        lastBaseColor = color;

        if (masterOpacityActive)
            color.a = masterOpacityValue;

        if (overlayImage != null)
            overlayImage.color = color;
    }

    private static AnimationCurve BuildDefaultHeartbeatCurve()
    {
        return new AnimationCurve(
            new Keyframe(0.00f, 0.00f, 0f, 10f),
            new Keyframe(0.08f, 1.00f, 10f, -15f),
            new Keyframe(0.13f, 0.05f, -10f, 3f),
            new Keyframe(0.22f, 0.40f, 3f, -3f),
            new Keyframe(0.35f, 0.00f, -3f, 0f),
            new Keyframe(1.00f, 0.00f, 0f, 0f)
        );
    }
}
