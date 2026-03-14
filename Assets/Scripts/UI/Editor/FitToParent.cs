using UnityEditor;
using UnityEngine;
using System.Linq;

/// <summary>
/// Editor utility that resizes all selected UI GameObjects' RectTransforms
/// to exactly match their respective parents, then sets their anchors to
/// cover the full parent rect (0,0 → 1,1) so the fit is maintained on any
/// screen size. Supports multi-selection.
/// </summary>
public static class FitToParent
{
    [MenuItem("Tools/UI/Fit Selected to Parent %#F", validate = false)]
    public static void FitSelectedToParent()
    {
        // Gather only the selected GameObjects that are valid candidates.
        RectTransform[] validTargets = Selection.gameObjects
            .Select(go => go.GetComponent<RectTransform>())
            .Where(rt => rt != null && rt.parent is RectTransform)
            .ToArray();

        if (validTargets.Length == 0)
        {
            Debug.LogWarning("[FitToParent] No selected GameObjects have a RectTransform with a RectTransform parent.");
            return;
        }

        // Register a single grouped undo for all targets.
        Undo.RecordObjects(validTargets, "Fit RectTransforms to Parent");

        int skipped = Selection.gameObjects.Length - validTargets.Length;

        foreach (RectTransform rt in validTargets)
        {
            RectTransform parentRt = (RectTransform)rt.parent;

            // 1. Stretch anchors to fill parent completely.
            rt.anchorMin = Vector2.zero;   // bottom-left  (0, 0)
            rt.anchorMax = Vector2.one;    // top-right    (1, 1)

            // 2. Zero out offsets so the edges land exactly on the parent edges.
            rt.offsetMin = Vector2.zero;   // left / bottom offsets
            rt.offsetMax = Vector2.zero;   // right / top offsets

            // 3. Reset pivot to centre (optional – comment out if unwanted).
            rt.pivot = new Vector2(0.5f, 0.5f);

            EditorUtility.SetDirty(rt);

            Debug.Log($"[FitToParent] '{rt.name}' fitted to parent '{parentRt.name}'. " +
                      $"Parent size: {parentRt.rect.size}");
        }

        if (skipped > 0)
            Debug.LogWarning($"[FitToParent] {skipped} selected object(s) skipped — no RectTransform or no RectTransform parent.");
    }

    /// <summary>
    /// Grey-out the menu item when no selected object qualifies.
    /// </summary>
    [MenuItem("Tools/UI/Fit Selected to Parent %#F", validate = true)]
    public static bool FitSelectedToParentValidate()
    {
        return Selection.gameObjects.Any(go =>
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            return rt != null && rt.parent is RectTransform;
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Anchor to Current Rect  (Ctrl+Shift+M)
    // Moves the anchors to match where the RectTransform currently sits inside
    // its parent, expressed as normalised (0–1) parent-space values.
    // The rect itself is NOT moved — offsetMin/offsetMax are recalculated to
    // compensate so the object stays exactly where it is.
    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/UI/Anchor to Current Rect %#M", validate = false)]
    public static void AnchorToCurrentRect()
    {
        RectTransform[] validTargets = Selection.gameObjects
            .Select(go => go.GetComponent<RectTransform>())
            .Where(rt => rt != null && rt.parent is RectTransform)
            .ToArray();

        if (validTargets.Length == 0)
        {
            Debug.LogWarning("[AnchorToRect] No selected GameObjects have a RectTransform with a RectTransform parent.");
            return;
        }

        Undo.RecordObjects(validTargets, "Anchor RectTransforms to Current Rect");

        int skipped = Selection.gameObjects.Length - validTargets.Length;

        foreach (RectTransform rt in validTargets)
        {
            RectTransform parentRt = (RectTransform)rt.parent;
            Rect parentRect = parentRt.rect;

            if (parentRect.width == 0 || parentRect.height == 0)
            {
                Debug.LogWarning($"[AnchorToRect] Skipping '{rt.name}' — parent has zero size.");
                continue;
            }

            // Current world-space corners of this rect (0=BL,1=BR,2=TR,3=TL).
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            // Convert to parent local space.
            Vector2 localBL = parentRt.InverseTransformPoint(corners[0]);
            Vector2 localTR = parentRt.InverseTransformPoint(corners[2]);

            // Express as normalised fractions of the parent rect.
            Vector2 newAnchorMin = new Vector2(
                (localBL.x - parentRect.x) / parentRect.width,
                (localBL.y - parentRect.y) / parentRect.height);

            Vector2 newAnchorMax = new Vector2(
                (localTR.x - parentRect.x) / parentRect.width,
                (localTR.y - parentRect.y) / parentRect.height);

            // Apply new anchors — at this point Unity would shift the rect,
            // so we immediately zero the offsets to keep edges pinned.
            rt.anchorMin = newAnchorMin;
            rt.anchorMax = newAnchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            EditorUtility.SetDirty(rt);

            Debug.Log($"[AnchorToRect] '{rt.name}' anchors set to " +
                      $"min{newAnchorMin:F3} max{newAnchorMax:F3}.");
        }

        if (skipped > 0)
            Debug.LogWarning($"[AnchorToRect] {skipped} selected object(s) skipped — no RectTransform or no RectTransform parent.");
    }

    [MenuItem("Tools/UI/Anchor to Current Rect %#M", validate = true)]
    public static bool AnchorToCurrentRectValidate()
    {
        return Selection.gameObjects.Any(go =>
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            return rt != null && rt.parent is RectTransform;
        });
    }
}