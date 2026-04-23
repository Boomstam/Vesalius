#if UNITY_EDITOR
using FishNet.Object;
using FishNet.Observing;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MessageSystemSceneSetupTool
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const string ClientScenePath = "Assets/Scenes/Client.unity";
    private const string MonitorScenePath = "Assets/Scenes/Monitor.unity";

    [MenuItem("Tools/Vesalius/Setup Message System")]
    public static void SetupMessageSystem()
    {
        SetupMainScene();
        SetupClientScene();
        SetupMonitorScene();

        AssetDatabase.SaveAssets();
        Debug.Log("[MessageSystemSceneSetupTool] Message system setup complete.");
    }

    private static void SetupMainScene()
    {
        OpenScene(MainScenePath);

        GameObject go = GameObject.Find("NetworkedMessageSystem");
        if (go == null)
            go = new GameObject("NetworkedMessageSystem");

        EnsureComponent<NetworkObject>(go);
        EnsureComponent<NetworkedMessageSystem>(go);
        EnsureComponent<NetworkObserver>(go);

        SaveActiveScene();
    }

    private static void SetupClientScene()
    {
        OpenScene(ClientScenePath);

        GameObject identityHost = GameObject.Find("AudioManager") ?? GameObject.Find("MainCanvas");
        if (identityHost == null)
            identityHost = new GameObject("ClientIdentityHost");

        EnsureComponent<ClientIdentity>(identityHost);

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("MainCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject root = FindChild(canvas.transform, "MessageOverlayRoot") ?? CreateRectObject("MessageOverlayRoot", canvas.transform);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);

        MessageOverlay overlay = EnsureComponent<MessageOverlay>(root);

        GameObject panel = FindChild(root.transform, "Panel") ?? CreateRectObject("Panel", root.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        Stretch(panelRect);

        Image panelImage = EnsureComponent<Image>(panel);
        panelImage.color = new Color(0f, 0f, 0f, 200f / 255f);
        panelImage.raycastTarget = false;

        GameObject textGo = FindChild(panel.transform, "MessageText") ?? CreateRectObject("MessageText", panel.transform);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(900f, 600f);

        TextMeshProUGUI messageText = EnsureComponent<TextMeshProUGUI>(textGo);
        messageText.text = string.Empty;
        messageText.fontSize = 120f;
        messageText.fontStyle = FontStyles.Bold;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = Color.white;
        messageText.overflowMode = TextOverflowModes.Overflow;
        messageText.raycastTarget = false;

        SerializedObject serializedOverlay = new SerializedObject(overlay);
        serializedOverlay.FindProperty("_panel").objectReferenceValue = panel;
        serializedOverlay.FindProperty("_messageText").objectReferenceValue = messageText;
        serializedOverlay.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);

        SaveActiveScene();
    }

    private static void SetupMonitorScene()
    {
        OpenScene(MonitorScenePath);

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[MessageSystemSceneSetupTool] Monitor scene has no Canvas; skipping monitor UI setup.");
            return;
        }

        GameObject panel = FindChild(canvas.transform, "MessageSystemPanel") ?? CreateRectObject("MessageSystemPanel", canvas.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(-650f, -170f);
        panelRect.sizeDelta = new Vector2(520f, 680f);

        Image panelImage = EnsureComponent<Image>(panel);
        panelImage.color = new Color(0.102f, 0.102f, 0.102f, 1f);
        panelImage.raycastTarget = false;

        CreateLabel(panel.transform, "Label", "MESSAGE SYSTEM", new Vector2(0f, 290f), new Vector2(500f, 60f), 28f);

        GameObject grid = FindChild(panel.transform, "SoundButtonGrid") ?? CreateRectObject("SoundButtonGrid", panel.transform);
        RectTransform gridRect = grid.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.anchoredPosition = new Vector2(0f, 60f);
        gridRect.sizeDelta = new Vector2(480f, 360f);

        GridLayoutGroup layout = EnsureComponent<GridLayoutGroup>(grid);
        layout.cellSize = new Vector2(220f, 110f);
        layout.spacing = new Vector2(16f, 16f);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 2;

        Button chimes = CreateButton(grid.transform, "ChimesButton", "chimes", Vector2.zero, new Vector2(220f, 110f), new Color(0.18f, 0.22f, 0.26f, 1f));
        Button anvil = CreateButton(grid.transform, "AnvilButton", "anvil", Vector2.zero, new Vector2(220f, 110f), new Color(0.18f, 0.22f, 0.26f, 1f));
        Button waterphone = CreateButton(grid.transform, "WaterphoneButton", "waterphone", Vector2.zero, new Vector2(220f, 110f), new Color(0.18f, 0.22f, 0.26f, 1f));
        Button crotales = CreateButton(grid.transform, "CrotalesButton", "crotales", Vector2.zero, new Vector2(220f, 110f), new Color(0.18f, 0.22f, 0.26f, 1f));
        Button cymbal = CreateButton(grid.transform, "CymbalButton", "cymbal", Vector2.zero, new Vector2(220f, 110f), new Color(0.18f, 0.22f, 0.26f, 1f));
        Button waterStation = CreateButton(grid.transform, "WaterStationButton", "water station", Vector2.zero, new Vector2(220f, 110f), new Color(0.18f, 0.22f, 0.26f, 1f));

        GameObject divider = FindChild(panel.transform, "Divider") ?? CreateRectObject("Divider", panel.transform);
        RectTransform dividerRect = divider.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0.5f, 0.5f);
        dividerRect.anchorMax = new Vector2(0.5f, 0.5f);
        dividerRect.pivot = new Vector2(0.5f, 0.5f);
        dividerRect.anchoredPosition = new Vector2(0f, -160f);
        dividerRect.sizeDelta = new Vector2(460f, 2f);
        Image dividerImage = EnsureComponent<Image>(divider);
        dividerImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        dividerImage.raycastTarget = false;

        Button groupMessage = CreateButton(panel.transform, "Go To Your Color Button", "Go To Your Color", new Vector2(0f, -185f), new Vector2(440f, 56f), new Color(0.18f, 0.22f, 0.26f, 1f));
        Button resetDeck = CreateButton(panel.transform, "ResetDeckButton", "Reset Deck", new Vector2(-120f, -265f), new Vector2(210f, 80f), new Color(0.267f, 0.267f, 0.267f, 1f));
        Button hardCut = CreateButton(panel.transform, "HardCutButton", "Hard Cut", new Vector2(120f, -265f), new Vector2(210f, 80f), new Color(0.545f, 0.102f, 0.102f, 1f));

        MonitorMessagePanel monitorPanel = EnsureComponent<MonitorMessagePanel>(panel);
        SerializedObject serializedPanel = new SerializedObject(monitorPanel);
        serializedPanel.FindProperty("_chimesButton").objectReferenceValue = chimes;
        serializedPanel.FindProperty("_anvilButton").objectReferenceValue = anvil;
        serializedPanel.FindProperty("_waterphoneButton").objectReferenceValue = waterphone;
        serializedPanel.FindProperty("_crotalesButton").objectReferenceValue = crotales;
        serializedPanel.FindProperty("_cymbalButton").objectReferenceValue = cymbal;
        serializedPanel.FindProperty("_waterStationButton").objectReferenceValue = waterStation;
        serializedPanel.FindProperty("_resetDeckButton").objectReferenceValue = resetDeck;
        serializedPanel.FindProperty("_hardCutButton").objectReferenceValue = hardCut;
        serializedPanel.FindProperty("_groupMessageButton").objectReferenceValue = groupMessage;
        serializedPanel.ApplyModifiedPropertiesWithoutUndo();

        SaveActiveScene();
    }

    private static void OpenScene(string path)
    {
        if (SceneManager.GetActiveScene().path != path)
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }

    private static void SaveActiveScene()
    {
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static GameObject CreateRectObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject FindChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child != null ? child.gameObject : null;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject label = FindChild(parent, name) ?? CreateRectObject(name, parent);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = EnsureComponent<TextMeshProUGUI>(label);
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color)
    {
        GameObject buttonGo = FindChild(parent, name) ?? CreateRectObject(name, parent);
        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = EnsureComponent<Image>(buttonGo);
        image.color = color;

        Button button = EnsureComponent<Button>(buttonGo);
        button.targetGraphic = image;

        GameObject textGo = FindChild(buttonGo.transform, "Text (TMP)") ?? CreateRectObject("Text (TMP)", buttonGo.transform);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        Stretch(textRect);

        TextMeshProUGUI tmp = EnsureComponent<TextMeshProUGUI>(textGo);
        tmp.text = label;
        tmp.fontSize = size.y > 90f ? 26f : 24f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return button;
    }
}
#endif
