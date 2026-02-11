using UnityEngine;
using System.Collections;

public class ActivatableCrystal : MonoBehaviour, IEchoResponsive
{
    [Header("Frequency")]
    [SerializeField] private float requiredFrequency = 440f;
    [SerializeField] private float frequencyTolerance = 20f;
    [SerializeField] private float chargeTimeRequired = 3f;
    [SerializeField] private float drainTime = 9f;

    [Header("Visuals")]
    [SerializeField] private ShaderPropertyController shaderController;

    [Header("Effected Object")]
    [SerializeField] private GameObject[] connectedObjects;

    [Header("FX")]
    [SerializeField] private ParticleSystem activationParticles;

    private bool isSolved = false;
    private bool isReceivingPulse = false;
    private float charge = 0f;

    private void Start()
    {
        shaderController?.SetInactiveInstant();

        foreach (var b in connectedObjects)
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

    public float GetRequiredFrequency()
    {
        return requiredFrequency;
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
        charge -= Time.deltaTime / (drainTime);
        charge = Mathf.Max(0f, charge);

        //if (charge == 0f)
        //    StopAllVisuals();
    }

    private void ManualPulseUpdate()
    {
        if (!shaderController) return;

        shaderController.FadeIn();
    }

    private void StopAllVisuals()
    {
        shaderController?.SetPulseActive(false);
    }

    private void Solve()
    {
        isSolved = true;

        //StopAllVisuals();

        shaderController.SetActiveInstant();

        if (activationParticles != null)
            activationParticles.Play();

        foreach (var b in connectedObjects)
        {
            if (!b) continue;

            b.SetActive(true);
            b.GetComponent<ShaderPropertyController>()?.FadeIn();
        }
    }
}
