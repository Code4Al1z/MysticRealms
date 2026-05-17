using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public void OnResume()
    { 
        if (GameManager.Instance == null) return;
        GameManager.Instance.Resume();
    }

    public void OnQuitToMenu()
    { 
        if (GameManager.Instance == null) return;
        GameManager.Instance.LoadMainMenu();
    }
}
