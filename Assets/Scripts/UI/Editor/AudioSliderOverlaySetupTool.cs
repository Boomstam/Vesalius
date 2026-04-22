using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Reconfigures the hidden global slider canvas overlay in the Client scene.
/// It reuses the existing tutorial slider visuals, creates a centered single
/// slider variant, and wires everything to GlobalAudioSliderOverlay.
/// </summary>
public static class AudioSliderOverlaySetupTool
{
    private const string ClientScenePath = "Assets/Scenes/Client.unity";
    private const string SlidersPath = "MainCanvas/Content/Sliders";

    [MenuItem("Tools/UI/Configure Global Audio Sliders")]
    public static void ConfigureGlobalAudioSliders()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ClientScenePath)
        {
            Debug.LogError($"[AudioSliderOverlaySetupTool] Open '{ClientScenePath}' before running this tool.");
            return;
        }

        GameObject slidersRoot = GameObject.Find(SlidersPath);
        if (slidersRoot == null)
        {
            Debug.LogError($"[AudioSliderOverlaySetupTool] Could not find '{SlidersPath}'.");
            return;
        }

        RectTransform rootRect = slidersRoot.GetComponent<RectTransform>();
        if (rootRect == null)
        {
            Debug.LogError("[AudioSliderOverlaySetupTool] Sliders root is missing a RectTransform.");
            return;
        }

        Canvas.ForceUpdateCanvases();
        slidersRoot.SetActive(true);
        CleanupTopLevelLabels(slidersRoot.transform);

        List<Slider> topLevelSliders = slidersRoot.transform
            .Cast<Transform>()
            .Select(child => child.GetComponent<Slider>())
            .Where(slider => slider != null)
            .ToList();

        if (topLevelSliders.Count < 2)
        {
            Debug.LogError("[AudioSliderOverlaySetupTool] Expected at least two top-level sliders under the global Sliders root.");
            return;
        }

        Slider dualLeftSlider = topLevelSliders.OrderBy(slider => slider.transform.position.x).First();
        Slider dualRightSlider = topLevelSliders.OrderBy(slider => slider.transform.position.x).Last();

        ConfigureSliderGameObject(dualLeftSlider.gameObject, "Global Dual Slider Left");
        ConfigureSliderGameObject(dualRightSlider.gameObject, "Global Dual Slider Right");

        Slider singleSlider = EnsureSingleSlider(dualLeftSlider, slidersRoot.transform);
        ConfigureSliderGameObject(singleSlider.gameObject, "Global Single Slider");

        TMP_Text dualLeftTopLabel = EnsureTopLevelLabel(slidersRoot.transform, "Global Dual Left Max Label");
        TMP_Text dualLeftBottomLabel = EnsureTopLevelLabel(slidersRoot.transform, "Global Dual Left Min Label");
        TMP_Text dualRightTopLabel = EnsureTopLevelLabel(slidersRoot.transform, "Global Dual Right Max Label");
        TMP_Text dualRightBottomLabel = EnsureTopLevelLabel(slidersRoot.transform, "Global Dual Right Min Label");
        TMP_Text singleTopLabel = EnsureTopLevelLabel(slidersRoot.transform, "Global Single Max Label");
        TMP_Text singleBottomLabel = EnsureTopLevelLabel(slidersRoot.transform, "Global Single Min Label");

        dualLeftTopLabel.text = "HIGH";
        dualLeftBottomLabel.text = "LOW";
        dualRightTopLabel.text = "LONG";
        dualRightBottomLabel.text = "SHORT";
        singleTopLabel.text = "HIGH";
        singleBottomLabel.text = "LOW";

        SetRectFromNormalizedBounds((RectTransform)dualLeftSlider.transform, new Vector2(0.15f, 0.23f), new Vector2(0.45f, 0.80f));
        SetRectFromNormalizedBounds((RectTransform)dualRightSlider.transform, new Vector2(0.59f, 0.23f), new Vector2(0.88f, 0.80f));
        SetRectFromNormalizedBounds((RectTransform)singleSlider.transform, new Vector2(0.33f, 0.22f), new Vector2(0.67f, 0.81f));

        SetRectFromNormalizedBounds((RectTransform)dualLeftTopLabel.transform, new Vector2(0.15f, 0.84f), new Vector2(0.45f, 0.91f));
        SetRectFromNormalizedBounds((RectTransform)dualLeftBottomLabel.transform, new Vector2(0.15f, 0.11f), new Vector2(0.45f, 0.18f));
        SetRectFromNormalizedBounds((RectTransform)dualRightTopLabel.transform, new Vector2(0.59f, 0.84f), new Vector2(0.88f, 0.91f));
        SetRectFromNormalizedBounds((RectTransform)dualRightBottomLabel.transform, new Vector2(0.59f, 0.11f), new Vector2(0.88f, 0.18f));
        SetRectFromNormalizedBounds((RectTransform)singleTopLabel.transform, new Vector2(0.33f, 0.84f), new Vector2(0.67f, 0.91f));
        SetRectFromNormalizedBounds((RectTransform)singleBottomLabel.transform, new Vector2(0.33f, 0.11f), new Vector2(0.67f, 0.18f));

        AnchorToCurrentRects(
            dualLeftSlider.gameObject,
            dualRightSlider.gameObject,
            singleSlider.gameObject,
            dualLeftTopLabel.gameObject,
            dualLeftBottomLabel.gameObject,
            dualRightTopLabel.gameObject,
            dualRightBottomLabel.gameObject,
            singleTopLabel.gameObject,
            singleBottomLabel.gameObject);

        GlobalAudioSliderOverlay overlay = AddComponentIfMissing<GlobalAudioSliderOverlay>(slidersRoot);
        AssignOverlayReferences(
            overlay,
            dualLeftSlider,
            dualRightSlider,
            singleSlider,
            dualLeftBottomLabel,
            dualLeftTopLabel,
            dualRightBottomLabel,
            dualRightTopLabel,
            singleBottomLabel,
            singleTopLabel);

        SetOverlayDefaults(
            dualLeftSlider.gameObject,
            dualRightSlider.gameObject,
            singleSlider.gameObject,
            dualLeftTopLabel.gameObject,
            dualLeftBottomLabel.gameObject,
            dualRightTopLabel.gameObject,
            dualRightBottomLabel.gameObject,
            singleTopLabel.gameObject,
            singleBottomLabel.gameObject);

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("[AudioSliderOverlaySetupTool] Global audio sliders configured.");
    }

    private static void ConfigureSliderGameObject(GameObject sliderObject, string objectName)
    {
        Undo.RecordObject(sliderObject, "Configure Global Audio Slider");
        sliderObject.name = objectName;
        DestroyComponentIfPresent<CrossfadePlayer>(sliderObject);
        sliderObject.SetActive(false);
    }

    private static Slider EnsureSingleSlider(Slider template, Transform parent)
    {
        Transform existing = parent.Find("Global Single Slider");
        if (existing != null && existing.TryGetComponent(out Slider existingSlider))
            return existingSlider;

        Slider duplicate = Object.Instantiate(template, parent);
        duplicate.name = "Global Single Slider";
        Undo.RegisterCreatedObjectUndo(duplicate.gameObject, "Create Global Single Slider");
        return duplicate;
    }

    private static TMP_Text EnsureTopLevelLabel(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null && existing.TryGetComponent(out TMP_Text existingText))
            return existingText;

        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(labelObject, "Create Global Slider Label");

        labelObject.transform.SetParent(parent, false);

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.text = objectName;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 36f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = 36f;
        label.color = Color.white;

        return label;
    }

    private static void SetRectFromNormalizedBounds(RectTransform rectTransform, Vector2 normalizedMin, Vector2 normalizedMax)
    {
        RectTransform parentRect = (RectTransform)rectTransform.parent;
        Rect parent = parentRect.rect;

        Vector2 center = new Vector2(
            Mathf.Lerp(parent.xMin, parent.xMax, (normalizedMin.x + normalizedMax.x) * 0.5f),
            Mathf.Lerp(parent.yMin, parent.yMax, (normalizedMin.y + normalizedMax.y) * 0.5f));

        Vector2 size = new Vector2(
            parent.width * (normalizedMax.x - normalizedMin.x),
            parent.height * (normalizedMax.y - normalizedMin.y));

        Undo.RecordObject(rectTransform, "Position Global Audio Slider Rect");
        rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = center;
        rectTransform.sizeDelta = size;
    }

    private static void AnchorToCurrentRects(params GameObject[] objectsToAnchor)
    {
        Object[] previousSelection = Selection.objects;
        Selection.objects = objectsToAnchor;
        FitToParent.AnchorToCurrentRect();
        Selection.objects = previousSelection;
    }

    private static void AssignOverlayReferences(
        GlobalAudioSliderOverlay overlay,
        Slider dualLeftSlider,
        Slider dualRightSlider,
        Slider singleSlider,
        TMP_Text dualLeftBottomLabel,
        TMP_Text dualLeftTopLabel,
        TMP_Text dualRightBottomLabel,
        TMP_Text dualRightTopLabel,
        TMP_Text singleBottomLabel,
        TMP_Text singleTopLabel)
    {
        SerializedObject serializedOverlay = new SerializedObject(overlay);
        serializedOverlay.FindProperty("dualPrimarySlider").objectReferenceValue = dualLeftSlider;
        serializedOverlay.FindProperty("dualSecondarySlider").objectReferenceValue = dualRightSlider;
        serializedOverlay.FindProperty("singleSlider").objectReferenceValue = singleSlider;
        serializedOverlay.FindProperty("dualPrimaryMinLabel").objectReferenceValue = dualLeftBottomLabel;
        serializedOverlay.FindProperty("dualPrimaryMaxLabel").objectReferenceValue = dualLeftTopLabel;
        serializedOverlay.FindProperty("dualSecondaryMinLabel").objectReferenceValue = dualRightBottomLabel;
        serializedOverlay.FindProperty("dualSecondaryMaxLabel").objectReferenceValue = dualRightTopLabel;
        serializedOverlay.FindProperty("singleMinLabel").objectReferenceValue = singleBottomLabel;
        serializedOverlay.FindProperty("singleMaxLabel").objectReferenceValue = singleTopLabel;
        serializedOverlay.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(overlay);
    }

    private static void SetOverlayDefaults(params GameObject[] overlayObjects)
    {
        foreach (GameObject overlayObject in overlayObjects)
            overlayObject.SetActive(false);
    }

    private static void DestroyComponentIfPresent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component != null)
            Undo.DestroyObjectImmediate(component);
    }

    private static void CleanupTopLevelLabels(Transform parent)
    {
        List<GameObject> topLevelLabels = parent.Cast<Transform>()
            .Where(child => child.GetComponent<TMP_Text>() != null)
            .Select(child => child.gameObject)
            .ToList();

        foreach (GameObject labelObject in topLevelLabels)
            Undo.DestroyObjectImmediate(labelObject);
    }

    private static T AddComponentIfMissing<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }
}
