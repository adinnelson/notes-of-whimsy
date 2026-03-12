using UnityEngine;

/// Tanky enemy that slowly chases the player, telegraphs a charge on beats 1 and 3,
/// then charges in that direction until hitting a wall on beats 2 and 4.
/// Becomes stunned on wall impact. Only takes damage from player spells.
public class TreantEnemy : EnemyBase
{
    [Header("Treant - Movement")]
    [SerializeField] private float chaseSpeed = 1.5f;

    [Header("Treant - Charge")]
    [SerializeField] private float chargeSpeed = 16.0f;
    [SerializeField] private float telegraphJitterDegrees = 10.0f;
    [SerializeField] private GameObject telegraphVisual;
    [SerializeField] private string wallTag = "Walls";

    [Header("Treant - Wall Stun")]
    [SerializeField] private int wallStunBeats = 2;
    [SerializeField] private GameObject stunVisual;

    private TreantHitbox hitbox;
    private Collider2D bodyCollider;

    private Vector2 lockedChargeDir;
    private bool isCharging = false;
    private bool shouldTelegraphNext = true;
    private int wallStunBeatsRemaining = 0;
    private Animator animator;

    private EnemyFlip enemyFlip;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        enemyFlip = GetComponent<EnemyFlip>();
        
        bodyCollider = GetComponent<Collider2D>();

        hitbox = GetComponentInChildren<TreantHitbox>(includeInactive: true);
        if (hitbox == null)
        {
            Debug.LogWarning($"{name}: No TreantHitbox found in children.");
        }
        else
        {
            hitbox.Initialize(config.damage);
        }

        SetHitboxActive(false);
        SetPlayerCollisionEnabled(false);

        HideTelegraphVisual();
        SetStunVisualActive(false);
    }

    // Beat cycle is fully managed in OnBeat — EnemyBase attack flow is not used
    protected override bool CanStartAttack() => false;

    protected override void Start()
    {
        base.Start();
        // Skip Idle/aggro range — Treant starts telegraphing immediately from wherever it spawns
        state = State.Chase;
    }

    protected override void OnBeat()
    {
        if (target == null || state == State.Dead)
        {
            return;
        }

        if (stunEffects.Count > 0)
        {
            return;
        }

        if (wallStunBeatsRemaining > 0)
        {
            wallStunBeatsRemaining--;
            if (wallStunBeatsRemaining <= 0)
            {
                ExitWallStun();
            }
            return;
        }

        if (isCharging)
        {
            return;
        }

        if (shouldTelegraphNext)
        {
            TelegraphCharge();
        }
        else
        {
            ExecuteCharge();
        }

        shouldTelegraphNext = !shouldTelegraphNext;
    }

    private void FixedUpdate()
    {
        if (state == State.Dead) return;
        if (target == null) return;
        if (state == State.Telegraph) return;
        if (wallStunBeatsRemaining > 0) return;
        if (stunEffects.Count > 0) return;

        if (isCharging)
        {
            // Keep forcing charge velocity so knockback can't permanently stop the charge
            rb.linearVelocity = lockedChargeDir * chargeSpeed;
            return;
        }

        if (state == State.Telegraph)
        {
            return;
        }

        MoveTowardsTarget(chaseSpeed);
    }

    // Wall collision during a charge triggers the stun
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isCharging)
        {
            return;
        }

        if (!collision.gameObject.CompareTag(wallTag))
        {
            return;
        }

        EnterWallStun();
    }

    protected override void HandleDeath()
    {
        isCharging = false;
        animator.SetBool("Charging", false);
        SetHitboxActive(false);
        SetPlayerCollisionEnabled(false);
        HideTelegraphVisual();
        SetStunVisualActive(false);

        base.HandleDeath();
    }

    /// Beats 1 and 3. Stops the treant, locks aim direction with a small jitter,
    /// and shows the telegraph visual.
    private void TelegraphCharge()
    {
        isCharging = false;
        animator.SetBool("Charging", false);
        enemyFlip.FlipEnemy();
        SetHitboxActive(false);
        SetPlayerCollisionEnabled(false);
        StopMovement();

        lockedChargeDir = CalculateJitteredDirection();
        ShowTelegraphVisual(lockedChargeDir);

        state = State.Telegraph;
    }

    /// Beats 2 and 4. Launches the charge in the locked direction.
    /// The charge continues until hitting a wall — there is no beat-based stop like the boar.
    private void ExecuteCharge()
    {
        HideTelegraphVisual();

        isCharging = true;
         animator.SetBool("Charging", true);
        SetHitboxActive(true);
        SetPlayerCollisionEnabled(true);
        rb.linearVelocity = lockedChargeDir * chargeSpeed;

        state = State.Chase;
    }

    private void EnterWallStun()
    {
        isCharging = false;
        SetHitboxActive(false);
        SetPlayerCollisionEnabled(false);
        StopMovement();
        wallStunBeatsRemaining = wallStunBeats;
        SetStunVisualActive(true);
        state = State.Recover;
    }

    private void ExitWallStun()
    {
        SetStunVisualActive(false);
        shouldTelegraphNext = true;  // always telegraph before charging again after a stun
        state = State.Chase;
    }

    private Vector2 CalculateJitteredDirection()
    {
        Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;
        float baseAngle = Mathf.Atan2(toTarget.y, toTarget.x);
        float jitterRad = Random.Range(-telegraphJitterDegrees, telegraphJitterDegrees) * Mathf.Deg2Rad;
        float finalAngle = baseAngle + jitterRad;

        return new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));
    }

    private void ShowTelegraphVisual(Vector2 direction)
    {
        if (telegraphVisual == null)
        {
            return;
        }

        float degrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        telegraphVisual.transform.rotation = Quaternion.Euler(0.0f, 0.0f, degrees);
        telegraphVisual.SetActive(true);
    }

    private void HideTelegraphVisual()
    {
        if (telegraphVisual == null)
        {
            return;
        }

        telegraphVisual.SetActive(false);
    }

    private void SetHitboxActive(bool active)
    {
        if (hitbox == null)
        {
            return;
        }

        hitbox.SetEnabled(active);
    }

    private void SetPlayerCollisionEnabled(bool enabled)
    {
        if (target == null || bodyCollider == null)
        {
            return;
        }

        Collider2D playerCol = target.GetComponent<Collider2D>();
        if (playerCol != null)
        {
            Physics2D.IgnoreCollision(bodyCollider, playerCol, !enabled);
        }
    }

    private void SetStunVisualActive(bool active)
    {
        if (stunVisual == null)
        {
            return;
        }

        stunVisual.SetActive(active);
    }
}
