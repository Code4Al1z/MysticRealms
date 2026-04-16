using UnityEngine;
using UnityEngine.AI;

public class GolemResonanceHandler : MonoBehaviour, IResonanceResponsive
{
    [Header("Resonance")]
    [SerializeField] private float maxResonanceRange = 12f;
    [SerializeField] private float resonanceSlowMultiplier = 0.3f;
    [SerializeField] private float drainDamagePerSecond = 6f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 14f;
    [SerializeField] private float knockbackDamage = 25f;
    [SerializeField] private float knockbackStunDuration = 1.2f;
    [SerializeField] private float knockbackCooldown = 4f;

    [Header("Stress Visuals")]
    [SerializeField] private ParticleSystem stressParticles;

    [Header("Wwise")]
    [SerializeField] private AK.Wwise.Event knockbackHitEvent;
    [SerializeField] private AK.Wwise.RTPC resonanceStressRTPC;

    public bool IsKnockedBack { get; private set; }
    public float ResonanceStress { get; private set; }

    private RockGolemEnemy golem;
    private NavMeshAgent agent;
    private Rigidbody rb;

    private bool isBeingDrained = false;
    private bool wasBeingDrained = false;
    private float knockbackTimer = -999f;
    private float stunTimer = 0f;
    private float lastStressRTPC = -1f;

    public void Initialise(RockGolemEnemy golem, NavMeshAgent agent, Rigidbody rb)
    {
        this.golem = golem;
        this.agent = agent;
        this.rb = rb;
    }

    public void Tick()
    {
        TickKnockbackRecovery();
        TickStressDecay();
        if (resonanceStressRTPC != null)
        {
            float stressValue = ResonanceStress * 100f;
            if (!Mathf.Approximately(stressValue, lastStressRTPC))
            {
                lastStressRTPC = stressValue;
                resonanceStressRTPC.SetValue(gameObject, stressValue);
            }
        }

        wasBeingDrained = isBeingDrained;
        isBeingDrained = false;
    }

    public void OnResonanceHumActive(Vector3 sourcePosition, float distance)
    {
        if (golem.IsDead || distance > maxResonanceRange) return;

        isBeingDrained = true;

        bool freshContact = !wasBeingDrained;
        bool canKnockback = Time.time > knockbackTimer + knockbackCooldown;

        if (freshContact && canKnockback)
        {
            ApplyKnockback(sourcePosition);
            golem.NotifyStatusEffectPublic("ResonanceHum", true);
            return;
        }

        golem.SetSpeedMultiplier("resonance_hum", resonanceSlowMultiplier);
        golem.TakeDamage(drainDamagePerSecond * Time.deltaTime, "ResonanceHum");
        ResonanceStress = Mathf.MoveTowards(ResonanceStress, 1f, 1.5f * Time.deltaTime);

        if (freshContact)
            golem.NotifyStatusEffectPublic("ResonanceHum", true);
    }

    public void OnResonanceHumStopped()
    {
        if (!wasBeingDrained && !isBeingDrained) return;

        isBeingDrained = false;
        golem.ClearSpeedMultiplier("resonance_hum");
        golem.NotifyStatusEffectPublic("ResonanceHum", false);
    }

    private void ApplyKnockback(Vector3 sourcePosition)
    {
        knockbackTimer = Time.time;
        IsKnockedBack = true;
        stunTimer = knockbackStunDuration;

        agent.isStopped = true;
        agent.enabled = false;
        rb.isKinematic = false;

        Vector3 dir = (transform.position - sourcePosition).normalized;
        dir.y = 0.3f;
        rb.AddForce(dir.normalized * knockbackForce, ForceMode.Impulse);

        golem.TakeDamage(knockbackDamage, "ResonanceHum_Knockback");
        if (knockbackHitEvent != null)
            knockbackHitEvent.Post(gameObject);

        ResonanceStress = Mathf.MoveTowards(ResonanceStress, 1f, 1.5f * Time.deltaTime);
    }

    public void ResetDrainState()
    {
        isBeingDrained = false;
        wasBeingDrained = false;
    }

    public void StopStressParticles()
    {
        if (stressParticles == null) return;
        stressParticles.Stop();
    }

    private void TickKnockbackRecovery()
    {
        if (!IsKnockedBack) return;

        stunTimer -= Time.deltaTime;
        if (stunTimer > 0f) return;

        IsKnockedBack = false;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        agent.enabled = true;
        agent.isStopped = false;
        agent.Warp(transform.position);
    }

    private void TickStressDecay()
    {
        if (isBeingDrained) return;

        ResonanceStress = Mathf.MoveTowards(ResonanceStress, 0f, 0.8f * Time.deltaTime);

        if (stressParticles == null) return;

        if (ResonanceStress > 0.05f)
        {
            if (!stressParticles.isPlaying) stressParticles.Play();
            var em = stressParticles.emission;
            em.rateOverTime = Mathf.Lerp(0f, 40f, ResonanceStress);
        }
        else
        {
            if (stressParticles.isPlaying) stressParticles.Stop();
        }
    }
}