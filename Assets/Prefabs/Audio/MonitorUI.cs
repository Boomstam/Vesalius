using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Monitor-side UI. Communicates exclusively through NetworkedMonitor —
/// never touches AudioManager directly (AudioManager lives on the client).
///
/// Required UI elements (wire in Inspector):
///   Master section:  masterVolumeSlider, fadeInButton, fadeOutButton,
///                    muteButton, resetButton
///   Audio toggles:   organsOfNutritionToggle, organsOfGenerationToggle, heartToggle
/// </summary>
public class MonitorUI : MonoBehaviour
{
    [Header("Master Volume")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Button fadeInButton;
    [SerializeField] private Button fadeOutButton;
    [SerializeField] private Button muteButton;
    [SerializeField] private Button resetButton;

    [Header("Audio Mode Toggles")]
    [SerializeField] private Toggle organsOfNutritionToggle;
    [SerializeField] private Toggle organsOfGenerationToggle;
    [SerializeField] private Toggle heartToggle;

    // ── Unity Lifecycle ────────────────────────────────────────────────────────

    private void Start()
    {
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        fadeInButton.onClick.AddListener(OnFadeInClicked);
        fadeOutButton.onClick.AddListener(OnFadeOutClicked);
        muteButton.onClick.AddListener(OnMuteClicked);
        resetButton.onClick.AddListener(OnResetClicked);

        organsOfNutritionToggle.onValueChanged.AddListener(OnOrgansOfNutritionToggled);
        organsOfGenerationToggle.onValueChanged.AddListener(OnOrgansOfGenerationToggled);
        heartToggle.onValueChanged.AddListener(OnHeartToggled);
    }

    // ── Master Volume ──────────────────────────────────────────────────────────

    private void OnMasterVolumeChanged(float value)
    {
        Instances.NetworkedMonitor.SetMasterVolume(value);
    }

    private void OnFadeInClicked()  => Instances.NetworkedMonitor.TriggerMasterFadeIn();
    private void OnFadeOutClicked() => Instances.NetworkedMonitor.TriggerMasterFadeOut();
    private void OnMuteClicked()    => Instances.NetworkedMonitor.TriggerMasterMute();
    private void OnResetClicked()   => Instances.NetworkedMonitor.TriggerMasterReset();

    // ── Audio Mode Toggles ─────────────────────────────────────────────────────

    private void OnOrgansOfNutritionToggled(bool value)
    {
        Instances.NetworkedMonitor.SetShouldPlayOrgansOfNutrition(value);
    }

    private void OnOrgansOfGenerationToggled(bool value)
    {
        Instances.NetworkedMonitor.SetShouldPlayOrgansOfGeneration(value);
    }

    private void OnHeartToggled(bool value)
    {
        Instances.NetworkedMonitor.SetShouldPlayHeart(value);
    }
}
