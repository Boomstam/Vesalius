// Place this file in any folder named "Editor" inside your Assets directory.
// e.g. Assets/Editor/SceneOverhaulTool.cs
//
// Open via:  Tools → Scene Overhaul → Run Scene Overhaul
//
// What it does:
//   Step 1  — Deletes TabBar, old Content children (Tutorial/Vesalius/Film/Sounds/Content Image)
//   Step 2  — Creates Part_0_Content … Part_10_Content and Part_0_Info … Part_10_Info
//             Each gets a full InfoManager UI sub-tree (background, title, content, prev/next).
//             Parts 1-10 Content also get a child Image GO for server-driven art.
//             Part 0 Content gets SoundsSlotManager + SoundsPanel child.
//             Parts with image entries get ContentImageManager.
//   Step 3  — Adds ViewManager to MainCanvas and wires all arrays.
//   Step 4  — Clears stale onClick listeners on the Information Icon button.
//
// Anything that requires manual attention afterwards is logged to the Console
// with the prefix [SceneOverhaul].

#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SceneOverhaulTool
{
    // ── Part metadata ────────────────────────────────────────────────────────

    private static readonly string[] PartNames =
    {
        "Intro",
        "BookI_Nutrition",
        "BookIII_VeinsArteries",
        "BookI_Generation",
        "BookII_LigamentsMuscles",
        "WordsOfVesalius",
        "BookIV_Nerves",
        "BookV_BonesCartilages",
        "BookVI_Brain",
        "BookVI_Senses",
        "BookVII_Heart",
    };

    // Parts whose Info JSON contains image-type entries → get ContentImageManager.
    // Adjust this set if your JSONs differ.
    private static readonly bool[] InfoHasImages =
    {
        false, // 0  Intro
        true,  // 1  Nutrition
        true,  // 2  Veins/Arteries
        true,  // 3  Generation
        true,  // 4  Ligaments/Muscles
        false, // 5  Words of Vesalius
        true,  // 6  Nerves
        true,  // 7  Bones/Cartilages
        true,  // 8  Brain
        true,  // 9  Senses
        true,  // 10 Heart
    };

    // ── Entry point ──────────────────────────────────────────────────────────

    [MenuItem("Tools/Scene Overhaul/Run Scene Overhaul")]
    public static void RunOverhaul()
    {
        if (!EditorUtility.DisplayDialog(
            "Scene Overhaul",
            "This will permanently restructure the active Client scene.\n\n" +
            "Make sure you have committed or backed up the scene first.\n\n" +
            "Continue?",
            "Yes, do it", "Cancel"))
        {
            return;
        }

        try
        {
            AssetDatabase.StartAssetEditing();

            var mainCanvas = FindRequired<Canvas>("MainCanvas").gameObject;
            var content    = FindRequiredChild(mainCanvas, "Content");

            Step1_DeleteOldStructure(mainCanvas, content);
            var (contentViews, infoViews, infoManagers, part0ContentIM) =
                Step2_BuildPartGameObjects(content);
            Step3_AddViewManager(mainCanvas, contentViews, infoViews, infoManagers, part0ContentIM);
            Step4_CleanInfoToggleButton(mainCanvas);

            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[SceneOverhaul] ✅ Done. Review Console warnings for any manual wiring needed.");
            EditorUtility.DisplayDialog("Scene Overhaul", "Overhaul complete!\n\nCheck the Console for any items that need manual attention.", "OK");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SceneOverhaul] ❌ Aborted with error: {e}");
            EditorUtility.DisplayDialog("Scene Overhaul Failed", e.Message, "OK");
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
    }

    // ── Step 1 ───────────────────────────────────────────────────────────────

    private static void Step1_DeleteOldStructure(GameObject mainCanvas, GameObject content)
    {
        return;
        Debug.Log("[SceneOverhaul] Step 1: Removing old structure…");

        // Remove TabBar from MainCanvas
        DeleteChild(mainCanvas, "TabBar");

        // Remove old Content children
        foreach (string name in new[] { "Tutorial", "Vesalius", "Film", "Sounds", "Content Image" })
            DeleteChild(content, name);
    }

    // ── Step 2 ───────────────────────────────────────────────────────────────

    private static (GameObject[] contentViews,
                    GameObject[] infoViews,
                    InfoManager[] infoManagers,
                    InfoManager   part0ContentIM)
        Step2_BuildPartGameObjects(GameObject content)
    {
        Debug.Log("[SceneOverhaul] Step 2: Building Part GameObjects…");

        int count = PartNames.Length;

        var contentViews  = new GameObject[count];
        var infoViews     = new GameObject[count];
        var infoManagers  = new InfoManager[count];
        InfoManager part0ContentIM = null;

        for (int i = 0; i < count; i++)
        {
            // ── Content view ─────────────────────────────────────────────────
            var cv = CreateChild(content, $"Part_{i}_Content");
            StretchToParent(cv);
            cv.SetActive(i == 0); // only Part 0 active initially

            if (i == 0)
            {
                // Part 0 Content: InfoManager (the tutorial) + SoundsSlotManager
                var im = BuildInfoManagerSubTree(cv, $"Part_0_Content InfoManager");
                part0ContentIM = im;

                var sounds = CreateChild(cv, "SoundsPanel");
                StretchToParent(sounds);
                sounds.SetActive(false);
                // CrossfadePlayer / slider UI goes inside SoundsPanel — add manually.
                Debug.Log("[SceneOverhaul] ⚠️  Part_0_Content/SoundsPanel created. " +
                          "Move your CrossfadePlayer + slider UI inside it.");

                var ssm = cv.AddComponent<SoundsSlotManager>();
                SetPrivateField(ssm, "infoManager", im);
                SetPrivateField(ssm, "soundsPanel", sounds);
            }
            else
            {
                // Parts 1-10 Content: just an Image child for server-driven art.
                var img = CreateChild(cv, "ContentImage");
                StretchToParent(img);
                img.AddComponent<Image>();
                Debug.Log($"[SceneOverhaul] ℹ️  Part_{i}_Content/ContentImage created. " +
                          "Assign art sprites here from the server layer.");
            }

            // ── Info view ────────────────────────────────────────────────────
            var iv = CreateChild(content, $"Part_{i}_Info");
            StretchToParent(iv);
            iv.SetActive(false);

            var infoIM = BuildInfoManagerSubTree(iv, $"Part_{i}_Info InfoManager");
            infoManagers[i] = infoIM;

            // Set a placeholder resource path — user must fill in the real path.
            SetPrivateField(infoIM, "chaptersJsonResourcePath", $"Parts/{PartNames[i]}.en");
            Debug.Log($"[SceneOverhaul] ⚠️  Set chaptersJsonResourcePath on Part_{i}_Info " +
                      $"to placeholder 'Parts/{PartNames[i]}.en' — update to your real path.");

            if (i == 0)
            {
                // Also set placeholder on Part 0 Content IM.
                SetPrivateField(part0ContentIM, "chaptersJsonResourcePath", $"Parts/{PartNames[0]}_Tutorial.en");
                Debug.Log($"[SceneOverhaul] ⚠️  Set chaptersJsonResourcePath on Part_0_Content " +
                          $"to placeholder 'Parts/Intro_Tutorial.en' — update to your real path.");
            }

            if (InfoHasImages[i])
            {
                var cim = iv.AddComponent<ContentImageManager>();
                SetPrivateField(cim, "infoManager", infoIM);
                SetPrivateField(cim, "chaptersJsonResourcePath", $"Parts/{PartNames[i]}.en");
                // displayImage and imageTitleText need manual wiring — they live inside the InfoManager sub-tree.
                Debug.Log($"[SceneOverhaul] ⚠️  ContentImageManager added to Part_{i}_Info. " +
                          "Wire displayImage and imageTitleText in the Inspector, and assign imageAssets.");
            }

            contentViews[i] = cv;
            infoViews[i]    = iv;
        }

        return (contentViews, infoViews, infoManagers, part0ContentIM);
    }

    // ── Step 3 ───────────────────────────────────────────────────────────────

    private static void Step3_AddViewManager(
        GameObject   mainCanvas,
        GameObject[] contentViews,
        GameObject[] infoViews,
        InfoManager[] infoManagers,
        InfoManager   part0ContentIM)
    {
        Debug.Log("[SceneOverhaul] Step 3: Adding ViewManager…");

        // Remove any existing ViewManager to avoid duplicates on re-run.
        var existing = mainCanvas.GetComponent<ViewManager>();
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing);
            Debug.Log("[SceneOverhaul] Removed existing ViewManager before re-adding.");
        }

        var vm = mainCanvas.AddComponent<ViewManager>();
        var so = new SerializedObject(vm);

        SetSerializedArray(so, "contentViews",  contentViews.Cast<UnityEngine.Object>().ToArray());
        SetSerializedArray(so, "infoViews",     infoViews.Cast<UnityEngine.Object>().ToArray());
        SetSerializedArray(so, "infoManagers",  infoManagers.Cast<UnityEngine.Object>().ToArray());
        so.FindProperty("part0ContentInfoManager").objectReferenceValue = part0ContentIM;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Info toggle button: locate by name and wire it.
        var infoIconGO = FindChildRecursive(mainCanvas.transform, "Information Icon");
        if (infoIconGO != null)
        {
            var btn = infoIconGO.GetComponentInChildren<Button>();
            if (btn != null)
            {
                so.Update();
                so.FindProperty("infoToggleButton").objectReferenceValue = btn;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[SceneOverhaul] ✅ Wired infoToggleButton to Information Icon.");
            }
            else
            {
                Debug.Log("[SceneOverhaul] ⚠️  Could not find a Button under 'Information Icon'. Wire infoToggleButton manually.");
            }
        }
        else
        {
            Debug.Log("[SceneOverhaul] ⚠️  'Information Icon' GameObject not found. Wire infoToggleButton manually.");
        }
    }

    // ── Step 4 ───────────────────────────────────────────────────────────────

    private static void Step4_CleanInfoToggleButton(GameObject mainCanvas)
    {
        Debug.Log("[SceneOverhaul] Step 4: Cleaning stale onClick listeners…");

        var infoIconGO = FindChildRecursive(mainCanvas.transform, "Information Icon");
        if (infoIconGO == null)
        {
            Debug.Log("[SceneOverhaul] ⚠️  'Information Icon' not found — skipping onClick cleanup.");
            return;
        }

        var btn = infoIconGO.GetComponentInChildren<Button>();
        if (btn == null) return;

        var so  = new SerializedObject(btn);
        var onClick = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");

        if (onClick != null && onClick.arraySize > 0)
        {
            onClick.ClearArray();
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[SceneOverhaul] ✅ Cleared persistent onClick listeners on Information Icon button. " +
                      "ViewManager.ToggleView is wired in code — no Inspector listener needed.");
        }
    }

    // ── InfoManager sub-tree builder ─────────────────────────────────────────

    /// <summary>
    /// Creates a standard InfoManager UI hierarchy under <paramref name="parent"/>
    /// and wires all serialized references. Returns the InfoManager component.
    ///
    /// Hierarchy created:
    ///   parent
    ///   ├── Background      (Image)
    ///   ├── Title           (TextMeshProUGUI)
    ///   ├── Content         (TextMeshProUGUI)
    ///   └── Navigation
    ///       ├── PrevButton  (Button → Text "<")
    ///       └── NextButton  (Button → Text ">")
    /// </summary>
    private static InfoManager BuildInfoManagerSubTree(GameObject parent, string debugLabel)
    {
        // Background
        var bg = CreateChild(parent, "Background");
        StretchToParent(bg);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0f); // transparent — assign sprite manually

        // Title
        var titleGO = CreateChild(parent, "Title");
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f,   0.85f);
        titleRT.anchorMax = new Vector2(1f,   1f);
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "TITLE";
        titleTMP.fontSize  = 36;
        titleTMP.alignment = TextAlignmentOptions.Center;

        // Content
        var contentGO = CreateChild(parent, "Content");
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f,   0.15f);
        contentRT.anchorMax = new Vector2(1f,   0.85f);
        contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;
        var contentTMP = contentGO.AddComponent<TextMeshProUGUI>();
        contentTMP.text      = "";
        contentTMP.fontSize  = 24;
        contentTMP.alignment = TextAlignmentOptions.TopLeft;

        // Navigation
        var nav    = CreateChild(parent, "Navigation");
        var navRT  = nav.GetComponent<RectTransform>();
        navRT.anchorMin = new Vector2(0f, 0f);
        navRT.anchorMax = new Vector2(1f, 0.15f);
        navRT.offsetMin = navRT.offsetMax = Vector2.zero;

        var prevBtn = CreateButton(nav, "PrevButton", "<");
        var nextBtn = CreateButton(nav, "NextButton", ">");

        // Position prev left, next right
        var prevRT = prevBtn.GetComponent<RectTransform>();
        prevRT.anchorMin = new Vector2(0f,   0f);
        prevRT.anchorMax = new Vector2(0.2f, 1f);
        prevRT.offsetMin = prevRT.offsetMax = Vector2.zero;

        var nextRT = nextBtn.GetComponent<RectTransform>();
        nextRT.anchorMin = new Vector2(0.8f, 0f);
        nextRT.anchorMax = new Vector2(1f,   1f);
        nextRT.offsetMin = nextRT.offsetMax = Vector2.zero;

        // Add + wire InfoManager
        var im = parent.AddComponent<InfoManager>();
        var so = new SerializedObject(im);
        so.FindProperty("infoTitleText").objectReferenceValue      = titleTMP;
        so.FindProperty("infoContentText").objectReferenceValue    = contentTMP;
        so.FindProperty("previousInfoButton").objectReferenceValue = prevBtn.GetComponent<Button>();
        so.FindProperty("nextInfoButton").objectReferenceValue     = nextBtn.GetComponent<Button>();
        so.FindProperty("infoBackground").objectReferenceValue     = bgImg;
        so.ApplyModifiedPropertiesWithoutUndo();

        return im;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static GameObject CreateChild(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void StretchToParent(GameObject go)
    {
        var rt        = go.GetComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
    }

    private static GameObject CreateButton(GameObject parent, string name, string label)
    {
        var go  = CreateChild(parent, name);
        go.AddComponent<Image>();
        go.AddComponent<Button>();

        var textGO = CreateChild(go, "Text");
        StretchToParent(textGO);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 32;
        tmp.alignment = TextAlignmentOptions.Center;

        return go;
    }

    private static void DeleteChild(GameObject parent, string childName)
    {
        var t = parent.transform.Find(childName);
        if (t != null)
        {
            UnityEngine.Object.DestroyImmediate(t.gameObject);
            Debug.Log($"[SceneOverhaul] Deleted '{childName}'.");
        }
        else
        {
            Debug.Log($"[SceneOverhaul] ℹ️  '{childName}' not found — already removed or renamed.");
        }
    }

    private static T FindRequired<T>(string goName) where T : Component
    {
        var obj = GameObject.Find(goName);
        if (obj == null) throw new Exception($"Could not find GameObject '{goName}' in the active scene.");
        var comp = obj.GetComponent<T>();
        if (comp == null) throw new Exception($"GameObject '{goName}' has no {typeof(T).Name} component.");
        return comp;
    }

    private static GameObject FindRequiredChild(GameObject parent, string childName)
    {
        var t = parent.transform.Find(childName);
        if (t == null) throw new Exception($"Could not find child '{childName}' under '{parent.name}'.");
        return t.gameObject;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

        if (field == null)
        {
            Debug.LogWarning($"[SceneOverhaul] ⚠️  Field '{fieldName}' not found on {target.GetType().Name}. Skipping.");
            return;
        }

        field.SetValue(target, value);
    }

    private static void SetSerializedArray(SerializedObject so, string propertyName, UnityEngine.Object[] values)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning($"[SceneOverhaul] ⚠️  Serialized property '{propertyName}' not found.");
            return;
        }

        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif