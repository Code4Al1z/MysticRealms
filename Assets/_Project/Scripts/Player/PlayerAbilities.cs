using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player sound-based abilities: Echo Pulse and Resonance Hum
/// Echo Pulse: Hold E to emit continuous sound wave that charges crystals with frequency
/// Resonance Hum: Hold R to sustain tone that keeps platforms solid/doors open
/// </summary>
public class PlayerAbilities : MonoBehaviour
{
    [Header("Echo Pulse Settings")]
    [Tooltip("Radius of the echo pulse effect")]
    [SerializeField] private float echoPulseRadius = 10f;

    [Tooltip("Layer mask for objects that can be activated by echo pulse")]
    [SerializeField] private LayerMask echoTargetLayer;

    [Header("Resonance Hum Settings")]
    [Tooltip("Maximum duration player can hold resonance hum in seconds")]
    [SerializeField] private float maxResonanceDuration = 8f;

    [Tooltip("How fast resonance energy recharges (units per second)")]
    [SerializeField] private float resonanceRechargeRate = 1f;

    [Tooltip("Delay before recharge starts after releasing hum (seconds)")]
    [SerializeField] private float resonanceRechargeDelay = 1f;

    [Header("Visual Effects")]
    [Tooltip("Prefab for echo pulse visual effect (blue ripple)")]
    [SerializeField] private GameObject echoPulseVFXPrefab;

    [Tooltip("Prefab for resonance hum visual effect (orange glow)")]
    [SerializeField] private GameObject resonanceHumVFXPrefab;

    [Header("Wwise Events")]
    [SerializeField] private AK.Wwise.Event echoPulseStartEvent;
    [SerializeField] private AK.Wwise.Event echoPulseStopEvent;
    [SerializeField] private AK.Wwise.Event resonanceHumStartEvent;
    [SerializeField] private AK.Wwise.Event resonanceHumStopEvent;

    [Header("Wwise RTPCs")]
    [Tooltip("RTPC for echo pulse frequency (Hz)")]
    [SerializeField] private AK.Wwise.RTPC echoPulseFrequencyRTPC;

    [Tooltip("RTPC for resonance intensity (0-100)")]
    [SerializeField] private AK.Wwise.RTPC resonanceIntensityRTPC;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool enableDebugLog = false;

    // Echo Pulse state
    private bool isEchoPulseActive = false;
    private float currentEchoPulseFrequency = 100f; // Start low
    private float echoPulseFrequencyRampSpeed = 50f; // Hz per second
    private GameObject activeEchoPulseVFX;
    private uint echoPulsePlayingID = 0;

    // Resonance Hum state
    private bool isResonanceHumActive = false;
    private float currentResonanceEnergy;
    private float timeSinceResonanceStop = 0f;
    private GameObject activeResonanceVFX;

    private void Start()
    {
        currentResonanceEnergy = maxResonanceDuration;
        currentEchoPulseFrequency = 100f;
    }

    private void Update()
    {
        HandleInput();
        UpdateEchoPulse();
        UpdateResonanceHum();
        HandleResonanceRecharge();
    }

    private void HandleInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Echo Pulse - Hold E
        if (kb.eKey != null)
        {
            if (kb.eKey.isPressed && !isEchoPulseActive)
            {
                StartEchoPulse();
            }
            else if (!kb.eKey.isPressed && isEchoPulseActive)
            {
                StopEchoPulse();
            }
        }

