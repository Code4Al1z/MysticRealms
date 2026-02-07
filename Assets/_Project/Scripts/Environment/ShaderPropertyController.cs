using UnityEngine;
using System.Collections;

/// <summary>
/// Controls shader properties for the SoundReactiveShader to create fade/glow effects
/// without needing multiple materials. Works with crystals, bridges, and doors.
/// </summary>
public class ShaderPropertyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Effect Settings")]
    [SerializeField] private EffectType effectType = EffectType.Pulse;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Pulse Effect (For Crystals)")]
    [SerializeField] private Color pulseColor = new Color(0.15f, 0.98f, 0.05f, 1f);
    [SerializeField] private float pulseStrength = 1f;

    [Header("Alpha Clip Effect (For Bridges)")]
    [SerializeField] private bool disableOnFadeOut = true;

    private Material materialInstance;
    private Coroutine currentCoroutine;

    // Shader property name constants
    private static readonly int PulseProperty = Shader.PropertyToID("_Pulse");
    private static readonly int PulseStrengthProperty = Shader.PropertyToID("_PulseStrength");
    private static readonly int PulseColorProperty = Shader.PropertyToID("_PulseColor");
    private static readonly int OpacityProperty = Shader.PropertyToID("_Opacity");
    private static readonly int IsChangingColourProperty = Shader.PropertyToID("_IsChangingColour");

    public enum EffectType
    {
        Pulse,          // Animates Pulse (0→1) - Best for crystals
        Opacity,      // Animates Opacity (1→0 to fade in) - For bridges
        PulseAndColor   // Animates Pulse + enables color change - Advanced effect
    }

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer != null)
        {
            // Create material instance to avoid affecting other objects
            materialInstance = targetRenderer.material;
        }
        else
        {
            Debug.LogError($"ShaderPropertyController on {gameObject.name}: No Renderer found!", this);
        }
    }

    /// <summary>
    /// Fade in the effect (activate crystal, show bridge, etc.)
    /// </summary>
    public void FadeIn()
    {
        if (materialInstance == null) return;

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(FadeInCoroutine());
    }

    /// <summary>
    /// Fade out the effect (deactivate crystal, hide bridge, etc.)
    /// </summary>
    public void FadeOut()
    {
        if (materialInstance == null) return;

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(FadeOutCoroutine());
    }

    private IEnumerator FadeInCoroutine()
    {
        float elapsed = 0f;

        // Enable GameObject if it was disabled
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        switch (effectType)
        {
            case EffectType.Pulse:
                // Animate Pulse from 0 to 1
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fadeDuration;

                    materialInstance.SetFloat(PulseProperty, Mathf.Lerp(0f, 1f, t));
                    materialInstance.SetFloat(PulseStrengthProperty, pulseStrength);

                    yield return null;
                }
                materialInstance.SetFloat(PulseProperty, 1f);
                break;

            case EffectType.Opacity:
                // Animate Opacity from 1 to 0 (fade in)
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fadeDuration;
                    
                    // Start at 0.0 (fully clipped/invisible) and lerp to 1.0 (fully visible)
                    materialInstance.SetFloat(OpacityProperty, Mathf.Lerp(0f, 1f, t));

                    yield return null;
                }
                materialInstance.SetFloat(OpacityProperty, 1f);
                break;

            case EffectType.PulseAndColor:
                materialInstance.SetInt(IsChangingColourProperty, 1);
                materialInstance.SetColor(PulseColorProperty, pulseColor);

                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fadeDuration;

                    materialInstance.SetFloat(PulseProperty, Mathf.Lerp(0f, 1f, t));
                    materialInstance.SetFloat(PulseStrengthProperty, pulseStrength);

                    yield return null;
                }
                materialInstance.SetFloat(PulseProperty, 1f);
                break;
        }

        currentCoroutine = null;
    }

    private IEnumerator FadeOutCoroutine()
    {
        float elapsed = 0f;

        switch (effectType)
        {
            case EffectType.Pulse:
                // Animate Pulse from 1 to 0
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fadeDuration;

                    materialInstance.SetFloat(PulseProperty, Mathf.Lerp(1f, 0f, t));

                    yield return null;
                }
                materialInstance.SetFloat(PulseProperty, 0f);
                break;

            case EffectType.Opacity:
                // Animate Opacity from 0 to 1 (fade out)
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fadeDuration;

                    // Start at 1.0 (fully visible) and lerp to 0.0 (fully clipped/invisible)
                    materialInstance.SetFloat(OpacityProperty, Mathf.Lerp(1f, 0f, t));

                    yield return null;
                }
                materialInstance.SetFloat(OpacityProperty, 0f);

                if (disableOnFadeOut)
                {
                    gameObject.SetActive(false);
                }
                break;

            case EffectType.PulseAndColor:
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fadeDuration;

                    materialInstance.SetFloat(PulseProperty, Mathf.Lerp(1f, 0f, t));

                    yield return null;
                }
                materialInstance.SetFloat(PulseProperty, 0f);
                materialInstance.SetInt(IsChangingColourProperty, 0);
                break;
        }

        currentCoroutine = null;
    }

    /// <summary>
    /// Instantly set effect to active state (no animation)
    /// </summary>
    public void SetActiveInstant()
    {
        if (materialInstance == null) return;

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        gameObject.SetActive(true);

        switch (effectType)
        {
            case EffectType.Pulse:
            case EffectType.PulseAndColor:
                materialInstance.SetFloat(PulseProperty, 1f);
                materialInstance.SetFloat(PulseStrengthProperty, pulseStrength);
                if (effectType == EffectType.PulseAndColor)
                {
                    materialInstance.SetInt(IsChangingColourProperty, 1);
                    materialInstance.SetColor(PulseColorProperty, pulseColor);
                }
                break;

            case EffectType.Opacity:
                materialInstance.SetFloat(OpacityProperty, 1f);
                break;
        }
    }

    /// <summary>
    /// Instantly set effect to inactive state (no animation)
    /// </summary>
    public void SetInactiveInstant()
    {
        if (materialInstance == null) return;

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        switch (effectType)
        {
            case EffectType.Pulse:
            case EffectType.PulseAndColor:
                materialInstance.SetFloat(PulseProperty, 0f);
                if (effectType == EffectType.PulseAndColor)
                {
                    materialInstance.SetInt(IsChangingColourProperty, 0);
                }
                break;

            case EffectType.Opacity:
                materialInstance.SetFloat(OpacityProperty, 0f);
                if (disableOnFadeOut)
                {
                    gameObject.SetActive(false);
                }
                break;
        }
    }

    /// <summary>
    /// Directly set pulse strength without animation (for continuous updates)
    /// </summary>
    public void SetPulseStrengthDirect(float strength)
    {
        if (materialInstance == null) return;

        pulseStrength = strength;
        Debug.Log("PulseStrength set to: " + strength);
        materialInstance.SetFloat(PulseStrengthProperty, pulseStrength);
    }

    public void SetPulseActive(bool active)
    {
        if (materialInstance == null) return;

        materialInstance.SetFloat(PulseProperty, active ? 1f : 0f);
    }

    private void OnDestroy()
    {
        // Clean up material instance
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}