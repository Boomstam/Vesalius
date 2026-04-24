using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dedicated full-screen color overlay used for audience grouping.
/// It is kept separate from the animated ColorOverlay system.
/// </summary>
public class GroupColorOverlay : MonoBehaviour
{
    private const string RootName = "Group Color Overlay";
    private const string OverlayImageName = "Overlay";
    private const string MarbleLayerName = "Marble Layer";
    private const string MainCanvasName = "MainCanvas";
    private const string MessageOverlayRootName = "MessageOverlayRoot";
    private static readonly Color DefaultMarbleColor = new(1f, 1f, 1f, 0.35f);

    [SerializeField] private bool registerAsSharedInstance = true;
    [SerializeField] private Image overlayImage;
    [SerializeField] private RawImage marbleOverlayImage;

    private bool isVisible;
    private Color currentColor = Color.clear;

    public bool IsVisible => isVisible && overlayImage != null && overlayImage.enabled;

    public static GroupColorOverlay EnsureExistsInScene()
    {
        if (Instances.GroupColorOverlay != null)
        {
            Instances.GroupColorOverlay.EnsureDisplayOrder();
            return Instances.GroupColorOverlay;
        }

        Canvas canvas = FindTargetCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("[GroupColorOverlay] No Canvas found in scene.");
            return null;
        }

        GroupColorOverlay overlay = FindExistingOverlayInScene();
        Transform existingRoot = overlay != null ? overlay.transform : canvas.transform.Find(RootName);

        if (overlay == null)
        {
            GameObject root = existingRoot != null ? existingRoot.gameObject : new GameObject(RootName, typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)root.transform);

            overlay = root.GetComponent<GroupColorOverlay>();
            if (overlay == null)
                overlay = root.AddComponent<GroupColorOverlay>();

