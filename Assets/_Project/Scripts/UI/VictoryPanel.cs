using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VictoryPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text summaryText;

    private void OnEnable()
    {
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null || summaryText == null) return;

        PlayerHealth ph = playerGO.GetComponent<PlayerHealth>();
        if (ph != null)
            summaryText.text = $"Collectables gathered: {ph.Collectables}";
    }

    public void OnContinue()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.LoadNextLevel();
    }
    public void OnMainMenu()
    { 
        if (GameManager.Instance == null) return;
        GameManager.Instance.LoadMainMenu();
    }
}
