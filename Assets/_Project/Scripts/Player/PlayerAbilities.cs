using UnityEngine;

/// <summary>
/// Central manager for player abilities
/// Delegates to individual ability components
/// </summary>
public class PlayerAbilities : MonoBehaviour
{
    [Header("Ability References")]
    [SerializeField] private EchoPulseAbility echoPulseAbility;
    [SerializeField] private ResonanceHumAbility resonanceHumAbility;

    private void Awake()
    {
        if (echoPulseAbility == null)
        {
            echoPulseAbility = GetComponent<EchoPulseAbility>();
        }

        if (resonanceHumAbility == null)
        {
            resonanceHumAbility = GetComponent<ResonanceHumAbility>();
        }
    }

    public EchoPulseAbility GetEchoPulse()
    {
        return echoPulseAbility;
    }

    public ResonanceHumAbility GetResonanceHum()
    {
        return resonanceHumAbility;
    }

    public bool IsEchoPulseActive()
    {
        return echoPulseAbility != null && echoPulseAbility.IsActive();
    }

    public bool IsResonanceActive()
    {
        return resonanceHumAbility != null && resonanceHumAbility.IsActive();
    }

    public float GetEchoPulseFrequency()
    {
        return echoPulseAbility != null ? echoPulseAbility.GetFrequency() : 0f;
    }

    public float GetResonanceEnergyPercent()
    {
        return resonanceHumAbility != null ? resonanceHumAbility.GetEnergyPercent() : 0f;
    }
}

/// <summary>
/// Interface for objects that respond to Echo Pulse
/// </summary>
public interface IEchoResponsive
{
    void OnEchoPulseActive(Vector3 sourcePosition, float distance, float frequency);
    void OnEchoPulseStopped();
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