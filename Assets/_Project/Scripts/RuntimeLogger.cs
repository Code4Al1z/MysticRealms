using System;
using System.IO;
using UnityEngine;

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

        WriteHeader();

        Application.logMessageReceived += OnLogMessage;

        Debug.Log("[RuntimeLogger] Logger initialised. Writing to: " + _logPath);
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= OnLogMessage;
        _writer?.Close();
    }

    void OnApplicationQuit()
    {
        _writer?.WriteLine($"\n[{Timestamp()}] === Application quit cleanly ===");
        _writer?.Close();
    }

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

        // For errors and exceptions, also write the stack trace
        if (type == LogType.Error || type == LogType.Exception)
        {
            if (!string.IsNullOrEmpty(stackTrace))
                _writer?.WriteLine($"           Stack: {stackTrace.Replace("\n", "\n           ")}");
        }
    }

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