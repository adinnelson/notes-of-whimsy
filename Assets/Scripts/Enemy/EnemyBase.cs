using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class EnemyBase : MonoBehaviour
{
    protected enum State { Idle, Chase, Telegraph, Attack, Recover, Hurt, Dead }

    [Header("Core")]
    [SerializeField] protected EnemyConfig config;

    [Header("Death Behavior")]
    [SerializeField] private bool disableOnDeath = true;

    protected Transform target;
    protected Rigidbody2D rb;
    protected State state;

    private Health health;
    private Collider2D col;

    protected float stateTimer;
    protected float cooldownTimer;

    protected HashSet<string> stunEffects = new HashSet<string>();

    // Get Player location based on tag. If no player found, log an error.
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        health = GetComponent<Health>();
        if (health != null)
        {
            health.OnDeath += HandleDeath;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
        else
        {
            Debug.LogError($"{name}: No GameObject with tag 'Player' found in scene.");
        }
    }

    // initialize the enemy into the Idle state.
    protected virtual void Start()
    {
        EnterState(State.Idle);
    }

    // Handles "global" timers (cooldown + stateTimer) and updates the current state's behavior.
    protected virtual void Update()
    {
        if (stunEffects.Count > 0)
        {
            return;
        }

        if (target == null)
        {
            return;
        }

        if (state == State.Dead)
        {
            return;
        }

        if (cooldownTimer > 0.0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        stateTimer -= Time.deltaTime;

        TickState(Time.deltaTime);
    }

    // State machine transition, switches to a new state and runs one-time "enter" logic
    protected void EnterState(State newState)
    {
        state = newState;
        OnEnterState(newState);
    }

    // Runs once when changing state to do things like set timer, trigger one-shot anim/sfx/vfx
    protected virtual void OnEnterState(State newState)
    {
        switch (newState)
        {
            case State.Idle:
                stateTimer = 0.0f;
                OnIdleStart();
                break;

            case State.Chase:
                stateTimer = 0.0f;
                OnChaseStart();
                break;

            case State.Telegraph:
                stateTimer = config.telegraphSeconds;
                OnTelegraphStart();
                break;

            case State.Attack:
                stateTimer = config.attackSeconds;
                OnAttackStart();
                break;

            case State.Recover:
                stateTimer = config.recoverSeconds;
                OnRecoverStart();
                break;

            case State.Hurt:
                stateTimer = config.hurtSeconds;
                OnHurtStart();
                break;

            case State.Dead:
                stateTimer = 0.0f;
                StopMovement();
                break;
        }
    }

    // Runs every frame while in current state.
    protected void TickState(float dt)
    {
        switch (state)
        {
            case State.Idle: IdleTick(dt); break;
            case State.Chase: ChaseTick(dt); break;
            case State.Telegraph: TelegraphTick(dt); break;
            case State.Attack: AttackTick(dt); break;
            case State.Recover: RecoverTick(dt); break;
            case State.Hurt: HurtTick(dt); break;
            case State.Dead: DeadTick(dt); break;
        }
    }

    // returns true if the enemy can attack right now. IF attack cooldown is finished AND player is within attack range.
    protected bool IsReadyAndInRange()
    {
        if (cooldownTimer > 0.0f)
        {
            return false;
        }
        return DistanceToTarget() <= config.attackRange;
    }

    // Chase state behavior, Move toward the target until we're within stopDistance, and if this enemy decides it can attack, transition into Telegraph.
    protected virtual void ChaseTick(float dt)
    {
        ChaseUntilStopDistance(config.chaseSpeed, config.attackRange);

        if (CanStartAttack())
        {
            EnterState(State.Telegraph);
        }
    }

    // Telegraph state behavior, Enemy holds still during wind-up. When the telegraph timer ends, transition into Attack
    protected virtual void TelegraphTick(float dt)
    {
        StopMovement();

        if (stateTimer <= 0.0f)
        {
            EnterState(State.Attack); 
        }
    }

    // Attack state behavior, Runs enemy-specific attack logic each frame. When the attack window ends, stop the attack, start cooldown, and transition into Recover.
    protected virtual void AttackTick(float dt)
    {
        OnAttackTick(dt);

        if (stateTimer <= 0.0f)
        {
            OnAttackEnd();
            cooldownTimer = config.cooldownSeconds;
            EnterState(State.Recover);
        }
    }

    // Recover state behavior, Enemy holds still during recovery. When recover timer ends, return to Chase
    protected virtual void RecoverTick(float dt)
    {
        StopMovement();

        if (stateTimer <= 0.0f)
        {
            EnterState(State.Chase);
        }
    }

    // Idle state behavior, Enemy holds still until the player gets within aggroRange, then transitions into Chase.
    protected virtual void IdleTick(float dt)
    {
        StopMovement();

        if (DistanceToTarget() <= config.aggroRange)
        {
            EnterState(State.Chase);
        }
    }

    // Hurt state behavior, Enemy holds still while "stunned/flinching". Once hurt timer ends, transition back to Chase.
    protected virtual void HurtTick(float dt)
    {
        StopMovement();

        if (stateTimer <= 0.0f)
        {
            EnterState(State.Chase);
        }
    }

    // Dead state behavior, Enemy does nothing except enforce no movement. (Later you might disable collisions, play death anim, or return to pool.)
    protected virtual void DeadTick(float dt)
    {
        StopMovement();
    }

    // call this when the enemy takes damage. If configured to interrupt attacks, switch to Hurt state. (Timer and one-shot effects are handled in OnEnterState(Hurt).)
    public void NotifyDamaged()
    {
        if (state == State.Dead)
        {
            return;
        }

        if (config.hurtInterruptsAttack)
        {
            EnterState(State.Hurt); 
        }
    }

    // sets Rigidbody velocity to move directly toward the target at the given speed
    protected void MoveTowardsTarget(float speed)
    {
        Vector2 toTarget = (target.position - transform.position);
        Vector2 dir = toTarget.normalized;
        rb.linearVelocity = dir * speed;
    }

    // force the enmy to stop by zeroing out its velocity
    protected void StopMovement()
    {
        rb.linearVelocity = Vector2.zero;
    }

    // returns the current distance from enemy to target (player).
    protected float DistanceToTarget()
    {
        return Vector2.Distance(transform.position, target.position);
    }

    // chase the target until within stopDistance. If too far, move toward the target at speed; otherwise stop.
    protected void ChaseUntilStopDistance(float speed, float stopDistance)
    {
        float dist = Vector2.Distance(transform.position, target.position);

        if (dist > stopDistance)
        {
            Vector2 dir = (target.position - transform.position).normalized;
            rb.linearVelocity = dir * speed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void AddStunEffect(string key)
    {
        if(stunEffects.Contains(key)) return;

        stunEffects.Add(key);
    }

    public void RemoveStunEffect(string key)
    {
        stunEffects.Remove(key);
    }
    protected virtual void HandleDeath()
    {
        if (state == State.Dead)
        {
            return;
        }

        EnterState(State.Dead);

        // Stop physics interactions
        StopMovement();
        if (col != null)
        {
            col.enabled = false;
        }

        if (disableOnDeath)
        {
            gameObject.SetActive(false);   // pooled-friendly death for enemy pooling later
        }
    }
    protected virtual void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath -= HandleDeath;
        }
    }

    // Required attack customization
    protected abstract bool CanStartAttack();
    protected abstract void OnAttackStart();
    protected abstract void OnAttackTick(float dt);
    protected abstract void OnAttackEnd();

    // one-shot hooks for other states
    protected virtual void OnIdleStart() { }
    protected virtual void OnChaseStart() { }
    protected virtual void OnTelegraphStart() { }
    protected virtual void OnRecoverStart() { }
    protected virtual void OnHurtStart() { }
}
