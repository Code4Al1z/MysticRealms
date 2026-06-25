using UnityEngine;
using UnityEngine.InputSystem;

public class GameHUD : MonoBehaviour
{
    [Header("Game Panels")]
    [SerializeField] private PlayerHealthPanel playerHealthPanel;
    [SerializeField] private AbilityPanel abilityPanel;
    [SerializeField] private CollectablePanel collectablePanel;
    [SerializeField] private TutorialMessage tutorialMessage;
    [SerializeField] private BossHealthBarPanel bossHealthBarPanel;

    [Header("Overlay Layers")]
    [SerializeField] private GameObject pauseLayer;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    [Header("Data")]
    [SerializeField] private LevelData levelData;

    private PlayerHealth playerHealth;
    private PlayerAbilities playerAbilities;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        if (PlayerHealth.Instance != null)
        {
            playerHealth = PlayerHealth.Instance;
            playerAbilities = playerHealth.GetComponent<PlayerAbilities>();

            if (playerHealthPanel != null) playerHealthPanel.Initialise(playerHealth);
            if (collectablePanel != null) collectablePanel.Initialise(playerHealth, levelData);
            if (abilityPanel != null && playerAbilities != null)
                abilityPanel.Initialise(playerAbilities);

            playerHealth.OnGameOver += OnGameOver;
        }
        else
        {
            Debug.LogWarning("[GameHUD] PlayerHealth.Instance is null - player not found in scene.");
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnStateChanged;

        HideAllOverlays();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnGameOver -= OnGameOver;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    // ─── Input ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    // ─── State ────────────────────────────────────────────────────────────────

    private void OnStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.Paused: ShowPause(); break;
            case GameManager.GameState.GameOver: ShowGameOver(); break;
            case GameManager.GameState.Victory: ShowVictory(); break;
            case GameManager.GameState.Playing: HideAllOverlays(); break;
        }
    }

    private void OnGameOver() => GameManager.Instance?.TriggerGameOver();

    // ─── Public API ───────────────────────────────────────────────────────────

    public void TogglePause()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            GameManager.Instance.Pause();
        else if (GameManager.Instance.CurrentState == GameManager.GameState.Paused)
            GameManager.Instance.Resume();
    }

    public void ShowTutorialMessage(string message, float duration)
    {
        if (tutorialMessage != null)
            tutorialMessage.Show(message, duration);
    }

    public void SetBossTarget(IEnemyDamageable boss)
    {
        if (bossHealthBarPanel != null)
            bossHealthBarPanel.SetTarget(boss);
    }

    public void UnlockEchoPulse()
    {
        if (abilityPanel != null)
            abilityPanel.UnlockEchoPulse();
    }

    public void UnlockResonanceHum()
    {
        if (abilityPanel != null)
            abilityPanel.UnlockResonanceHum();
    }

    // ─── Overlays ─────────────────────────────────────────────────────────────

    private void ShowPause()
    {
        if (pauseLayer != null)
            pauseLayer.SetActive(true);
    }

    private void ShowGameOver()
    {
        HideAllOverlays();
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    private void ShowVictory()
    {
        HideAllOverlays();
        if (victoryPanel != null)
            victoryPanel.SetActive(true);
    }

    private void HideAllOverlays()
    {
        if (pauseLayer != null) pauseLayer.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }
}