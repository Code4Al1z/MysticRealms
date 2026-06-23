using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sits on the Settings panel. Wires DisplayToggle and DisplayDropdown
/// to DisplayManager, and hides the resolution row when in fullscreen.
/// </summary>
public class DisplaySettingsController : MonoBehaviour
{
    [Header("Display controls")]
    [SerializeField] private DisplayToggle   fullscreenToggle;
    [SerializeField] private DisplayDropdown resolutionDropdown;

    [Tooltip("The entire resolution row (label + dropdown) — hidden when fullscreen.")]
    [SerializeField] private GameObject resolutionRow;

    // ─── Resolution presets (must match Resolutions array order) ─────────────

    private static readonly List<string> ResolutionLabels = new()
    {
        "960 \u00d7 540",
        "1280 \u00d7 720",
        "1600 \u00d7 900",
        "1920 \u00d7 1080",
        "2560 \u00d7 1440",
    };

    private static readonly (int w, int h)[] Resolutions =
    {
        (960,  540),
        (1280, 720),
        (1600, 900),
        (1920, 1080),
        (2560, 1440),
    };

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    void Start()
    {
        if (DisplayManager.Instance == null)
        {
            Debug.LogError("[DisplaySettingsController] DisplayManager not found. " +
                           "Make sure it exists on a persistent GameObject.");
            return;
        }

        // Restore UI state to match current display settings
        fullscreenToggle.SetWithoutNotify(DisplayManager.Instance.IsFullscreen);
        resolutionDropdown.SetOptions(ResolutionLabels, GetCurrentResolutionIndex());
        UpdateResolutionRowVisibility();

        // Subscribe
        fullscreenToggle.OnValueChanged     += OnFullscreenToggled;
        resolutionDropdown.OnOptionSelected += OnResolutionSelected;
    }

    void OnDestroy()
    {
        if (fullscreenToggle   != null) fullscreenToggle.OnValueChanged     -= OnFullscreenToggled;
        if (resolutionDropdown != null) resolutionDropdown.OnOptionSelected -= OnResolutionSelected;
    }

    // ─── Handlers ─────────────────────────────────────────────────────────────

    private void OnFullscreenToggled(bool isOn)
    {
        if (isOn) DisplayManager.Instance.SetFullscreen();
        else      DisplayManager.Instance.SetWindowed();
        UpdateResolutionRowVisibility();
    }

    private void OnResolutionSelected(int index)
    {
        var (w, h) = Resolutions[index];
        DisplayManager.Instance.SetWindowedResolution(w, h);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void UpdateResolutionRowVisibility()
    {
        if (resolutionRow != null)
            resolutionRow.SetActive(!DisplayManager.Instance.IsFullscreen);
    }

    /// <summary>Find the preset index matching the current screen width, defaulting to 1280x720.</summary>
    private int GetCurrentResolutionIndex()
    {
        int w = Screen.width;
        for (int i = 0; i < Resolutions.Length; i++)
            if (Resolutions[i].w == w) return i;
        return 1; // 1280x720 fallback
    }
}
