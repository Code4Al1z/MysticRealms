using UnityEngine;
using UnityEngine.UI;

public class GameOverPanel : MonoBehaviour
{
    public void OnRetry()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.RetryLevel();
    }

    public void OnMainMenu()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.LoadMainMenu();
    }
}
