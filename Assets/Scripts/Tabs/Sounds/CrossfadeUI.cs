using UnityEngine;
using UnityEngine.UI;

public class CrossfadeUI : MonoBehaviour
{
    [SerializeField] private CrossfadePlayer crossfadePlayer;
    [SerializeField] private Slider slider;

    private void Start()
    {
        slider.onValueChanged.AddListener(crossfadePlayer.SetFadeValue);
        crossfadePlayer.Play();
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(crossfadePlayer.SetFadeValue);
    }
}