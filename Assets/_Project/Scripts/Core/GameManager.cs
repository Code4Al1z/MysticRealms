using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level")]
    [SerializeField] private LevelData levelData;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public enum GameState { Playing, Paused, GameOver, Victory }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    public event System.Action<GameState> OnStateChanged;

    // The name of the game scene currently running
    private string currentGameSceneName;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        currentGameSceneName = SceneManager.GetActiveScene().name;
    }

    private void Start()
    {
        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
            ph.OnGameOver += TriggerGameOver;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Game State ────────────────────────────────────────────────────────────

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

    // ── Scene Transitions ─────────────────────────────────────────────────────

    public void RetryLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(currentGameSceneName);
    }

    public void LoadNextLevel()
    {
        if (levelData == null || string.IsNullOrEmpty(levelData.nextSceneName))
        {
            Debug.LogWarning("[GameManager] No next scene name in LevelData.");
            return;
        }
        Time.timeScale = 1f;
        StartCoroutine(TransitionToNextLevel(levelData.nextSceneName));
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 0f;
        StartCoroutine(LoadMainMenuAdditive());
    }

    public LevelData GetLevelData() => levelData;

    // ── Internal ──────────────────────────────────────────────────────────────

    private void SetState(GameState next)
    {
        CurrentState = next;
        OnStateChanged?.Invoke(next);
    }

    private IEnumerator LoadMainMenuAdditive()
    {
        // Only load MainMenu if it isn't already loaded
        Scene menuScene = SceneManager.GetSceneByName(mainMenuSceneName);
        if (!menuScene.IsValid() || !menuScene.isLoaded)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(
                mainMenuSceneName, LoadSceneMode.Additive);
            yield return load;
        }

        // Make MainMenu the active scene so its skybox takes effect
        SceneManager.SetActiveScene(
            SceneManager.GetSceneByName(mainMenuSceneName));
    }

    private IEnumerator TransitionToNextLevel(string nextSceneName)
    {
        // Load next scene additively first so there is no black frame
        AsyncOperation load = SceneManager.LoadSceneAsync(
            nextSceneName, LoadSceneMode.Additive);
        yield return load;

        // Unload current game scene
        AsyncOperation unload = SceneManager.UnloadSceneAsync(currentGameSceneName);
        yield return unload;

        currentGameSceneName = nextSceneName;
        SceneManager.SetActiveScene(
            SceneManager.GetSceneByName(nextSceneName));
        Time.timeScale = 1f;
    }
}