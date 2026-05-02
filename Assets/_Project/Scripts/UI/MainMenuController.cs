using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string firstGameSceneName = "Scene1";

    [Header("Audio Settings UI")]
    [SerializeField] private List<GameObject> objectsToDisable;
    [SerializeField] private List<GameObject> objectsToEnable;

    private void Start()
    {
        Time.timeScale = 0f; // Pause the game logic while in the main menu
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(mainMenuSceneName));
    }

    public void NewGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(firstGameSceneName);
    }

    public void ContinueGame()
    {
        Scene gameScene = GetBackgroundGameScene();

        if (gameScene.IsValid())
        {
            // Hand environmental control back to the game level
            SceneManager.SetActiveScene(gameScene);

            // Unfreeze the game logic
            Time.timeScale = 1f;

            // Remove the menu overlay
            SceneManager.UnloadSceneAsync(mainMenuSceneName);
        }
    }

    private Scene GetBackgroundGameScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && s.name != mainMenuSceneName)
                return s;
        }
        return default;
    }

    public void OpenAudioSettings()
    {
        objectsToDisable.ForEach(obj => obj.SetActive(false));
        objectsToEnable.ForEach(obj => obj.SetActive(true));
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}