#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ColorOverlaySetupTool
{
    private const string ClientScenePath = "Assets/Scenes/Client.unity";
    private const string MonitorScenePath = "Assets/Scenes/Monitor.unity";
    private const string MarblePath = "Assets/Graphics/Marble/Marble.jpg";

    [MenuItem("Tools/UI/Configure Color Overlay")]
    public static void ConfigureColorOverlay()
    {
        ConfigureColorOverlayNow();
    }

    [MenuItem("Tools/UI/Configure Color Overlay Client Scene")]
    public static void ConfigureColorOverlayClientScene()
    {
        ConfigureClientScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[ColorOverlaySetupTool] Client color overlay setup complete.");
    }

    [MenuItem("Tools/UI/Configure Color Overlay Monitor Scene")]
    public static void ConfigureColorOverlayMonitorScene()
    {
        ConfigureMonitorScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[ColorOverlaySetupTool] Monitor color overlay setup complete.");
    }

    private static void ConfigureColorOverlayNow()
    {
        try
        {
            ConfigureClientScene();
            ConfigureMonitorScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[ColorOverlaySetupTool] Color overlay setup complete.");
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void ConfigureClientScene()
    {
        EditorSceneManager.OpenScene(ClientScenePath, OpenSceneMode.Single);

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ColorOverlaySetupTool] Client scene has no Canvas.");
            return;
        }

        Transform stack = canvas.transform.Find("Color Overlay Stack");
        if (stack == null)
        {
            GameObject stackObject = new GameObject("Color Overlay Stack", typeof(RectTransform));
            stack = stackObject.transform;
            stack.SetParent(canvas.transform, false);
            InsertBeforeMessageOverlay(stack);
        }

        Stretch((RectTransform)stack);
        DestroyComponentIfPresent<Image>(stack.gameObject);

        Image blackBackground = EnsureImage(stack, "Black Background", Color.black);
        blackBackground.raycastTarget = false;
        blackBackground.sprite = null;
        blackBackground.type = Image.Type.Simple;
        Stretch((RectTransform)blackBackground.transform);

        Image colorLayer = EnsureImage(stack, "Color Layer", Color.white);
        colorLayer.raycastTarget = false;
        colorLayer.sprite = null;
        colorLayer.type = Image.Type.Simple;
        Stretch((RectTransform)colorLayer.transform);

        Image marbleLayer = EnsureImage(stack, "Marble Layer", new Color(1f, 1f, 1f, 0.35f));
        marbleLayer.raycastTarget = false;
        marbleLayer.sprite = LoadMarbleSprite();
        marbleLayer.type = Image.Type.Simple;
        marbleLayer.preserveAspect = false;
        Stretch((RectTransform)marbleLayer.transform);

        blackBackground.transform.SetSiblingIndex(0);
        colorLayer.transform.SetSiblingIndex(1);
        marbleLayer.transform.SetSiblingIndex(2);

        System.Type overlayType = System.Type.GetType("ColorOverlay, Assembly-CSharp");
        if (overlayType == null)
        {
            Debug.LogError("[ColorOverlaySetupTool] ColorOverlay type was not found.");
            return;
        }

        Component overlay = stack.GetComponent(overlayType);
        if (overlay == null)
            overlay = stack.gameObject.AddComponent(overlayType);

        SerializedObject serializedOverlay = new SerializedObject(overlay);
        serializedOverlay.FindProperty("overlayImage").objectReferenceValue = colorLayer;
        serializedOverlay.FindProperty("fadeTime").floatValue = 2f;
        serializedOverlay.FindProperty("masterFadeInTime").floatValue = 2f;
        serializedOverlay.FindProperty("masterFadeOutTime").floatValue = 3f;
        serializedOverlay.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
    }

    private static void ConfigureMonitorScene()
    {
        EditorSceneManager.OpenScene(MonitorScenePath, OpenSceneMode.Single);

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ColorOverlaySetupTool] Monitor scene has no Canvas.");
            return;
        }

        RectTransform panel = EnsureRectTransform(canvas.transform, "Color Overlay Panel");
        panel.SetAsLastSibling();
        panel.anchorMin = new Vector2(0.72f, 0.08f);
        panel.anchorMax = new Vector2(0.98f, 0.52f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;

        Image background = panel.GetComponent<Image>();
        if (background == null)
            background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.04f, 0.04f, 0.04f, 0.82f);

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text title = EnsureText(panel, "Title", "COLOR OVERLAY", 20f);
        SetLayoutHeight(title.gameObject, 32f);

        Toggle masterToggle = EnsureToggle(panel, "Master Opacity Toggle", "Master Opacity", false);
        Slider masterSlider = EnsureSlider(panel, "Master Opacity Slider", 0f, 1f, 1f);
        Button fadeIn = EnsureButton(panel, "Fade In Button", "Fade In");
        Button fadeOut = EnsureButton(panel, "Fade Out Button", "Fade Out");
        Button cutToBlack = EnsureButton(panel, "Cut To Black Button", "Cut to Black");
        Toggle heartbeatToggle = EnsureToggle(panel, "Heartbeat Toggle", "Heartbeat", false);

        MonitorColorOverlayUI ui = panel.GetComponent<MonitorColorOverlayUI>();
        if (ui == null)
            ui = panel.gameObject.AddComponent<MonitorColorOverlayUI>();

        SerializedObject serializedUi = new SerializedObject(ui);
        serializedUi.FindProperty("masterOpacityToggle").objectReferenceValue = masterToggle;
        serializedUi.FindProperty("masterOpacitySlider").objectReferenceValue = masterSlider;
        serializedUi.FindProperty("fadeInButton").objectReferenceValue = fadeIn;
        serializedUi.FindProperty("fadeOutButton").objectReferenceValue = fadeOut;
        serializedUi.FindProperty("cutToBlackButton").objectReferenceValue = cutToBlack;
        serializedUi.FindProperty("heartbeatToggle").objectReferenceValue = heartbeatToggle;
        serializedUi.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
    }

    private static void InsertBeforeMessageOverlay(Transform stack)
    {
        Transform parent = stack.parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).name.ToLowerInvariant().Contains("message"))
            {
                stack.SetSiblingIndex(i);
                return;
            }
        }

        stack.SetAsLastSibling();
    }

    private static Image EnsureImage(Transform parent, string name, Color color)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            GameObject childObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child = childObject.transform;
            child.SetParent(parent, false);
        }

        Image image = child.GetComponent<Image>();
        if (image == null)
            image = child.gameObject.AddComponent<Image>();

        image.color = color;
        return image;
    }

    private static RectTransform EnsureRectTransform(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            GameObject childObject = new GameObject(name, typeof(RectTransform));
            child = childObject.transform;
            child.SetParent(parent, false);
        }

        return (RectTransform)child;
    }

    private static TMP_Text EnsureText(Transform parent, string name, string text, float fontSize)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            GameObject childObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            child = childObject.transform;
            child.SetParent(parent, false);
        }

        TMP_Text label = child.GetComponent<TMP_Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        return label;
    }

    private static Toggle EnsureToggle(Transform parent, string name, string label, bool isOn)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            DefaultControls.Resources resources = new DefaultControls.Resources();
            GameObject toggleObject = DefaultControls.CreateToggle(resources);
            toggleObject.name = name;
            child = toggleObject.transform;
            child.SetParent(parent, false);
        }

        Toggle toggle = child.GetComponent<Toggle>();
        toggle.SetIsOnWithoutNotify(isOn);
        SetChildText(child, label);
        SetLayoutHeight(child.gameObject, 32f);
        return toggle;
    }

    private static Slider EnsureSlider(Transform parent, string name, float minValue, float maxValue, float value)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            DefaultControls.Resources resources = new DefaultControls.Resources();
            GameObject sliderObject = DefaultControls.CreateSlider(resources);
            sliderObject.name = name;
            child = sliderObject.transform;
            child.SetParent(parent, false);
        }

        Slider slider = child.GetComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(value);
        SetLayoutHeight(child.gameObject, 28f);
        return slider;
    }

    private static Button EnsureButton(Transform parent, string name, string label)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            DefaultControls.Resources resources = new DefaultControls.Resources();
            GameObject buttonObject = DefaultControls.CreateButton(resources);
            buttonObject.name = name;
            child = buttonObject.transform;
            child.SetParent(parent, false);
        }

        Button button = child.GetComponent<Button>();
        SetChildText(child, label);
        SetLayoutHeight(child.gameObject, 34f);
        return button;
    }

    private static void SetChildText(Transform root, string text)
    {
        TMP_Text tmpText = root.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = text;
            tmpText.fontSize = 16f;
            tmpText.color = Color.white;
            return;
        }

        Text legacyText = root.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            legacyText.text = text;
            legacyText.fontSize = 16;
            legacyText.color = Color.white;
        }
    }

    private static void SetLayoutHeight(GameObject gameObject, float height)
    {
        LayoutElement layout = gameObject.GetComponent<LayoutElement>();
        if (layout == null)
            layout = gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static Sprite LoadMarbleSprite()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MarblePath);
        if (sprite != null)
            return sprite;

        TextureImporter importer = AssetImporter.GetAtPath(MarblePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[ColorOverlaySetupTool] Could not import Marble sprite at {MarblePath}.");
            return null;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(MarblePath);
    }

    private static void DestroyComponentIfPresent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component != null)
            Object.DestroyImmediate(component);
    }
}
#endif