            overlay.overlayImage = EnsureOverlayImage(root.transform);
            overlay.marbleOverlayImage = EnsureMarbleOverlayImage(root.transform, canvas.transform);
        }
        else if (overlay.transform.parent != canvas.transform)
        {
            overlay.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)overlay.transform);
        }

        overlay.overlayImage = EnsureOverlayImage(overlay.transform);
        overlay.marbleOverlayImage = EnsureMarbleOverlayImage(overlay.transform, canvas.transform);

        overlay.InitializeRuntimeReferences();
        overlay.EnsureDisplayOrder();
        return overlay;
    }

    private void Awake()
    {
        InitializeRuntimeReferences();
        EnsureDisplayOrder();
    }

    private void OnDestroy()
    {
        if (registerAsSharedInstance && Instances.GroupColorOverlay == this)
            Instances.GroupColorOverlay = null;
    }

    public void Show(Color color)
    {
        currentColor = color;
        currentColor.a = 1f;
        isVisible = true;
        EnsureDisplayOrder();
        ApplyState();
    }

    public void Hide()
    {
        isVisible = false;
        ApplyState();
    }

    private void InitializeRuntimeReferences()
    {
        if (overlayImage == null)
            overlayImage = FindChildImage(OverlayImageName);

        if (marbleOverlayImage == null)
            marbleOverlayImage = FindChildRawImage(MarbleLayerName);

        if (registerAsSharedInstance)
            Instances.GroupColorOverlay = this;

        ApplyState();
    }

    private void EnsureDisplayOrder()
    {
        Canvas canvas = FindTargetCanvas();
        if (canvas == null)
            return;

        if (transform.parent != canvas.transform)
            transform.SetParent(canvas.transform, false);

        Stretch((RectTransform)transform);
        InsertBeforeProtectedUi(transform);
    }

    private void ApplyState()
    {
        if (overlayImage == null)
            return;

        overlayImage.color = currentColor;
        overlayImage.enabled = isVisible;

        if (marbleOverlayImage != null)
            marbleOverlayImage.enabled = isVisible;
    }

    private static void InsertBeforeProtectedUi(Transform overlayRoot)
    {
        Transform parent = overlayRoot.parent;
        if (parent == null)
            return;

        Transform messageOverlayRoot = parent.Find(MessageOverlayRootName);
        if (messageOverlayRoot != null)
        {
            overlayRoot.SetSiblingIndex(messageOverlayRoot.GetSiblingIndex());
            return;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name.Contains("MessageOverlay"))
            {
                overlayRoot.SetSiblingIndex(i);
                return;
            }
        }

        overlayRoot.SetAsLastSibling();
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

    private Image FindChildImage(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private RawImage FindChildRawImage(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<RawImage>() : null;
    }

    private static Image EnsureOverlayImage(Transform parent)
    {
        Image image = EnsureImage(parent, OverlayImageName);
        Stretch((RectTransform)image.transform);
        image.raycastTarget = false;
        image.color = Color.clear;
        image.transform.SetSiblingIndex(0);
        return image;
    }

    private static RawImage EnsureMarbleOverlayImage(Transform parent, Transform canvasTransform)
    {
        RawImage marbleImage = EnsureRawImage(parent, MarbleLayerName);
        Stretch((RectTransform)marbleImage.transform);
        marbleImage.raycastTarget = false;
        marbleImage.transform.SetSiblingIndex(1);

        RawImage sourceMarble = FindSourceMarbleLayer(canvasTransform);
        if (sourceMarble != null)
        {
            marbleImage.texture = sourceMarble.texture;
            marbleImage.color = sourceMarble.color;
            marbleImage.material = sourceMarble.material;
            marbleImage.uvRect = sourceMarble.uvRect;
        }
        else
        {
            marbleImage.color = DefaultMarbleColor;
        }

        return marbleImage;
    }

    private static Image EnsureImage(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            GameObject childObject = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child = childObject.transform;
            child.SetParent(parent, false);
        }

        Image image = child.GetComponent<Image>();
        if (image == null)
            image = child.gameObject.AddComponent<Image>();

        return image;
    }

    private static RawImage EnsureRawImage(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            GameObject childObject = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            child = childObject.transform;
            child.SetParent(parent, false);
        }

        RawImage rawImage = child.GetComponent<RawImage>();
        if (rawImage == null)
        {
            Image legacyImage = child.GetComponent<Image>();
            if (legacyImage != null)
                Object.Destroy(legacyImage);

            rawImage = child.gameObject.AddComponent<RawImage>();
        }

        return rawImage;
    }

    private static RawImage FindSourceMarbleLayer(Transform canvasTransform)
    {
        foreach (RawImage rawImage in Resources.FindObjectsOfTypeAll<RawImage>())
        {
            if (rawImage == null ||
                rawImage.gameObject.name != MarbleLayerName ||
                rawImage.texture == null ||
                !rawImage.gameObject.scene.IsValid() ||
                (rawImage.hideFlags & HideFlags.HideAndDontSave) != 0)
            {
                continue;
            }

            if (canvasTransform == null || rawImage.transform.root == canvasTransform.root)
                return rawImage;
        }

        return null;
    }

    private static Canvas FindTargetCanvas()
    {
        Canvas namedCanvas = GameObject.Find(MainCanvasName)?.GetComponent<Canvas>();
        if (IsUsableRootCanvas(namedCanvas))
            return namedCanvas;

        Canvas fallbackCanvas = null;
        foreach (Canvas canvas in Resources.FindObjectsOfTypeAll<Canvas>())
        {
            if (!IsUsableRootCanvas(canvas))
                continue;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return canvas;

            fallbackCanvas ??= canvas;
        }

        return fallbackCanvas;
    }

    private static bool IsUsableRootCanvas(Canvas canvas)
    {
        return canvas != null
            && canvas.gameObject.scene.IsValid()
            && (canvas.hideFlags & HideFlags.HideAndDontSave) == 0
            && canvas.isActiveAndEnabled
            && canvas.rootCanvas == canvas;
    }

    private static GroupColorOverlay FindExistingOverlayInScene()
    {
        foreach (GroupColorOverlay overlay in Resources.FindObjectsOfTypeAll<GroupColorOverlay>())
        {
            if (overlay == null ||
                !overlay.gameObject.scene.IsValid() ||
                (overlay.hideFlags & HideFlags.HideAndDontSave) != 0)
            {
                continue;
            }

            return overlay;
        }

        return null;
    }
}
