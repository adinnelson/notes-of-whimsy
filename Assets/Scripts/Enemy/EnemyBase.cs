using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class EnemyBase : MonoBehaviour
{
    protected enum State { Idle, Chase, Telegraph, Recover, Hurt, Dead }

    [Header("Core")]
    [SerializeField] protected EnemyConfig config;

    [Header("Death Behavior")]
    [SerializeField] private bool disableOnDeath = true;
    [SerializeField] protected float deathFadeDuration = 0.5f;

    [Header("Stunned")]
    [SerializeField] protected Material stunnedMaterial;
    [SerializeField] protected Material defaultMaterial;
    protected SpriteRenderer sprite;

    protected Transform target;
    protected Rigidbody2D rb;
    protected State state;

    protected GameManager gameManager;
    private int recoverBeatsRemaining = 0;

    protected Health health;
    protected Collider2D col;

    protected HashSet<string> stunEffects = new HashSet<string>();

    // Get Player location based on tag. If no player found, log an error.
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();

        health = GetComponent<Health>();
        if (health == null) 
        {
            Debug.LogError($"{name}: No Health component found on enemy.");
        }
        health.OnDeath += HandleDeath;


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

    protected virtual void Start()
    {
        gameManager = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError($"{name}: No GameObject with tag 'GameManager' found in scene.");
        }

        gameManager.OnOddBeatTriggered += OnBeat;

        EnterState(State.Idle);
    }

    // State machine transition, switches to a new state and immediately runs state logic.
    protected void EnterState(State newState)
    {
        state = newState;
        RunStateLogic();
    }

    // Run current state logic.
    protected void RunStateLogic()
    {
        switch (state)
        {
            case State.Idle:
                OnIdle();
                break;

            case State.Chase:
                OnChase();
                break;

            case State.Telegraph:
                OnTelegraph(gameManager.GetBPM());
                break;

            case State.Recover:
                OnRecover();
                break;

            case State.Hurt:
                OnHurt();
                break;

            case State.Dead:
                break;
        }
     }

    // Runs on odd beats to handle beat-synced behaviour based on current state.
    protected virtual void OnBeat()
    {
        if (target == null || state == State.Dead)
        {
            return;
        }

        // If stunned, skip beat logic until stun is removed. Stun clears remaining recovery,
        // but does not cancel an attack already queued for this beat.
        if (stunEffects.Count > 0)
        {
            recoverBeatsRemaining = 0;
            StunVisuals();
            return;
        }

        // NOTE: EnterState also runs state logic and may cause two states to run in one beat.
        RunStateLogic();
    }

    // Returns true if the target is within this enemy's attack range.
    protected bool TargetInAttackRange()
    {
        return DistanceToTarget() <= config.attackRange;
    }

    // Returns true if the target is within this enemy's visual aggeo range.
    protected bool TargetInAggroRange()
    {
        return DistanceToTarget() <= config.aggroRange;
    }

    // Idle state behavior, Enemy holds still until the player gets within aggroRange, then transitions into Chase.
    protected virtual void OnIdle()
    {
        StopMovement();

        if (TargetInAggroRange())
        {
            EnterState(State.Chase);
        }
    }

    protected virtual void OnTelegraph(float bpm)
    {
        recoverBeatsRemaining = config.recoverBeats;

        float secondsPerBeat = 60.0f / bpm;
        float timeToNextbeat = secondsPerBeat;

        // If no attack queued, queue attack for next beat.
        if (!IsInvoking(nameof(OnAttack)))
        {
            Invoke(nameof(OnAttack), timeToNextbeat);
        }

        state = State.Recover;
    }

    // Recover state behavior. After recoverBeats, switch back to Idle.
    protected virtual void OnRecover()
    {
        recoverBeatsRemaining--;

        if (recoverBeatsRemaining <= 0)
        {
            // Switch to idle but skip the one-shot OnEnterState since this beat is the recovery beat
            state = State.Idle;
        }
    }

    // Hurt state behavior, enemy is briefly vulnerable and cannot act
    protected virtual void OnHurt()
    {
        state = State.Recover;

        if (config.hurtInterruptsAttack)
        {
            // Stop any queued attacks
            CancelInvoke(nameof(OnAttack));
        }

        if (recoverBeatsRemaining > config.hurtBeats)
        {
            return;
        }
        recoverBeatsRemaining = config.hurtBeats;
    }

    // Call when the enemy takes damage. If configured to interrupt attacks, enter Hurt.
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

    /// Temporarily cranks up linear drag so the knockback impulse decelerates naturally
    /// instead of sliding forever. Saves and restores the original drag value.
    public void StartKnockbackDecay(float duration = 0.2f)
    {
        StartCoroutine(KnockbackDecayRoutine(duration));
    }

    private IEnumerator KnockbackDecayRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        rb.linearVelocity = Vector2.zero;
    }

    public void AddStunEffect(string key)
    {
        if(stunEffects.Contains(key)) return;

        stunEffects.Add(key);
    }

    public void RemoveStunEffect(string key)
    {
        stunEffects.Remove(key);

        if(stunEffects.Count <= 0)
        {
            sprite.material = defaultMaterial;
        }
    }

    protected void StunVisuals()
    {
        sprite.material = stunnedMaterial;
    }

    protected virtual void HandleDeath()
    {
        if (state == State.Dead)
        {
            return;
        }

        EnterState(State.Dead);

        // Stop attacks
        CancelInvoke(nameof(OnAttack));

        // Stop physics interactions
        StopMovement();
        if (col != null)
        {
            col.enabled = false;
        }

        // gold drops from enemies when they die
        if (config.goldCoinPrefab != null)
        {
            for (int i = 0; i < config.goldCoinCount;  i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.5f;
                Instantiate(config.goldCoinPrefab, transform.position + (Vector3)offset, Quaternion.identity);
            }
        }

        if (disableOnDeath)
        {
            StartCoroutine(FadeOutAndDisable());
        }

        EnemyWaveController controller = transform.parent.GetComponent<EnemyWaveController>();
        if (controller != null)
        {
            controller.OnEnemyDeath(gameObject);
        }
    }

    private IEnumerator FadeOutAndDisable()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();

        // Capture starting colours so we preserve any tints already on sprites
        Color[] startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            startColors[i] = renderers[i].color;
        }

        float elapsed = 0.0f;
        while (elapsed < deathFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1.0f, 0.0f, elapsed / deathFadeDuration);
            for (int i = 0; i < renderers.Length; i++)
            {
                Color c = startColors[i];
                c.a = alpha;
                renderers[i].color = c;
            }
            yield return null;
        }

        gameObject.SetActive(false);
    }

    protected virtual void OnDestroy()
    {
        health.OnDeath -= HandleDeath;

        if (gameManager != null)
        {
            gameManager.OnOddBeatTriggered -= OnBeat;
        }
    }

    // Required attack customization
    protected abstract bool CanStartAttack();

    // one-shot hooks for other states
    protected virtual void OnChase() { }
    protected virtual void OnAttack() { }
}
