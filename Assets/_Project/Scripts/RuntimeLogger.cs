using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RuntimeLogger : MonoBehaviour
{
    private string _logPath;
    private StreamWriter _writer;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        string fileName = $"MysticRealms_Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
        _logPath = Path.Combine(Application.persistentDataPath, fileName);
        _writer = new StreamWriter(_logPath, append: false) { AutoFlush = true };

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;

        WriteHeader();
        Application.logMessageReceived += OnLogMessage;

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        Log("RuntimeLogger initialised.");
        Log($"Persistent data path: {Application.persistentDataPath}");
        LogLoadedScenes("Awake");
    }

    void Start()
    {
        LogLoadedScenes("Start");
        StartCoroutine(DiagnoseButtonsDelayed());
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        Application.logMessageReceived -= OnLogMessage;
        _writer?.Close();
    }

    void OnApplicationQuit()
    {
        _writer?.WriteLine($"\n[{Timestamp()}] === Application quit cleanly ===");
        _writer?.Close();
    }

    // ─── Scene events ────────────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Log($"SCENE LOADED   → [{scene.buildIndex}] \"{scene.name}\"  mode: {mode}  isLoaded: {scene.isLoaded}");
        LogLoadedScenes("after load");
        // Re-run button diagnosis whenever a new scene comes in
        StartCoroutine(DiagnoseButtonsDelayed());
    }

    private void OnSceneUnloaded(Scene scene)
    {
        Log($"SCENE UNLOADED → [{scene.buildIndex}] \"{scene.name}\"");
        LogLoadedScenes("after unload");
    }

    private void LogLoadedScenes(string context)
    {
        int count = SceneManager.loadedSceneCount;
        Log($"Loaded scenes ({context}): {count} total");
        for (int i = 0; i < count; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            Log($"  [{s.buildIndex}] \"{s.name}\"  isLoaded:{s.isLoaded}  rootObjects:{s.rootCount}");
        }
    }

    // ─── Button diagnosis ─────────────────────────────────────────────────────

    // Wait one frame so all Start() methods have finished before scanning
    private IEnumerator DiagnoseButtonsDelayed()
    {
        yield return null;
        DiagnoseButtons();
        DiagnoseEventSystem();
        DiagnoseCanvas();
    }

    private void DiagnoseButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Log($"BUTTONS FOUND: {buttons.Length}");

        foreach (Button btn in buttons)
        {
            bool interactable = btn.interactable;
            bool activeInScene = btn.gameObject.activeInHierarchy;
            bool hasOnClick = btn.onClick.GetPersistentEventCount() > 0;
            bool raycastBlocked = IsRaycastBlocked(btn);
            CanvasGroup cg = btn.GetComponentInParent<CanvasGroup>();
            bool cgBlocks = cg != null && (!cg.interactable || !cg.blocksRaycasts);
            string scene = btn.gameObject.scene.name;

            Log($"  Button \"{btn.name}\" [{scene}]" +
                $"  active:{activeInScene}" +
                $"  interactable:{interactable}" +
                $"  hasOnClick:{hasOnClick}" +
                $"  raycastBlocked:{raycastBlocked}" +
                $"  canvasGroupIssue:{cgBlocks}");

            if (!interactable)
                Log($"    !! \"{btn.name}\" is NOT interactable");
            if (!hasOnClick)
                Log($"    !! \"{btn.name}\" has no onClick listeners");
            if (raycastBlocked)
                Log($"    !! \"{btn.name}\" is blocked by a raycast-blocking overlay");
            if (cgBlocks)
                Log($"    !! \"{btn.name}\" parent CanvasGroup blocks interaction  interactable:{cg.interactable}  blocksRaycasts:{cg.blocksRaycasts}");
        }
    }

    // Checks whether a transparent/invisible graphic is sitting on top and eating clicks
    private bool IsRaycastBlocked(Button btn)
    {
        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt == null) return false;

        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector2 centre = (corners[0] + corners[2]) / 2f;

        PointerEventData ped = new PointerEventData(EventSystem.current) { position = centre };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current?.RaycastAll(ped, results);

        if (results.Count == 0) return false;

        // If the topmost hit isn't the button itself or one of its children, something is blocking it
        GameObject top = results[0].gameObject;
        return top != btn.gameObject && !top.transform.IsChildOf(btn.transform);
    }

    private void DiagnoseEventSystem()
    {
        EventSystem es = EventSystem.current;
        if (es == null)
        {
            Log("!! NO ACTIVE EventSystem FOUND — this will break all UI input");
            return;
        }
        Log($"EventSystem: \"{es.name}\"  enabled:{es.enabled}  scene:{es.gameObject.scene.name}");

        var inputModule = es.currentInputModule;
        Log($"  InputModule: {(inputModule != null ? inputModule.GetType().Name : "NONE")}  enabled:{inputModule?.enabled}");
    }

    private void DiagnoseCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Log($"CANVASES FOUND: {canvases.Length}");

        foreach (Canvas c in canvases)
        {
            GraphicRaycaster gr = c.GetComponent<GraphicRaycaster>();
            Log($"  Canvas \"{c.name}\" [{c.gameObject.scene.name}]" +
                $"  active:{c.gameObject.activeInHierarchy}" +
                $"  renderMode:{c.renderMode}" +
                $"  hasGraphicRaycaster:{gr != null}" +
                $"  raycasterEnabled:{gr?.enabled}");

            if (gr == null)
                Log($"    !! Canvas \"{c.name}\" has no GraphicRaycaster — buttons inside it cannot receive clicks");
        }
    }

    // ─── Log message receiver ─────────────────────────────────────────────────

    private void OnLogMessage(string message, string stackTrace, LogType type)
    {
        string prefix = type switch
        {
            LogType.Error => "[ERROR]  ",
            LogType.Exception => "[EXCEPT] ",
            LogType.Warning => "[WARN]   ",
            LogType.Assert => "[ASSERT] ",
            _ => "[INFO]   ",
        };

        _writer?.WriteLine($"[{Timestamp()}] {prefix} {message}");

        if (type == LogType.Error || type == LogType.Exception)
            if (!string.IsNullOrEmpty(stackTrace))
                _writer?.WriteLine($"           Stack: {stackTrace.Replace("\n", "\n           ")}");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void Log(string message) =>
        _writer?.WriteLine($"[{Timestamp()}] [DIAG]   {message}");

    private void WriteHeader()
    {
        _writer.WriteLine("=================================================");
        _writer.WriteLine($"  Mystic Realms — Runtime Log");
        _writer.WriteLine($"  Session: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _writer.WriteLine($"  Platform: {Application.platform}");
        _writer.WriteLine($"  Unity: {Application.unityVersion}");
        _writer.WriteLine($"  OS: {SystemInfo.operatingSystem}");
        _writer.WriteLine($"  GPU: {SystemInfo.graphicsDeviceName}");
        _writer.WriteLine($"  VRAM: {SystemInfo.graphicsMemorySize} MB");
        _writer.WriteLine($"  RAM: {SystemInfo.systemMemorySize} MB");
        _writer.WriteLine($"  CPU: {SystemInfo.processorType}");
        _writer.WriteLine("=================================================\n");
    }

    private string Timestamp() => DateTime.Now.ToString("HH:mm:ss.fff");
}