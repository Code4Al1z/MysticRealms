using System;
using System.Collections;
using UnityEngine;

public class DisplayManager : MonoBehaviour
{
    // ─── Config ───────────────────────────────────────────────────────────────

    private const float TargetAspect      = 16f / 9f;
    private const int   MinWindowedWidth  = 960;
    private const int   MinWindowedHeight = 540;
    private const float ResizeCheckInterval = 0.25f;

    // ─── State ────────────────────────────────────────────────────────────────

    private FullScreenMode _currentMode;
    private int            _windowedWidth;
    private int            _windowedHeight;
    private int            _lastCheckedWidth;
    private int            _lastCheckedHeight;
    private Coroutine      _resizeWatcher;

    // ─── Singleton ────────────────────────────────────────────────────────────

    public static DisplayManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _windowedWidth  = PlayerPrefs.GetInt("WindowedWidth",  1280);
        _windowedHeight = PlayerPrefs.GetInt("WindowedHeight", 720);
        _currentMode    = (FullScreenMode)PlayerPrefs.GetInt(
                              "FullscreenMode", (int)FullScreenMode.Windowed);

        ApplyDisplayMode(_currentMode, _windowedWidth, _windowedHeight);
    }

    void OnDestroy()
    {
        if (_resizeWatcher != null) StopCoroutine(_resizeWatcher);
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Toggle between windowed and fullscreen on the current monitor.</summary>
    public void ToggleFullscreen()
    {
        if (_currentMode == FullScreenMode.Windowed)
            SetFullscreen();
        else
            SetWindowed();
    }

    /// <summary>Go fullscreen on the current monitor at its native resolution.</summary>
    public void SetFullscreen()
    {
        if (_currentMode == FullScreenMode.Windowed)
        {
            _windowedWidth  = Screen.width;
            _windowedHeight = Screen.height;
            SaveWindowedPrefs();
        }

        if (Screen.currentResolution.height > Screen.currentResolution.width)
            Debug.LogWarning("[DisplayManager] Portrait monitor detected — fullscreen may not look great.");

        ApplyDisplayMode(FullScreenMode.FullScreenWindow,
                         Screen.currentResolution.width,
                         Screen.currentResolution.height);
    }

    /// <summary>Return to the last saved windowed size.</summary>
    public void SetWindowed()
    {
        ApplyDisplayMode(FullScreenMode.Windowed, _windowedWidth, _windowedHeight);
    }

    /// <summary>Set a specific windowed resolution, snapped to 16:9.</summary>
    public void SetWindowedResolution(int width, int height)
    {
        (width, height) = SnapToAspect(width, height);
        width  = Mathf.Max(width,  MinWindowedWidth);
        height = Mathf.Max(height, MinWindowedHeight);
        _windowedWidth  = width;
        _windowedHeight = height;
        SaveWindowedPrefs();
        ApplyDisplayMode(FullScreenMode.Windowed, width, height);
    }

    /// <summary>Returns true if currently in any fullscreen mode.</summary>
    public bool IsFullscreen => _currentMode != FullScreenMode.Windowed;

    /// <summary>Returns true if the current display appears to be portrait orientation.</summary>
    public bool IsPortraitMonitor =>
        Screen.currentResolution.height > Screen.currentResolution.width;

    // ─── Windowed presets ─────────────────────────────────────────────────────

    public void SetWindowed960x540()   => SetWindowedResolution(960,  540);
    public void SetWindowed1280x720()  => SetWindowedResolution(1280, 720);
    public void SetWindowed1600x900()  => SetWindowedResolution(1600, 900);
    public void SetWindowed1920x1080() => SetWindowedResolution(1920, 1080);
    public void SetWindowed2560x1440() => SetWindowedResolution(2560, 1440);

    // ─── Internal ─────────────────────────────────────────────────────────────

    private void ApplyDisplayMode(FullScreenMode mode, int width, int height)
    {
        Screen.SetResolution(width, height, mode);
        _currentMode = mode;
        PlayerPrefs.SetInt("FullscreenMode", (int)mode);
        PlayerPrefs.Save();

        Debug.Log($"[DisplayManager] {mode}  {width}x{height}");

        if (_resizeWatcher != null) StopCoroutine(_resizeWatcher);
        if (mode == FullScreenMode.Windowed)
            _resizeWatcher = StartCoroutine(WatchForFreeResize());
    }

    private IEnumerator WatchForFreeResize()
    {
        var wait = new WaitForSecondsRealtime(ResizeCheckInterval);
        _lastCheckedWidth  = Screen.width;
        _lastCheckedHeight = Screen.height;

        while (true)
        {
            yield return wait;

            int w = Screen.width;
            int h = Screen.height;

            if (w == _lastCheckedWidth && h == _lastCheckedHeight)
                continue;

            (int sw, int sh) = SnapToAspect(w, h);
            sw = Mathf.Max(sw, MinWindowedWidth);
            sh = Mathf.Max(sh, MinWindowedHeight);

            if (sw != w || sh != h)
                Screen.SetResolution(sw, sh, FullScreenMode.Windowed);

            _windowedWidth     = sw;
            _windowedHeight    = sh;
            _lastCheckedWidth  = sw;
            _lastCheckedHeight = sh;

            SaveWindowedPrefs();
        }
    }

    private static (int w, int h) SnapToAspect(int width, int height)
    {
        int snappedHeight = Mathf.RoundToInt(width / TargetAspect);
        return (width, snappedHeight);
    }

    private void SaveWindowedPrefs()
    {
        PlayerPrefs.SetInt("WindowedWidth",  _windowedWidth);
        PlayerPrefs.SetInt("WindowedHeight", _windowedHeight);
        PlayerPrefs.Save();
    }
}
