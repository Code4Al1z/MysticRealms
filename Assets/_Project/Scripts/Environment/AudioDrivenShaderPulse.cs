using UnityEngine;

/// <summary>
/// Drives shader Pulse property from Wwise RTPC values in real-time.
/// Perfect for making crystals/objects pulse with the player's Echo Pulse audio energy.
/// </summary>
public class AudioDrivenShaderPulse : MonoBehaviour
{
    [Header("Wwise Integration")]
    [Tooltip("The Wwise RTPC to read from (e.g., 'EchoPulse_Energy')")]
    [SerializeField] private AK.Wwise.RTPC audioEnergyRTPC;

    [Tooltip("The player GameObject that plays the Echo Pulse sound")]
    [SerializeField] private GameObject playerGameObject;

    [Header("Shader Mapping")]
    [Tooltip("Material to drive the Pulse property on (this crystal's material)")]
    [SerializeField] private Material targetMaterial;

    [Tooltip("Multiplier for RTPC value (if RTPC is 0-100, use 0.01 to get 0-1)")]
    [SerializeField] private float pulseMultiplier = 0.01f;

    [Tooltip("Smoothing speed for pulse changes (higher = more responsive)")]
    [SerializeField] private float smoothingSpeed = 10f;

    [Tooltip("How quickly pulse decays when no audio")]
    [SerializeField] private float decaySpeed = 3f;

    [Tooltip("Minimum pulse value (base glow level)")]
    [SerializeField] private float minPulse = 0.2f;

    [Tooltip("Maximum pulse value (peak glow)")]
    [SerializeField] private float maxPulse = 1.0f;

    [Header("Distance-Based Response")]
    [Tooltip("Crystal responds stronger when player is closer")]
    [SerializeField] private bool useDistanceAttenuation = true;

    [Tooltip("Maximum distance for audio response")]
    [SerializeField] private float maxResponseDistance = 15f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = false;

    private static readonly int PulseProperty = Shader.PropertyToID("_Pulse");
    private float currentPulse = 0f;
    private float targetPulse = 0f;

    private void Awake()
    {
        // Auto-find player if not assigned
        if (playerGameObject == null)
        {
            playerGameObject = GameObject.FindGameObjectWithTag("Player");

            if (playerGameObject == null)
            {
                Debug.LogWarning($"[AudioPulse] {name}: No player found! Assign Player GameObject or tag player with 'Player' tag.", this);
            }
        }

        if (targetMaterial == null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                targetMaterial = renderer.material; // Creates instance
            }
        }

        currentPulse = minPulse;
    }

    private void Update()
    {
        if (targetMaterial == null || audioEnergyRTPC == null || playerGameObject == null)
            return;

        // Get current RTPC value from player's Echo Pulse sound
        float rtpcValue = audioEnergyRTPC.GetValue(playerGameObject);

        // Apply distance attenuation if enabled
        float distanceFactor = 1f;
        if (useDistanceAttenuation)
        {
            float distance = Vector3.Distance(transform.position, playerGameObject.transform.position);
            distanceFactor = Mathf.Clamp01(1f - (distance / maxResponseDistance));
        }

        // Map RTPC value to pulse range with distance attenuation
        float mappedValue = rtpcValue * pulseMultiplier * distanceFactor;
        targetPulse = Mathf.Clamp(mappedValue, minPulse, maxPulse);

        // Smooth the pulse value (fast attack, slower decay)
        float speed = targetPulse > currentPulse ? smoothingSpeed : decaySpeed;
        currentPulse = Mathf.Lerp(currentPulse, targetPulse, Time.deltaTime * speed);

        // Apply to shader
        targetMaterial.SetFloat(PulseProperty, currentPulse);

        if (enableDebugLog && Time.frameCount % 30 == 0) // Log every 30 frames
        {
            Debug.Log($"[AudioPulse] {name}: RTPC={rtpcValue:F1}, Distance={distanceFactor:F2}, Pulse={currentPulse:F3}");
        }
    }

    /// <summary>
    /// Enable/disable audio-driven pulsing
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;

        if (!enabled && targetMaterial != null)
        {
            // Reset pulse to minimum when disabled
            currentPulse = minPulse;
            targetMaterial.SetFloat(PulseProperty, currentPulse);
        }
    }

    /// <summary>
    /// Manually set the player reference (useful if player spawns dynamically)
    /// </summary>
    public void SetPlayer(GameObject player)
    {
        playerGameObject = player;
    }

    private void OnDrawGizmosSelected()
    {
        if (useDistanceAttenuation)
        {
            // Draw response radius
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(transform.position, maxResponseDistance);
        }
    }
}