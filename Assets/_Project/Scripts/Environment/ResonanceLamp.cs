using UnityEngine;

public class ResonanceLamp : MonoBehaviour, IResonanceResponsive
{
    [Header("Lamp Settings")]
    [Tooltip("Maximum distance player can be for resonance to work")]
    [SerializeField] private float maxResonanceDistance = 15f;

    [Header("References")]
    [SerializeField] private ShaderPropertyController shaderController;

    [Header("Wwise Events")]
    [SerializeField] private AK.Wwise.Event lampFillingEvent;
    [SerializeField] private AK.Wwise.Event lampDepletingEvent;
    [SerializeField] private AK.Wwise.Event lampFullEvent;
    [SerializeField] private AK.Wwise.Event lampEmptyEvent;

    [Header("Wwise RTPCs")]
    [SerializeField] private AK.Wwise.RTPC lampEnergyRTPC;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = false;

    private float currentEnergy = 0f;
    private float maxEnergy;
    private bool playerInRange = false;
    private bool isFilling = false;
    private bool isDepleting = false;
    private bool wasFull = false;
    private bool wasEmpty = true;

    public delegate void OnEnergyChanged(float energyPercent);
    public event OnEnergyChanged OnEnergyChangedEvent;

    private void Start()
    {
        if (shaderController == null)
        {
            shaderController = GetComponent<ShaderPropertyController>();
        }

        currentEnergy = 0f;

        if (shaderController != null)
        {
            shaderController.SetPulseStrengthDirect(0f);
            shaderController.SetPulseActive(false);
        }
    }

    private void Update()
    {
        // Sync lamp energy with shader pulse strength
        if (shaderController != null)
        {
            maxEnergy = shaderController.GetMaxPulseStrength();
            if (maxEnergy > 0f)
            {
                float previousEnergy = currentEnergy;
                currentEnergy = shaderController.GetPulseStrength() / maxEnergy;

                if (!Mathf.Approximately(previousEnergy, currentEnergy))
                {
                    OnEnergyChangedEvent?.Invoke(GetEnergyPercent());
                }
            }
        }

        UpdateWwiseRTPC();
        CheckEnergyThresholds();
    }

    public void OnResonanceHumActive(Vector3 sourcePosition, float distance)
    {
        playerInRange = distance <= maxResonanceDistance;

        if (playerInRange)
        {
            if (!isFilling)
            {
                StartFilling();
            }

            // Stop depleting if resonance hum becomes active again
            if (isDepleting)
            {
                isDepleting = false;
            }
        }
    }

    public void OnResonanceHumStopped()
    {
        playerInRange = false;

        //if (isFilling)
        //{
        //    StopFilling();
        //}

        // Start depleting when resonance hum stops (at any energy level)
        if (currentEnergy > 0f)
        {
            StartDepleting();
        }
    }

    private void StartFilling()
    {
        isFilling = true;
        isDepleting = false;

        if (shaderController != null)
        {
            shaderController.FadeIn();
        }

        if (lampFillingEvent != null)
        {
            lampFillingEvent.Post(gameObject);
        }

        if (enableDebugLog)
            Debug.Log($"[ResonanceLamp] {name} started filling");
    }

    private void StopFilling()
    {
        isFilling = false;

        if (enableDebugLog)
            Debug.Log($"[ResonanceLamp] {name} stopped filling");
    }

    private void StartDepleting()
    {
        isDepleting = true;
        isFilling = false;

        if (shaderController != null)
        {
            shaderController.FadeOut();
        }

        if (lampDepletingEvent != null)
        {
            lampDepletingEvent.Post(gameObject);
        }

        if (enableDebugLog)
            Debug.Log($"[ResonanceLamp] {name} started depleting");
    }

    private void UpdateWwiseRTPC()
    {
        if (lampEnergyRTPC != null)
        {
            lampEnergyRTPC.SetValue(gameObject, GetEnergyPercent() * 100f);
        }
    }

    private void CheckEnergyThresholds()
    {
        // Check if lamp became full
        if (currentEnergy >= maxEnergy && !wasFull)
        {
            wasFull = true;
            wasEmpty = false;

            if (lampFullEvent != null)
            {
                lampFullEvent.Post(gameObject);
            }

            if (enableDebugLog)
                Debug.Log($"[ResonanceLamp] {name} is now full");
        }
        else if (currentEnergy < maxEnergy)
        {
            wasFull = false;
        }

        // Check if lamp became empty
        if (currentEnergy <= 0f && !wasEmpty)
        {
            wasEmpty = true;
            wasFull = false;

            if (lampEmptyEvent != null)
            {
                lampEmptyEvent.Post(gameObject);
            }

            if (enableDebugLog)
                Debug.Log($"[ResonanceLamp] {name} is now empty");
        }
        else if (currentEnergy > 0f)
        {
            wasEmpty = false;
        }
    }

    public float GetEnergyPercent()
    {
        return currentEnergy / maxEnergy;
    }

    public bool HasEnergy()
    {
        return currentEnergy > 0f;
    }

    public bool IsFull()
    {
        return currentEnergy >= maxEnergy;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = HasEnergy() ? Color.yellow : Color.gray;
        Gizmos.DrawWireSphere(transform.position, maxResonanceDistance);

        // Draw energy level indicator
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f * GetEnergyPercent());
        }
    }
}