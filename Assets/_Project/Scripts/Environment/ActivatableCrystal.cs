using UnityEngine;
using System.Collections;

public class ActivatableCrystal : MonoBehaviour, IEchoResponsive
{
    [Header("Frequency")]
    [SerializeField] private float requiredFrequency = 440f;
    [SerializeField] private float frequencyTolerance = 20f;
    [SerializeField] private float chargeTimeRequired = 3f;

    [Header("Visuals")]
    [SerializeField] private ShaderPropertyController shaderController;
    [SerializeField] private AudioDrivenShaderPulse audioDrivenPulse;
    [SerializeField] private bool useAudioDrivenPulse = false;

    [Header("Bridge")]
    [SerializeField] private GameObject[] connectedBridges;

    [Header("FX")]
    [SerializeField] private ParticleSystem activationParticles;

    private bool isSolved = false;
    private bool isReceivingPulse = false;
    private float charge = 0f;

    private void Start()
    {
        //shaderController?.SetInactiveInstant();

        if (audioDrivenPulse != null)
            audioDrivenPulse.enabled = false;

        foreach (var b in connectedBridges)
        {
            if (!b) continue;

            var s = b.GetComponent<ShaderPropertyController>();
            if (s) s.SetInactiveInstant();

            b.SetActive(false);
        }
    }

    private void Update()
    {
        if (isSolved) return;

        if (!isReceivingPulse)
        {
            Drain();
            return;
        }

        ManualPulseUpdate();
    }

    // called every frame while player beams
    public void OnEchoPulseActive(Vector3 sourcePosition, float distance, float frequency)
    {
        if (isSolved) return;

        isReceivingPulse = true;

        bool match = Mathf.Abs(frequency - requiredFrequency) <= frequencyTolerance;

        if (!match)
        {
            Drain();
            return;
        }

        charge += Time.deltaTime / chargeTimeRequired;
        charge = Mathf.Clamp01(charge);

        if (charge >= 1f)
            Solve();
    }

    public void OnEchoPulseStopped()
    {
        isReceivingPulse = false;
    }

    private void Drain()
    {
        charge -= Time.deltaTime / (chargeTimeRequired * 0.5f);
        charge = Mathf.Max(0f, charge);

        if (charge == 0f)
            StopAllVisuals();
    }

    private void ManualPulseUpdate()
    {
        if (useAudioDrivenPulse)
        {
            if (audioDrivenPulse && !audioDrivenPulse.enabled)
                audioDrivenPulse.enabled = true;
            return;
        }

        if (!shaderController) return;

        float oscillation = Mathf.Sin(Time.time * 5f) * 0.5f + 0.5f;
        float strength = Mathf.Lerp(0.1f, 1f, oscillation);

        shaderController.SetPulseStrengthDirect(strength);
        shaderController.SetPulseActive(true);
    }

    private void StopAllVisuals()
    {
        if (audioDrivenPulse)
            audioDrivenPulse.enabled = false;

        shaderController?.SetPulseActive(false);
    }

    private void Solve()
    {
        isSolved = true;

        StopAllVisuals();

        shaderController.SetActiveInstant();

        if (activationParticles != null)
            activationParticles.Play();

        foreach (var b in connectedBridges)
        {
            if (!b) continue;

            b.SetActive(true);
            b.GetComponent<ShaderPropertyController>()?.FadeIn();
        }
    }
}
