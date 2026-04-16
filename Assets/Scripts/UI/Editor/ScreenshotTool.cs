using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool to capture a screenshot of the actual rendered Game View.
/// Must be used while in Play Mode to capture real scene content.
///
/// Usage: Tools > Screenshot Tool  (then press Capture while in Play Mode)
/// </summary>
public class ScreenshotTool : EditorWindow
{
    private string savePath = "Screenshots";
    private string fileName = "screenshot";
    private int superSampling = 1;

    [MenuItem("Tools/Screenshot Tool")]
    public static void ShowWindow()
    {
        ScreenshotTool window = GetWindow<ScreenshotTool>("Screenshot Tool");
        window.minSize = new Vector2(320, 260);
    }

    private void OnGUI()
    {
        GUILayout.Label("Play Store Screenshot Capture", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // --- Save settings ---
        EditorGUILayout.LabelField("Save Settings", EditorStyles.boldLabel);
        savePath = EditorGUILayout.TextField("Save Folder", savePath);
        fileName = EditorGUILayout.TextField("File Name", fileName);
        EditorGUILayout.Space(5);

        // --- Resolution info ---
        EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField("Width  (Game View)", Screen.width);
        EditorGUILayout.IntField("Height (Game View)", Screen.height);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(4);
        superSampling = EditorGUILayout.IntSlider("Super Sampling", superSampling, 1, 4);
        EditorGUILayout.HelpBox(
            superSampling > 1
                ? $"Output will be {Screen.width * superSampling} x {Screen.height * superSampling} — downsampled for quality."
                : "Output matches Game View resolution.",
            MessageType.Info);

        EditorGUILayout.Space(8);

        // --- Play Mode warning ---
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Enter Play Mode first — the screenshot captures the live rendered frame.",
                MessageType.Warning);
        }

        // --- Capture button ---
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        GUI.backgroundColor = Application.isPlaying ? new Color(0.3f, 0.8f, 0.4f) : Color.gray;
        if (GUILayout.Button("Capture Screenshot", GUILayout.Height(36)))
        {
            // Defer one frame so the Game View is fully rendered before we read it
            EditorApplication.delayCall += CaptureScreenshot;
        }
        GUI.backgroundColor = Color.white;
        EditorGUI.EndDisabledGroup();
    }

    private void CaptureScreenshot()
    {
        // ScreenCapture reads the exact composited frame — UI, post-processing and all
        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture(superSampling);

        if (screenshot == null)
        {
            EditorUtility.DisplayDialog("Screenshot Tool",
                "Capture failed. Make sure the Game View is visible and Play Mode is active.", "OK");
            return;
        }

        string fullPath = SavePNG(screenshot, screenshot.width, screenshot.height);
        DestroyImmediate(screenshot);

        if (fullPath != null)
        {
            AssetDatabase.Refresh();
            Debug.Log($"[ScreenshotTool] Saved: {fullPath}");
            EditorUtility.RevealInFinder(fullPath);
        }
    }

    private string SavePNG(Texture2D tex, int width, int height)
    {
        try
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string folder      = Path.Combine(projectRoot, savePath);
            Directory.CreateDirectory(folder);

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fullName  = $"{fileName}_{width}x{height}_{timestamp}.png";
            string fullPath  = Path.Combine(folder, fullName);

            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            return fullPath;
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Screenshot Tool", $"Failed to save:\n{e.Message}", "OK");
            return null;
        }
    }
}