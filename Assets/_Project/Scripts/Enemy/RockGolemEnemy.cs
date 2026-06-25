using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RockGolemEnemy : BaseEnemy
{
    [Header("Patrol")]
    [SerializeField] private Transform[] golemPatrolPoints;
    [SerializeField] private float patrolStoppingDistance = 0.6f;

    [Header("Melee Attack")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float meleeDamage = 15f;
    [SerializeField] private ParticleSystem swingParticles;

    [Header("Death")]
    [SerializeField] private ParticleSystem deathParticles;

    [Header("Wwise")]
    [SerializeField] private AK.Wwise.Event meleeSwingEvent;

    [Header("Components")]
    [SerializeField] private GolemAnimator golemAnimator;
    [SerializeField] private GolemAttackHitbox attackHitbox;
    [SerializeField] private GolemResonanceHandler resonanceHandler;
    [SerializeField] private GolemFootstepHandler footstepHandler;

    private Rigidbody rb;
    private float attackLockTimer = 0f;

    protected override void Awake()
    {
        RegisterPatrolPoints(golemPatrolPoints);
        base.Awake();

        rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.isKinematic = true;
    }

    protected override void Start()
    {
        base.Start();
        resonanceHandler.Initialise(this, agent, rb);
        footstepHandler.Initialise(agent, baseMoveSpeed);
        SetNextPatrolTarget();
    }

    public override string GetEnemyTypeID() => "RockGolem";

    protected override void OnEnemyUpdate()
    {
        resonanceHandler.Tick();
        footstepHandler.Tick(resonanceHandler.IsKnockedBack);
        SyncAnimator();
        TickAttackLock();

        if (resonanceHandler.IsKnockedBack) return;
    }

    protected override void OnStateChanged(EnemyState prev, EnemyState next)
    {
        SyncAnimator();

        if (next == EnemyState.Patrol)
        {
            float closestDist = float.MaxValue;
            for (int i = 0; i < golemPatrolPoints.Length; i++)
            {
                float d = Vector3.Distance(transform.position, golemPatrolPoints[i].position);
                if (d < closestDist)
                {
                    closestDist = d;
                    currentPatrolIndex = i;
                }
            }

            currentPatrolIndex = (currentPatrolIndex + 1) % golemPatrolPoints.Length;
            SetNextPatrolTarget();

            if (resonanceHandler != null)
                resonanceHandler.ResetDrainState();
        }
    }

    protected override void PerformAttack()
    {
        if (meleeSwingEvent != null)
            meleeSwingEvent.Post(gameObject);
        if (swingParticles != null)
            swingParticles.Play();
        if (golemAnimator != null)
            golemAnimator.SetAttack();

        float attackDuration = golemAnimator != null
            ? golemAnimator.AttackClipLength()
            : attackCooldown;

        isAttackLocked = true;
        attackLockTimer = attackDuration;

        if (attackHitbox != null)
        {
            attackHitbox.Activate(meleeDamage, this);
            Invoke(nameof(DeactivateHitbox), attackDuration * 0.5f);
        }
    }

    private void DeactivateHitbox()
    {
        if (attackHitbox != null)
            attackHitbox.Deactivate();
    }

    protected override void AdvancePatrol()
    {
        if (resonanceHandler.IsKnockedBack) return;
        if (golemPatrolPoints == null || golemPatrolPoints.Length == 0) return;
        if (!agent.enabled || !agent.isOnNavMesh) return;

        // Wait for the path to be calculated before checking arrival.
        // Without this, remainingDistance is 0 on the frame the destination is set,
        // causing the index to cycle immediately and the golem to never move.
        if (agent.pathPending) return;

        if (agent.remainingDistance <= patrolStoppingDistance)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % golemPatrolPoints.Length;
            SetNextPatrolTarget();
        }
    }

    protected override void OnEnemyDeath()
    {
        if (golemAnimator != null)
            golemAnimator.SetDead();
        if (resonanceHandler != null)
            resonanceHandler.StopStressParticles();

        if (deathParticles != null)
        {
            deathParticles.transform.SetParent(null);
            deathParticles.Play();
        }

        Destroy(gameObject, 1.2f);
    }

    private void TickAttackLock()
    {
        if (!isAttackLocked) return;

        attackLockTimer -= Time.deltaTime;
        if (attackLockTimer <= 0f)
            isAttackLocked = false;
    }

    private void SetNextPatrolTarget()
    {
        if (golemPatrolPoints == null || golemPatrolPoints.Length == 0) return;
        if (!agent.enabled || !agent.isOnNavMesh) return;
        agent.SetDestination(golemPatrolPoints[currentPatrolIndex].position);
    }

    private void SyncAnimator()
    {
        if (golemAnimator == null) return;
        if (isDead) { golemAnimator.SetDead(); return; }
        if (isAttackLocked) return;

        switch (CurrentState)
        {
            case EnemyState.Idle:
                golemAnimator.SetIdle();
                break;

            case EnemyState.Patrol:
            case EnemyState.Chase:
            case EnemyState.Return:
                golemAnimator.SetWalk();
                break;

                // Attack is set exclusively in PerformAttack, never here.
        }
    }

    public void NotifyStatusEffectPublic(string effect, bool active)
        => NotifyStatusEffect(effect, active);

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, 12f);

        if (golemPatrolPoints != null)
        {
            Gizmos.color = new Color(0.8f, 0.4f, 0.1f);
            for (int i = 0; i < golemPatrolPoints.Length; i++)
            {
                if (golemPatrolPoints[i] == null) continue;
                Gizmos.DrawSphere(golemPatrolPoints[i].position, 0.2f);
                if (i < golemPatrolPoints.Length - 1 && golemPatrolPoints[i + 1] != null)
                    Gizmos.DrawLine(golemPatrolPoints[i].position, golemPatrolPoints[i + 1].position);
            }
        }
    }
}