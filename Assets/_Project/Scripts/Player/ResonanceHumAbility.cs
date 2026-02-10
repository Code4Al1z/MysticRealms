using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Resonance Hum: Hold R to sustain tone that keeps platforms solid/doors open
/// Has a time limit and requires recharge after use
/// </summary>
public class ResonanceHumAbility : MonoBehaviour
{
    [Header("Resonance Hum Settings")]
    [Tooltip("Maximum duration player can hold resonance hum in seconds")]
    [SerializeField] private float maxDuration = 8f;

    [Tooltip("How fast resonance energy recharges (units per second)")]
    [SerializeField] private float rechargeRate = 1f;

    [Tooltip("Delay before recharge starts after releasing hum (seconds)")]
    [SerializeField] private float rechargeDelay = 1f;

    [Header("Visual Effects")]
    [Tooltip("Prefab for resonance hum visual effect (orange glow)")]
    [SerializeField] private GameObject resonanceHumVFXPrefab;

    [Header("Wwise Events")]
    [SerializeField] private AK.Wwise.Event resonanceHumStartEvent;
    [SerializeField] private AK.Wwise.Event resonanceHumStopEvent;
    [SerializeField] private AK.Wwise.Event resonanceDepletedEvent;

    [Header("Wwise RTPCs")]
    [Tooltip("RTPC for resonance intensity (0-100)")]
    [SerializeField] private AK.Wwise.RTPC resonanceIntensityRTPC;

    [Tooltip("RTPC for resonance energy remaining (0-100)")]
    [SerializeField] private AK.Wwise.RTPC resonanceEnergyRTPC;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool enableDebugLog = false;

    private bool isActive = false;
    private float currentEnergy;
    private float timeSinceStop = 0f;
    private GameObject activeVFX;

    public delegate void OnEnergyChanged(float energyPercent);
    public event OnEnergyChanged OnEnergyChangedEvent;

    private void Start()
    {
        currentEnergy = maxDuration;
    }

    private void Update()
    {
        HandleInput();
        if (isActive)
        {
            UpdateResonanceHum();
        }
        else
        {
            HandleRecharge();
        }
    }

    private void HandleInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.rKey != null)
        {
            if (kb.rKey.isPressed && !isActive && currentEnergy > 0f)
            {
                StartResonanceHum();
            }
            else if (!kb.rKey.isPressed && isActive)
            {
                StopResonanceHum();
            }
        }
    }

    private void StartResonanceHum()
    {
        isActive = true;
        timeSinceStop = 0f;

        if (resonanceHumStartEvent != null)
        {
            resonanceHumStartEvent.Post(gameObject);
        }

        if (resonanceHumVFXPrefab != null && activeVFX == null)
        {
            activeVFX = Instantiate(resonanceHumVFXPrefab, transform);
        }

        if (enableDebugLog)
            Debug.Log("[ResonanceHum] Started");
    }

    private void UpdateResonanceHum()
    {
        currentEnergy -= Time.deltaTime;

        float energyPercent = currentEnergy / maxDuration;
        float intensityPercent = (1f - energyPercent) * 100f;
        intensityPercent = Mathf.Clamp(intensityPercent, 0f, 100f);

        if (resonanceIntensityRTPC != null)
        {
            resonanceIntensityRTPC.SetValue(gameObject, intensityPercent);
        }

        if (resonanceEnergyRTPC != null)
        {
            resonanceEnergyRTPC.SetValue(gameObject, energyPercent * 100f);
        }

        OnEnergyChangedEvent?.Invoke(energyPercent);

        NotifyResonanceTargets(true);

        if (currentEnergy <= 0f)
        {
            currentEnergy = 0f;
            
            if (resonanceDepletedEvent != null)
            {
                resonanceDepletedEvent.Post(gameObject);
            }

            StopResonanceHum();

            if (enableDebugLog)
                Debug.Log("[ResonanceHum] Energy depleted!");
        }
    }

    private void StopResonanceHum()
    {
        if (!isActive) return;

        isActive = false;
        timeSinceStop = 0f;

        if (resonanceHumStopEvent != null)
        {
            resonanceHumStopEvent.Post(gameObject);
        }

        if (resonanceIntensityRTPC != null)
        {
            resonanceIntensityRTPC.SetValue(gameObject, 0f);
        }

        if (activeVFX != null)
        {
            Destroy(activeVFX);
            activeVFX = null;
        }

        NotifyResonanceTargets(false);

        if (enableDebugLog)
            Debug.Log($"[ResonanceHum] Stopped. Energy remaining: {currentEnergy:F1}s");
    }

    private void HandleRecharge()
    {
        if (currentEnergy >= maxDuration) return;

        timeSinceStop += Time.deltaTime;

        if (timeSinceStop >= rechargeDelay)
        {
            float previousEnergy = currentEnergy;
            currentEnergy += rechargeRate * Time.deltaTime;
            currentEnergy = Mathf.Min(currentEnergy, maxDuration);

            float energyPercent = currentEnergy / maxDuration;

            if (resonanceEnergyRTPC != null)
            {
                resonanceEnergyRTPC.SetValue(gameObject, energyPercent * 100f);
            }

            if (!Mathf.Approximately(previousEnergy, currentEnergy))
            {
                OnEnergyChangedEvent?.Invoke(energyPercent);
            }
        }
    }

    private void NotifyResonanceTargets(bool isActive)
    {
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

    public float GetEnergyPercent()
    {
        return currentEnergy / maxDuration;
    }

    public bool IsActive()
    {
        return isActive;
    }

    public bool CanActivate()
    {
        return currentEnergy > 0f;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        if (isActive)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
    }
}
