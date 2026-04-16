using UnityEngine;

public class WispEchoPulseHandler : MonoBehaviour, IEchoResponsive
{
    [Header("Echo Pulse")]
    [SerializeField] private float maxPulseRange = 12f;
    [SerializeField] private float damagePerSecond = 8f;
    [SerializeField] private float requiredFrequency = 300f;
    [SerializeField] private float frequencyTolerance = 30f;
    [SerializeField] private float pulseSlowMultiplier = 0.35f;
    [SerializeField] private float freqMatchExtraSlow = 0.6f;
    [SerializeField] private float stressRecoveryRate = 0.4f;
    [SerializeField] private float maxShakeAmplitude = 0.08f;
    [SerializeField] private float shakeFrequency = 18f;
    [SerializeField] private float shakeThreshold = 0.5f;

    [Header("Visuals")]
    [SerializeField] private Renderer wispRenderer;
    [SerializeField] private Color healthyColor = new Color(0.4f, 0.8f, 1f);
    [SerializeField] private Color stressedColor = new Color(1f, 0.2f, 0.1f);
    [SerializeField] private ParticleSystem wispBodyParticles;

    [Header("Wwise")]
    [SerializeField] private AK.Wwise.Event wispPulseHitEvent;
    [SerializeField] private AK.Wwise.Event wispRecoverEvent;
    [SerializeField] private AK.Wwise.RTPC wispSpeedRTPC;
    [SerializeField] private AK.Wwise.RTPC wispPulseStressRTPC;

    public float StressLevel { get; private set; }

    private WispEnemy wisp;
    private bool isBeingPulsed = false;
    private bool wasPulsedLastFrame = false;
    private float lastSpeedRTPC = -1f;
    private float lastStressRTPC = -1f;

    private static readonly int ShaderBaseColor = Shader.PropertyToID("_BaseColor");
    private Material wispMat;

    public void Initialise(WispEnemy wisp)
    {
        this.wisp = wisp;
        if (wispRenderer != null)
            wispMat = wispRenderer.material;
    }

    public void Tick()
    {
        if (!isBeingPulsed)
            TickStressRecovery();

        UpdateVisuals();

        if (wispSpeedRTPC != null)
        {
            float speedNorm = wisp.BaseMoveSpeed > 0f ? wisp.Agent.speed / wisp.BaseMoveSpeed : 1f;
            float speedValue = speedNorm * 100f;
            if (!Mathf.Approximately(speedValue, lastSpeedRTPC))
            {
                lastSpeedRTPC = speedValue;
                wispSpeedRTPC.SetValue(gameObject, speedValue);
            }
        }

        if (wispPulseStressRTPC != null)
        {
            float stressValue = StressLevel * 100f;
            if (!Mathf.Approximately(stressValue, lastStressRTPC))
            {
                lastStressRTPC = stressValue;
                wispPulseStressRTPC.SetValue(gameObject, stressValue);
            }
        }

        wasPulsedLastFrame = isBeingPulsed;
        isBeingPulsed = false;
    }

    public float GetRequiredFrequency() => requiredFrequency;

    public void OnEchoPulseActive(Vector3 sourcePosition, float distance, float frequency)
    {
        if (wisp.IsDead || distance > maxPulseRange) return;

        isBeingPulsed = true;

        if (!wasPulsedLastFrame)
        {
            if (wispPulseHitEvent != null) wispPulseHitEvent.Post(gameObject);
            wisp.NotifyStatusEffectPublic("EchoPulse", true);
        }

        bool freqMatch = Mathf.Abs(frequency - requiredFrequency) <= frequencyTolerance;
        float stressSpd = freqMatch ? 1.5f : 0.6f;
        StressLevel = Mathf.MoveTowards(StressLevel, 1f, stressSpd * Time.deltaTime);

        float slow = pulseSlowMultiplier * (freqMatch ? freqMatchExtraSlow : 1f);
        wisp.SetSpeedMultiplier("echo_pulse", slow);

        if (freqMatch)
            wisp.TakeDamage(damagePerSecond * Time.deltaTime, "EchoPulse");

        if (StressLevel >= shakeThreshold)
        {
            float mag = Mathf.InverseLerp(shakeThreshold, 1f, StressLevel) * maxShakeAmplitude;
            transform.position += new Vector3(
                Mathf.Sin(Time.time * shakeFrequency * 1.3f) * mag,
                Mathf.Sin(Time.time * shakeFrequency) * mag * 0.5f,
                Mathf.Sin(Time.time * shakeFrequency * 0.7f) * mag);
        }
    }

    public void OnEchoPulseStopped()
    {
        isBeingPulsed = false;
        if (wispRecoverEvent != null) wispRecoverEvent.Post(gameObject);
        wisp.NotifyStatusEffectPublic("EchoPulse", false);
    }

    public void StopBodyParticles() { if (wispBodyParticles != null) wispBodyParticles.Stop(); }

    private void TickStressRecovery()
    {
        StressLevel = Mathf.MoveTowards(StressLevel, 0f, stressRecoveryRate * Time.deltaTime);

        float recovery = Mathf.Lerp(pulseSlowMultiplier, 1f, 1f - StressLevel);
        wisp.SetSpeedMultiplier("echo_pulse", recovery);

        if (StressLevel <= 0f)
            wisp.ClearSpeedMultiplier("echo_pulse");
    }

    private void UpdateVisuals()
    {
        if (wispMat != null)
            wispMat.SetColor(ShaderBaseColor, Color.Lerp(healthyColor, stressedColor, StressLevel));

        if (wispBodyParticles != null)
        {
            var em = wispBodyParticles.emission;
            em.rateOverTime = Mathf.Lerp(30f, 6f, StressLevel);
        }
    }
}