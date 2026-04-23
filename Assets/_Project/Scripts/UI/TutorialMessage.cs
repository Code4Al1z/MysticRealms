using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TutorialMessage : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image    backgroundPanel;
    [SerializeField] private float    fadeDuration = 0.3f;

    private Coroutine activeRoutine;

    private void Awake()
    {
        SetAlpha(0f);
        gameObject.SetActive(false);
    }

    public void Show(string message, float displayDuration)
    {
        if (messageText != null)
            messageText.text = message;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        gameObject.SetActive(true);
        activeRoutine = StartCoroutine(ShowRoutine(displayDuration));
    }

    public void Hide()
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(FadeOut());
    }

    private IEnumerator ShowRoutine(float displayDuration)
    {
        yield return FadeIn();

        if (displayDuration > 0f)
        {
            yield return new WaitForSeconds(displayDuration);
            yield return FadeOut();
        }
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        SetAlpha(1f);
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(1f - elapsed / fadeDuration));
            yield return null;
        }
        SetAlpha(0f);
        gameObject.SetActive(false);
    }

    private void SetAlpha(float a)
    {
        if (messageText != null)
        {
            Color c = messageText.color;
            c.a = a;
            messageText.color = c;
        }

        if (backgroundPanel != null)
        {
            Color c = backgroundPanel.color;
            c.a = a * 0.85f;
            backgroundPanel.color = c;
        }
    }
}
