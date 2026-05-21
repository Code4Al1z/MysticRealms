using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string firstGameSceneName = "Scene1";

    [Header("Menu Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject audioSettingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button continueButton;

    // The game scene already loaded when this menu opened.
    // Null if this is a fresh startup with no prior game session.
    private string existingGameSceneName;
    private bool hasExistingGameSession;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        Time.timeScale = 0f;

        existingGameSceneName = FindExistingGameScene();
        hasExistingGameSession = !string.IsNullOrEmpty(existingGameSceneName);

        // Grey out Continue if there is nothing to continue
        if (continueButton != null)
            continueButton.interactable = hasExistingGameSession;

        SetPanel(mainPanel, true);
        SetPanel(audioSettingsPanel, false);

        // Music is handled by MusicManager which detects the scene automatically
    }

    // ── New Game ──────────────────────────────────────────────────────────────

    public void NewGame()
    {
        StartCoroutine(LoadFreshGame());
    }

    // ── Continue ──────────────────────────────────────────────────────────────

    public void ContinueGame()
    {
        if (!hasExistingGameSession)
        {
            NewGame();
            return;
        }

        StartCoroutine(ResumeExistingGame());
    }

    // ── Audio Settings ────────────────────────────────────────────────────────

    public void OpenAudioSettings(bool enable)
    {
        SetPanel(audioSettingsPanel, enable);
        SetPanel(mainPanel, !enable);
    }

    // ── Quit ─────────────────────────────────────────────────────────────────

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ── Private ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Unloads any existing game scene, loads Level1 fresh, unloads menu.
    /// </summary>
    private IEnumerator LoadFreshGame()
    {
        if (hasExistingGameSession)
            yield return SceneManager.UnloadSceneAsync(existingGameSceneName);

        yield return SceneManager.LoadSceneAsync(
            firstGameSceneName, LoadSceneMode.Additive);

        SceneManager.SetActiveScene(
            SceneManager.GetSceneByName(firstGameSceneName));

        SceneManager.UnloadSceneAsync(mainMenuSceneName);

        Time.timeScale = 1f;
    }

    /// <summary>
    /// Makes the frozen game scene active again and unloads the menu.
    /// Everything in the game scene is exactly where the player left it.
    /// </summary>
    private IEnumerator ResumeExistingGame()
    {
        SceneManager.SetActiveScene(
            SceneManager.GetSceneByName(existingGameSceneName));

        yield return SceneManager.UnloadSceneAsync(mainMenuSceneName);

        Time.timeScale = 1f;
    }

    /// <summary>
    /// Returns the name of any loaded scene that is not the MainMenu.
    /// Returns null if only the MainMenu is loaded (fresh startup, no game yet).
    /// </summary>
    private string FindExistingGameScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && s.name != mainMenuSceneName)
                return s.name;
        }
        return null;
    }

    private void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}