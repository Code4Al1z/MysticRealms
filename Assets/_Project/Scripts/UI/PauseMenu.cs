using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitToMenuButton;

    private void Awake()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResume);
        if (quitToMenuButton != null)
            quitToMenuButton.onClick.AddListener(OnQuitToMenu);
    }

    private void OnDestroy()
    {
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(OnResume);
        if (quitToMenuButton != null)
            quitToMenuButton.onClick.RemoveListener(OnQuitToMenu);
    }

    private void OnResume()
    { 
        if (GameManager.Instance == null) return;
        GameManager.Instance.Resume();
    }

    private void OnQuitToMenu()
    { 
        if (GameManager.Instance == null) return;
        GameManager.Instance.LoadMainMenu();
    }
}
