using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Debug overlay for beat timing system. Displays all timing variables to help diagnose sync issues.
/// Add this to any GameObject in the scene. Toggle with F3 key.
/// </summary>
public class BeatDebugUI : MonoBehaviour
{
    [Header("References (auto-finds if null)")]
    [SerializeField] private BeatHandler beatHandler;
    [SerializeField] private MusicManager musicManager;

    [Header("Display Settings")]
    [SerializeField] private bool showDebug = true;

    // Timing tracking
    private float lastBeatTime;
    private float lastBeatInterval;
    private int lastSeenBeat;

    // Input testing
    private struct InputRecord
    {
        public float time;
        public float percentToNextBeat;
        public int musicBeatIndex;
        public int beatIndexPlusOne;  // What CheckValidAttackInterval uses
        public bool wasValid;
        public float offsetMs;  // How far from nearest beat boundary
    }
    private List<InputRecord> inputHistory = new List<InputRecord>();
    private const int MAX_HISTORY = 8;

    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private GUIStyle hitStyle;
    private GUIStyle missStyle;

    void Start()
    {
        if (beatHandler == null)
            beatHandler = FindFirstObjectByType<BeatHandler>();
        if (musicManager == null)
            musicManager = MusicManager.instance;
    }

    void Update()
    {
        // Track beat intervals
        if (musicManager != null && musicManager.timelineInfo != null)
        {
            int currentBeat = musicManager.timelineInfo.currentBeat;
            if (currentBeat != lastSeenBeat)
            {
                lastBeatInterval = Time.time - lastBeatTime;
                lastBeatTime = Time.time;
                lastSeenBeat = currentBeat;
            }
        }

        // Track mouse clicks for input testing
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            RecordInput();
        }
        // Also track keyboard space
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            RecordInput();
        }
    }

    private void RecordInput()
    {
        if (beatHandler == null || musicManager == null) return;

        float percent = beatHandler.GetPercentToNextBeat();
        float bpm = musicManager.timelineInfo.currentBpm;
        float msPerBeat = bpm > 0 ? (60f / bpm / 2f) * 1000f : 0f;

        // Calculate offset from nearest beat boundary (0.0 or 1.0)
        float offsetFromBeat;
        if (percent <= 0.5f)
        {
            offsetFromBeat = percent;  // Distance from 0.0 (last beat)
        }
        else
        {
            offsetFromBeat = percent - 1.0f;  // Distance from 1.0 (next beat), will be negative
        }
        float offsetMs = offsetFromBeat * msPerBeat;

        var record = new InputRecord
        {
            time = Time.time,
            percentToNextBeat = percent,
            musicBeatIndex = beatHandler.GetMusicBeatIndex(),
            wasValid = beatHandler.ValidAttackInterval,
            offsetMs = offsetMs
        };

        inputHistory.Insert(0, record);
        if (inputHistory.Count > MAX_HISTORY)
        {
            inputHistory.RemoveAt(inputHistory.Count - 1);
        }
    }

    void OnGUI()
    {
        if (!showDebug) return;

        // Initialize styles
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTexture(2, 2, new Color(0f, 0f, 0f, 0.8f));
        }
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 14;
        }
        if (hitStyle == null)
        {
            hitStyle = new GUIStyle(GUI.skin.label);
            hitStyle.normal.textColor = Color.green;
            hitStyle.fontSize = 14;
        }
        if (missStyle == null)
        {
            missStyle = new GUIStyle(GUI.skin.label);
            missStyle.normal.textColor = Color.red;
            missStyle.fontSize = 14;
        }

        float x = 10;
        float y = 10;
        float width = 400;
        float lineHeight = 20;
        float sectionGap = 10;

        // Background box
        float totalHeight = 640;
        GUI.Box(new Rect(x - 5, y - 5, width + 10, totalHeight), "", boxStyle);

        // === FMOD Section ===
        GUI.Label(new Rect(x, y, width, lineHeight), "=== FMOD (MusicManager) ===", labelStyle);
        y += lineHeight;

        if (musicManager != null && musicManager.timelineInfo != null)
        {
            GUI.Label(new Rect(x, y, width, lineHeight),
                $"currentBeat: {musicManager.timelineInfo.currentBeat}  (1-8, authoritative)", labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(x, y, width, lineHeight),
                $"currentBpm: {musicManager.timelineInfo.currentBpm:F1}", labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(x, y, width, lineHeight),
                $"lastMarker: \"{(string)musicManager.timelineInfo.lastMarker}\"", labelStyle);
            y += lineHeight;
        }
        else
        {
            GUI.Label(new Rect(x, y, width, lineHeight), "MusicManager not found!", labelStyle);
            y += lineHeight;
        }

        y += sectionGap;

        // === BeatHandler Section ===
        GUI.Label(new Rect(x, y, width, lineHeight), "=== BeatHandler ===", labelStyle);
        y += lineHeight;

        if (beatHandler != null)
        {
            // These require public getters - see instructions below
            GUI.Label(new Rect(x, y, width, lineHeight),
                $"musicBeatIndex: {beatHandler.GetMusicBeatIndex()}  (copy of FMOD)", labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(x, y, width, lineHeight),
                $"prevMusicIndex: {beatHandler.GetPrevMusicIndex()}  (for change detection)", labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(x, y, width, lineHeight),
                $"percentToNextBeat: {beatHandler.GetPercentToNextBeat():F3}  (0-1 timer)", labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(x, y, width, lineHeight),
                $"beatIndexHasChanged: {beatHandler.GetBeatIndexHasChanged()}", labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(x, y, width, lineHeight),
                $"bpm (local): {beatHandler.GetBPM():F1}", labelStyle);
            y += lineHeight;
        }
        else
        {
            GUI.Label(new Rect(x, y, width, lineHeight), "BeatHandler not found!", labelStyle);
            y += lineHeight;
        }

        y += sectionGap;

        // === Validation Section ===
        GUI.Label(new Rect(x, y, width, lineHeight), "=== Validation ===", labelStyle);
        y += lineHeight;

        if (beatHandler != null)
        {
            string attackColor = beatHandler.ValidAttackInterval ? "<color=green>TRUE</color>" : "<color=red>FALSE</color>";
            string dashColor = beatHandler.ValidDashInterval ? "<color=green>TRUE</color>" : "<color=red>FALSE</color>";

            GUI.Label(new Rect(x, y, width, lineHeight),
                $"ValidAttackInterval: {beatHandler.ValidAttackInterval}", labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(x, y, width, lineHeight),
                $"ValidDashInterval: {beatHandler.ValidDashInterval}", labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(x, y, width, lineHeight),
                $"unlockedBeats: [{string.Join(", ", BeatHandler.UnlockedBeats)}]", labelStyle);
            y += lineHeight;
        }

        y += sectionGap;

        // === Timing Analysis ===
        GUI.Label(new Rect(x, y, width, lineHeight), "=== Timing Analysis ===", labelStyle);
        y += lineHeight;

        if (musicManager != null && musicManager.timelineInfo != null)
        {
            float bpm = musicManager.timelineInfo.currentBpm;
            float expectedInterval = bpm > 0 ? 60f / bpm / 2f : 0f; // /2 for 8 beats in 4/4

            GUI.Label(new Rect(x, y, width, lineHeight),
                $"Expected interval: {expectedInterval * 1000:F1}ms (at {bpm:F0} BPM)", labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(x, y, width, lineHeight),
                $"Last actual interval: {lastBeatInterval * 1000:F1}ms", labelStyle);
            y += lineHeight;

            float drift = (lastBeatInterval - expectedInterval) * 1000;
            GUI.Label(new Rect(x, y, width, lineHeight),
                $"Drift: {drift:+0.0;-0.0;0.0}ms", labelStyle);
            y += lineHeight;
        }

        y += sectionGap;

        // === Input Test Section ===
        GUI.Label(new Rect(x, y, width, lineHeight), "=== Input Test (click or space) ===", labelStyle);
        y += lineHeight;

        GUI.Label(new Rect(x, y, width, lineHeight),
            "Click/Space to test. Shows offset from beat & validity.", labelStyle);
        y += lineHeight;

        foreach (var record in inputHistory)
        {
            var style = record.wasValid ? hitStyle : missStyle;
            string validStr = record.wasValid ? "HIT" : "MISS";
            GUI.Label(new Rect(x, y, width, lineHeight),
                $"[{record.offsetMs:+0;-0;0}ms] pct={record.percentToNextBeat:F2} music={record.musicBeatIndex} beatIdx+1={record.beatIndexPlusOne} {validStr}",
                style);
            y += lineHeight;
        }

        y += sectionGap;

        // === Bug Analysis Section ===
        GUI.Label(new Rect(x, y, width, lineHeight), "=== Bug Analysis ===", labelStyle);
        y += lineHeight;

        if (beatHandler != null)
        {
            int musicBeat = beatHandler.GetMusicBeatIndex();
            GUI.Label(new Rect(x, y, width, lineHeight),
                $"FMOD says beat: {musicBeat}", labelStyle);
            y += lineHeight;
        }
    }

    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
