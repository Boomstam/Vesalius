using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dedicated full-screen color overlay used for audience grouping.
/// It is kept separate from the animated ColorOverlay system.
/// </summary>
public class GroupColorOverlay : MonoBehaviour
{
    private const string RootName = "Group Color Overlay";
    private const string ImageName = "Overlay";

    [SerializeField] private bool registerAsSharedInstance = true;
    [SerializeField] private Image overlayImage;

    private bool isVisible;
    private Color currentColor = Color.clear;

    public static GroupColorOverlay EnsureExistsInScene()
    {
        if (Instances.GroupColorOverlay != null)
            return Instances.GroupColorOverlay;

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[GroupColorOverlay] No Canvas found in scene.");
            return null;
        }

        Transform existingRoot = canvas.transform.Find(RootName);
        GroupColorOverlay overlay = existingRoot != null
            ? existingRoot.GetComponent<GroupColorOverlay>()
            : null;

        if (overlay == null)
        {
            GameObject root = existingRoot != null ? existingRoot.gameObject : new GameObject(RootName, typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)root.transform);

            overlay = root.GetComponent<GroupColorOverlay>();
            if (overlay == null)
                overlay = root.AddComponent<GroupColorOverlay>();

            Transform imageTransform = root.transform.Find(ImageName);
            Image image;
            if (imageTransform == null)
            {
                GameObject imageObject = new GameObject(ImageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                imageObject.transform.SetParent(root.transform, false);
                image = imageObject.GetComponent<Image>();
            }
            else
            {
                image = imageTransform.GetComponent<Image>();
            }

            Stretch((RectTransform)image.transform);
            image.raycastTarget = false;
            image.color = Color.clear;
            overlay.overlayImage = image;

            InsertBeforeMessageOverlay(root.transform);
        }

        overlay.InitializeRuntimeReferences();
        return overlay;
    }

    private void Awake()
    {
        InitializeRuntimeReferences();
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
            overlayImage = GetComponentInChildren<Image>(true);

        if (registerAsSharedInstance)
            Instances.GroupColorOverlay = this;

        ApplyState();
    }

    private void ApplyState()
    {
        if (overlayImage == null)
            return;

        overlayImage.color = currentColor;
        overlayImage.enabled = isVisible;
    }

    private static void InsertBeforeMessageOverlay(Transform overlayRoot)
    {
        Transform parent = overlayRoot.parent;
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
}
