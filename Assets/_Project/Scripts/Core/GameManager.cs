using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level")]
    [SerializeField] private LevelData levelData;

    [Header("Main Menu")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public enum GameState { Playing, Paused, GameOver, Victory }

    public GameState CurrentState { get; private set; } = GameState.Playing;

    public event System.Action<GameState> OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
        {
            ph.OnGameOver += TriggerGameOver;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Pause()
    {
        if (CurrentState != GameState.Playing) return;
        SetState(GameState.Paused);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (CurrentState != GameState.Paused) return;
        SetState(GameState.Playing);
        Time.timeScale = 1f;
    }

    public void TriggerGameOver()
    {
        if (CurrentState == GameState.GameOver) return;
        SetState(GameState.GameOver);
        Time.timeScale = 0f;
    }

    public void TriggerVictory()
    {
        if (CurrentState == GameState.Victory) return;
        SetState(GameState.Victory);
        Time.timeScale = 0f;
    }

    public void RetryLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadNextLevel()
    {
        if (levelData == null || string.IsNullOrEmpty(levelData.nextSceneName))
        {
            Debug.LogWarning("[GameManager] No next scene name set in LevelData.");
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelData.nextSceneName);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public LevelData GetLevelData() => levelData;

    private void SetState(GameState next)
    {
        CurrentState = next;
        if (OnStateChanged != null)
            OnStateChanged.Invoke(next);
    }
}