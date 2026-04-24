#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MonitorSceneUiSetupTool
{
    private const string MonitorScenePath = "Assets/Scenes/Monitor.unity";

    private static readonly Vector2 IntroToggleMin = new(0.525f, 0.79f);
    private static readonly Vector2 IntroToggleMax = new(0.555f, 0.845f);
    private static readonly Vector2 PingPongToggleMin = new(0.645f, 0.79f);
    private static readonly Vector2 PingPongToggleMax = new(0.675f, 0.845f);
    private static readonly Vector2 GenerationToggleMin = new(0.765f, 0.79f);
    private static readonly Vector2 GenerationToggleMax = new(0.795f, 0.845f);
    private static readonly Vector2 HeartToggleMin = new(0.885f, 0.79f);
    private static readonly Vector2 HeartToggleMax = new(0.915f, 0.845f);
    private static readonly Vector2 GroupColorToggleMin = new(0.645f, 0.69f);
    private static readonly Vector2 GroupColorToggleMax = new(0.675f, 0.745f);

    private static readonly Vector2 IntroLabelMin = new(0.47f, 0.855f);
    private static readonly Vector2 IntroLabelMax = new(0.61f, 0.915f);
    private static readonly Vector2 PingPongLabelMin = new(0.59f, 0.855f);
    private static readonly Vector2 PingPongLabelMax = new(0.73f, 0.915f);
    private static readonly Vector2 GenerationLabelMin = new(0.71f, 0.855f);
    private static readonly Vector2 GenerationLabelMax = new(0.87f, 0.915f);
    private static readonly Vector2 HeartLabelMin = new(0.83f, 0.855f);
    private static readonly Vector2 HeartLabelMax = new(0.97f, 0.915f);
    private static readonly Vector2 GroupColorLabelMin = new(0.56f, 0.755f);
    private static readonly Vector2 GroupColorLabelMax = new(0.76f, 0.815f);

    private static readonly Vector2 GroupMessageButtonMin = new(0.067307696f, 0.15441176f);
    private static readonly Vector2 GroupMessageButtonMax = new(0.9326923f, 0.2367647f);
    private static readonly Vector2 ResetDeckButtonMin = new(0.067307696f, 0.044117648f);
    private static readonly Vector2 ResetDeckButtonMax = new(0.47115386f, 0.13235295f);
    private static readonly Vector2 HardCutButtonMin = new(0.52884614f, 0.044117648f);
    private static readonly Vector2 HardCutButtonMax = new(0.9326923f, 0.13235295f);

    [MenuItem("Tools/Vesalius/Configure Monitor Scene UI")]
    public static void ConfigureMonitorSceneUi()
    {
        EditorSceneManager.OpenScene(MonitorScenePath, OpenSceneMode.Single);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[MonitorSceneUiSetupTool] Monitor scene has no Canvas.");
            return;
        }

        ConfigureMonitorAudioControls(canvas.transform);
        ConfigureMessagePanel();

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[MonitorSceneUiSetupTool] Monitor scene UI configured.");
    }

    private static void ConfigureMonitorAudioControls(Transform canvasTransform)
    {
        Toggle introToggle = ResolveToggle("Intro Toggle", "Organs Of Nutrition Toggle");
        Toggle generationToggle = ResolveToggle("Organs Of Generation Toggle");
        Toggle heartToggle = ResolveToggle("Heart Toggle");
        Toggle participationToggle = ResolveToggle("Participation");
        Toggle completeAnatomyToggle = ResolveToggle("Complete Anatomy");

        Text introLabel = ResolveLabel("Intro Label", "Organs Of Nutrition Label");
        Text generationLabel = ResolveLabel("Organs Of Generation Label");
        Text heartLabel = ResolveLabel("Heart Label");

        if (introToggle == null || introLabel == null)
        {
            Debug.LogError("[MonitorSceneUiSetupTool] Intro toggle template is missing from the monitor scene.");
            return;
        }

        Rename(introToggle.gameObject, "Intro Toggle");
        Rename(introLabel.gameObject, "Intro Label");

        Toggle pingPongToggle = EnsureToggleClone(canvasTransform, "Ping Pong Toggle", introToggle);
        Text pingPongLabel = EnsureLabelClone(canvasTransform, "Ping Pong Label", introLabel);
        Toggle groupColorToggle = EnsureToggleClone(canvasTransform, "Group Color Toggle", introToggle);
        Text groupColorLabel = EnsureLabelClone(canvasTransform, "Group Color Label", introLabel);
        participationToggle = participationToggle != null
            ? participationToggle
            : EnsureToggleClone(canvasTransform, "Participation", completeAnatomyToggle != null ? completeAnatomyToggle : introToggle);

        ApplyRect(introToggle.transform as RectTransform, IntroToggleMin, IntroToggleMax);
        ApplyRect(pingPongToggle.transform as RectTransform, PingPongToggleMin, PingPongToggleMax);
        ApplyRect(generationToggle != null ? generationToggle.transform as RectTransform : null, GenerationToggleMin, GenerationToggleMax);
        ApplyRect(heartToggle != null ? heartToggle.transform as RectTransform : null, HeartToggleMin, HeartToggleMax);
        ApplyRect(groupColorToggle.transform as RectTransform, GroupColorToggleMin, GroupColorToggleMax);
        PositionAbove(participationToggle, completeAnatomyToggle, 72f);

        ApplyRect(introLabel.transform as RectTransform, IntroLabelMin, IntroLabelMax);
        ApplyRect(pingPongLabel.transform as RectTransform, PingPongLabelMin, PingPongLabelMax);
        ApplyRect(generationLabel != null ? generationLabel.transform as RectTransform : null, GenerationLabelMin, GenerationLabelMax);
        ApplyRect(heartLabel != null ? heartLabel.transform as RectTransform : null, HeartLabelMin, HeartLabelMax);
        ApplyRect(groupColorLabel.transform as RectTransform, GroupColorLabelMin, GroupColorLabelMax);

        introLabel.text = "Intro";
        pingPongLabel.text = "Ping Pong";
        if (generationLabel != null) generationLabel.text = "Organs of Generation";
        if (heartLabel != null) heartLabel.text = "Heart";
        groupColorLabel.text = "Go To Your Color";
        SetToggleText(participationToggle, "Participation");

        MonitorUI monitorUi = Object.FindFirstObjectByType<MonitorUI>();
        if (monitorUi == null)
        {
            Debug.LogError("[MonitorSceneUiSetupTool] MonitorUI was not found in the monitor scene.");
            return;
        }

        SerializedObject serializedUi = new(monitorUi);
        SetObjectReference(serializedUi, "introToggle", introToggle);
        SetObjectReference(serializedUi, "pingPongToggle", pingPongToggle);
        SetObjectReference(serializedUi, "organsOfGenerationToggle", generationToggle);
        SetObjectReference(serializedUi, "heartToggle", heartToggle);
        SetObjectReference(serializedUi, "groupColorToggle", groupColorToggle);
        SetObjectReference(serializedUi, "participationToggle", participationToggle);
        SetObjectReference(serializedUi, "completeAnatomyToggle", completeAnatomyToggle);
        serializedUi.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureMessagePanel()
    {
        MonitorMessagePanel panel = Object.FindFirstObjectByType<MonitorMessagePanel>();
        if (panel == null)
        {
            Debug.LogError("[MonitorSceneUiSetupTool] MonitorMessagePanel was not found in the monitor scene.");
            return;
        }

        SerializedObject serializedPanel = new(panel);
        Button resetDeckButton = serializedPanel.FindProperty("_resetDeckButton")?.objectReferenceValue as Button;
        Button hardCutButton = serializedPanel.FindProperty("_hardCutButton")?.objectReferenceValue as Button;

        if (resetDeckButton == null || hardCutButton == null)
        {
            Debug.LogError("[MonitorSceneUiSetupTool] Message control button templates are missing.");
            return;
        }

        Button groupMessageButton = ResolveButton("Go To Your Color Button", "Group Message Button");
        if (groupMessageButton == null)
            groupMessageButton = CloneButton(resetDeckButton, "Go To Your Color Button");

        ApplyRect(groupMessageButton.transform as RectTransform, GroupMessageButtonMin, GroupMessageButtonMax);
        ApplyRect(resetDeckButton.transform as RectTransform, ResetDeckButtonMin, ResetDeckButtonMax);
        ApplyRect(hardCutButton.transform as RectTransform, HardCutButtonMin, HardCutButtonMax);

        SetButtonText(groupMessageButton, "Go To Your Color");
        groupMessageButton.transform.SetSiblingIndex(resetDeckButton.transform.GetSiblingIndex());
        resetDeckButton.transform.SetSiblingIndex(groupMessageButton.transform.GetSiblingIndex() + 1);
        hardCutButton.transform.SetSiblingIndex(resetDeckButton.transform.GetSiblingIndex() + 1);

        SetObjectReference(serializedPanel, "_groupMessageButton", groupMessageButton);
        serializedPanel.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Toggle EnsureToggleClone(Transform parent, string name, Toggle template)
    {
        Toggle existing = ResolveToggle(name);
        if (existing != null)
            return existing;

        GameObject clone = Object.Instantiate(template.gameObject, parent, false);
        clone.name = name;
        Toggle toggle = clone.GetComponent<Toggle>();
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(false);
            toggle.onValueChanged.RemoveAllListeners();
        }

        return toggle;
    }

    private static Text EnsureLabelClone(Transform parent, string name, Text template)
    {
        Text existing = ResolveLabel(name);
        if (existing != null)
            return existing;

        GameObject clone = Object.Instantiate(template.gameObject, parent, false);
        clone.name = name;
        return clone.GetComponent<Text>();
    }

    private static Button CloneButton(Button template, string name)
    {
        GameObject clone = Object.Instantiate(template.gameObject, template.transform.parent, false);
        clone.name = name;
        return clone.GetComponent<Button>();
    }

    private static Toggle ResolveToggle(params string[] names)
    {
        foreach (string name in names)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null && existing.TryGetComponent(out Toggle toggle))
                return toggle;
        }

        return null;
    }

    private static Text ResolveLabel(params string[] names)
    {
        foreach (string name in names)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null && existing.TryGetComponent(out Text label))
                return label;
        }

        return null;
    }

    private static Button ResolveButton(params string[] names)
    {
        foreach (string name in names)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null && existing.TryGetComponent(out Button button))
                return button;
        }

        return null;
    }

    private static void ApplyRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (rect == null)
            return;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void PositionAbove(Toggle toggle, Toggle reference, float yOffset)
    {
        if (toggle == null || reference == null)
            return;

        RectTransform toggleRect = toggle.transform as RectTransform;
        RectTransform referenceRect = reference.transform as RectTransform;
        if (toggleRect == null || referenceRect == null)
            return;

        toggleRect.anchorMin = referenceRect.anchorMin;
        toggleRect.anchorMax = referenceRect.anchorMax;
        toggleRect.pivot = referenceRect.pivot;
        toggleRect.sizeDelta = referenceRect.sizeDelta;
        toggleRect.anchoredPosition = referenceRect.anchoredPosition + new Vector2(0f, yOffset);
        toggleRect.localScale = Vector3.one;
    }

    private static void SetButtonText(Button button, string text)
    {
        if (button == null)
            return;

        TMP_Text tmpLabel = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpLabel != null)
            tmpLabel.text = text;

        Text label = button.GetComponentInChildren<Text>(true);
        if (label != null)
            label.text = text;
    }

    private static void SetToggleText(Toggle toggle, string text)
    {
        if (toggle == null)
            return;

        TMP_Text tmpLabel = toggle.GetComponentInChildren<TMP_Text>(true);
        if (tmpLabel != null)
            tmpLabel.text = text;

        Text label = toggle.GetComponentInChildren<Text>(true);
        if (label != null)
            label.text = text;
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void Rename(GameObject gameObject, string name)
    {
        if (gameObject != null)
            gameObject.name = name;
    }
}
#endif