        // Resonance Hum - Hold R
        if (kb.rKey != null)
        {
            if (kb.rKey.isPressed && !isResonanceHumActive && currentResonanceEnergy > 0f)
            {
                StartResonanceHum();
            }
            else if (!kb.rKey.isPressed && isResonanceHumActive)
            {
                StopResonanceHum();
            }
        }
    }

    #region Echo Pulse

    /// <summary>
    /// Starts the Echo Pulse - begins ramping up frequency
    /// </summary>
    private void StartEchoPulse()
    {
        isEchoPulseActive = true;
        currentEchoPulseFrequency = 100f; // Reset to low frequency

        // Play Wwise event (continuous loop)
        if (echoPulseStartEvent != null)
        {
            echoPulsePlayingID = echoPulseStartEvent.Post(gameObject);
        }

        // Spawn visual effect
        if (echoPulseVFXPrefab != null && activeEchoPulseVFX == null)
        {
            activeEchoPulseVFX = Instantiate(echoPulseVFXPrefab, transform);
        }

        if (enableDebugLog)
            Debug.Log("[EchoPulse] Started - frequency ramping up");
    }

    /// <summary>
    /// Updates Echo Pulse - ramps frequency and notifies crystals
    /// </summary>
    private void UpdateEchoPulse()
    {
        if (!isEchoPulseActive) return;

        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            echoPulseRadius,
            echoTargetLayer);

        IEchoResponsive closestTarget = null;
        float closestDistance = float.MaxValue;

        // Find closest crystal
        foreach (Collider col in hitColliders)
        {
            IEchoResponsive target = col.GetComponent<IEchoResponsive>();
            if (target == null) continue;

            float distance = Vector3.Distance(transform.position, col.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }

        // If we found one, steer frequency toward it
        if (closestTarget is ActivatableCrystal crystal)
        {
            float targetFreq = crystal.GetRequiredFrequency();

            currentEchoPulseFrequency = Mathf.MoveTowards(
                currentEchoPulseFrequency,
                targetFreq,
                echoPulseFrequencyRampSpeed * Time.deltaTime);
        }

        // Update Wwise
        echoPulseFrequencyRTPC?.SetValue(gameObject, currentEchoPulseFrequency);

        // Notify ALL (so mechanics still work if many)
        foreach (Collider col in hitColliders)
        {
            IEchoResponsive target = col.GetComponent<IEchoResponsive>();
            if (target == null) continue;

            float distance = Vector3.Distance(transform.position, col.transform.position);
            target.OnEchoPulseActive(transform.position, distance, currentEchoPulseFrequency);
        }
    }

    /// <summary>
    /// Stops the Echo Pulse
    /// </summary>
    private void StopEchoPulse()
    {
        if (!isEchoPulseActive) return;

        isEchoPulseActive = false;

        // Stop Wwise event
        if (echoPulseStopEvent != null)
        {
            echoPulseStopEvent.Post(gameObject);
        }
        else if (echoPulsePlayingID != 0)
        {
            AkUnitySoundEngine.StopPlayingID(echoPulsePlayingID);
        }

        echoPulsePlayingID = 0;

        // Destroy visual effect
        if (activeEchoPulseVFX != null)
        {
            Destroy(activeEchoPulseVFX);
            activeEchoPulseVFX = null;
        }

        // Notify all crystals that echo pulse stopped
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, echoPulseRadius, echoTargetLayer);
        foreach (Collider col in hitColliders)
        {
            IEchoResponsive echoTarget = col.GetComponent<IEchoResponsive>();
            if (echoTarget != null)
            {
                echoTarget.OnEchoPulseStopped();
            }
        }

        if (enableDebugLog)
            Debug.Log($"[EchoPulse] Stopped at frequency {currentEchoPulseFrequency:F0}Hz");
    }

    /// <summary>
    /// Gets current echo pulse frequency
    /// </summary>
    public float GetEchoPulseFrequency()
    {
        return isEchoPulseActive ? currentEchoPulseFrequency : 0f;
    }

    /// <summary>
    /// Gets whether echo pulse is active
    /// </summary>
    public bool IsEchoPulseActive()
    {
        return isEchoPulseActive;
    }

    #endregion

    #region Resonance Hum

    /// <summary>
    /// Starts the Resonance Hum ability
    /// </summary>
    private void StartResonanceHum()
    {
        isResonanceHumActive = true;
        timeSinceResonanceStop = 0f;

        // Play Wwise start event
        if (resonanceHumStartEvent != null)
        {
            resonanceHumStartEvent.Post(gameObject);
        }

        // Spawn visual effect
        if (resonanceHumVFXPrefab != null && activeResonanceVFX == null)
        {
            activeResonanceVFX = Instantiate(resonanceHumVFXPrefab, transform);
        }

        if (enableDebugLog)
            Debug.Log("[ResonanceHum] Started");
    }

    /// <summary>
    /// Updates Resonance Hum while active - drains energy and updates RTPC
    /// </summary>
    private void UpdateResonanceHum()
    {
        if (!isResonanceHumActive) return;

        // Drain energy
        currentResonanceEnergy -= Time.deltaTime;

        // Calculate intensity (0-100) for Wwise RTPC
        float intensityPercent = (1f - (currentResonanceEnergy / maxResonanceDuration)) * 100f;
        intensityPercent = Mathf.Clamp(intensityPercent, 0f, 100f);

        // Update Wwise RTPC
        if (resonanceIntensityRTPC != null)
        {
            resonanceIntensityRTPC.SetValue(gameObject, intensityPercent);
        }

        // Notify all resonance-responsive objects
        NotifyResonanceTargets(true);

        // Force stop if energy depleted
        if (currentResonanceEnergy <= 0f)
        {
            currentResonanceEnergy = 0f;
            StopResonanceHum();
        }
    }

    /// <summary>
    /// Stops the Resonance Hum ability
    /// </summary>
    private void StopResonanceHum()
    {
        if (!isResonanceHumActive) return;

        isResonanceHumActive = false;
        timeSinceResonanceStop = 0f;

        // Play Wwise stop event
        if (resonanceHumStopEvent != null)
        {
            resonanceHumStopEvent.Post(gameObject);
        }

        // Reset RTPC
        if (resonanceIntensityRTPC != null)
        {
            resonanceIntensityRTPC.SetValue(gameObject, 0f);
        }

        // Destroy visual effect
        if (activeResonanceVFX != null)
        {
            Destroy(activeResonanceVFX);
            activeResonanceVFX = null;
        }

        // Notify targets that resonance stopped
        NotifyResonanceTargets(false);

        if (enableDebugLog)
            Debug.Log($"[ResonanceHum] Stopped. Energy remaining: {currentResonanceEnergy:F1}s");
    }

    /// <summary>
    /// Handles energy recharge after resonance hum stops
    /// </summary>
    private void HandleResonanceRecharge()
    {
        if (isResonanceHumActive || currentResonanceEnergy >= maxResonanceDuration)
            return;

        timeSinceResonanceStop += Time.deltaTime;

        // Start recharging after delay
        if (timeSinceResonanceStop >= resonanceRechargeDelay)
        {
            currentResonanceEnergy += resonanceRechargeRate * Time.deltaTime;
            currentResonanceEnergy = Mathf.Min(currentResonanceEnergy, maxResonanceDuration);
        }
    }

    /// <summary>
    /// Notifies all resonance-responsive objects about resonance state
    /// </summary>
    private void NotifyResonanceTargets(bool isActive)
    {
        // Find all objects in scene that implement IResonanceResponsive
        IResonanceResponsive[] targets = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IResonanceResponsive>()
            .ToArray();

        foreach (var target in targets)
        {
            if (isActive)
            {
                float distance = Vector3.Distance(transform.position, ((MonoBehaviour)target).transform.position);
                target.OnResonanceHumActive(transform.position, distance);
            }
            else
            {
                target.OnResonanceHumStopped();
            }
        }
    }

    /// <summary>
    /// Gets current resonance energy as percentage (0-1)
    /// </summary>
    public float GetResonanceEnergyPercent()
    {
        return currentResonanceEnergy / maxResonanceDuration;
    }

    /// <summary>
    /// Gets whether resonance hum is currently active
    /// </summary>
    public bool IsResonanceActive()
    {
        return isResonanceHumActive;
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw echo pulse radius
        Gizmos.color = isEchoPulseActive ? new Color(0f, 1f, 1f, 0.5f) : new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, echoPulseRadius);

        // Draw resonance hum indicator
        if (isResonanceHumActive)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
    }
}

/// <summary>
/// Interface for objects that respond to Echo Pulse
/// </summary>
public interface IEchoResponsive
{
    /// <summary>
    /// Called continuously while Echo Pulse is active and in range
    /// </summary>
    /// <param name="sourcePosition">Player position</param>
    /// <param name="distance">Distance from player</param>
    /// <param name="frequency">Current Echo Pulse frequency in Hz</param>
    void OnEchoPulseActive(Vector3 sourcePosition, float distance, float frequency);

    /// <summary>
    /// Called when Echo Pulse stops or crystal moves out of range
    /// </summary>
    void OnEchoPulseStopped();

    /// <summary>
    /// Gets the required frequency for the crystal to activate
    /// </summary>
    /// <returns></returns>
    float GetRequiredFrequency();
}

/// <summary>
/// Interface for objects that respond to Resonance Hum
/// </summary>
public interface IResonanceResponsive
{
    void OnResonanceHumActive(Vector3 sourcePosition, float distance);
    void OnResonanceHumStopped();
}