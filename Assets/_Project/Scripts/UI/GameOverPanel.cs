using UnityEngine;
using UnityEngine.UI;

public class GameOverPanel : MonoBehaviour
{
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    private void Awake()
    {
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetry);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenu);
    }

    private void OnDestroy()
    {
        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetry);
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(OnMainMenu);
    }

    private void OnRetry()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.RetryLevel();
    }

    private void OnMainMenu()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.LoadMainMenu();
    }
}
