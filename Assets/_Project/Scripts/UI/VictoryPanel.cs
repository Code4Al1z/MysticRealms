using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VictoryPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private Button   continueButton;
    [SerializeField] private Button   mainMenuButton;

    private void Awake()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinue);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenu);
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null || summaryText == null) return;

        PlayerHealth ph = playerGO.GetComponent<PlayerHealth>();
        if (ph != null)
            summaryText.text = $"Collectables gathered: {ph.Collectables}";
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinue);
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(OnMainMenu);
    }

    private void OnContinue()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.LoadNextLevel();
    }
    private void OnMainMenu()
    { 
        if (GameManager.Instance == null) return;
        GameManager.Instance.LoadMainMenu();
    }
}
