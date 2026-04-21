using UnityEngine;

public class VibrateTest : MonoBehaviour
{
    public void Vibrate()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}
