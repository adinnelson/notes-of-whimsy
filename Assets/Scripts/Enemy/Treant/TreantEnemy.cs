using UnityEngine;
using System.Collections.Generic;
using FMODUnity;
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
    [SerializeField] private ChargeIndicator telegraphVisual;
    [SerializeField] private string wallTag = "Walls";

    [Header("Treant - Wall Stun")]
    [SerializeField] private int wallStunBeats = 2;
    [SerializeField] private GameObject stunVisual;

    [Header("Treant - Spread")]
    [Tooltip("Minimum distance between any two treant charge endpoints.")]
    [SerializeField] private float minEndpointSeparation = 2.0f;
    [Tooltip("How far to nudge the endpoint each attempt when it overlaps another treant.")]
    [SerializeField] private float nudgeDistance = 1.5f;
    [Tooltip("Max number of nudge rings to try before giving up.")]
    [SerializeField] private int maxNudgeAttempts = 3;

    // Shared across all treant instances each treant registers its endpoint here
    // so subsequent treants can pick a different spot
    private static readonly List<Vector2> claimedEndpoints = new List<Vector2>();

    private TreantHitbox hitbox;
    private Collider2D bodyCollider;

    private Vector2 lockedChargeDir;
    private bool isCharging = false;
    private bool shouldTelegraphNext = true;
    private int wallStunBeatsRemaining = 0;
    private Vector3 telegraphBaseScale;
    private Animator animator;

    // Track position between physics frames to detect when the charge is blocked
    private Vector2 lastChargePosition;
    private int stuckFrames = 0;
    private const int stuckFrameThreshold = 3;

    private EnemyFlip enemyFlip;

    protected override void Awake()
    {
        base.Awake();
        health.OnDeath += telegraphVisual.ReleaseIndicator;
        
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

        if (telegraphVisual != null)
        {
            telegraphBaseScale = telegraphVisual.transform.localScale;
        }
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
            StunVisuals();
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

        int zSub = (int)(gameObject.transform.position.y / GameManager.Z_RANGE);

        gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.y - GameManager.Z_RANGE * zSub);
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
            // Check if the treant has barely moved since last physics frame.
            // If blocked for several frames (e.g. player pinned against a wall),
            // treat it the same as hitting a wall.
            float distanceMoved = Vector2.Distance(rb.position, lastChargePosition);
            float expectedDistance = chargeSpeed * Time.fixedDeltaTime * 0.5f;

            if (distanceMoved < expectedDistance)
            {
                stuckFrames++;
                if (stuckFrames >= stuckFrameThreshold)
                {
                    EnterWallStun();
                    return;
                }
            }
            else
            {
                stuckFrames = 0;
            }

            lastChargePosition = rb.position;
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
    /// and shows the telegraph visual. If the endpoint would overlap another treant's,
    /// nudges it in cardinal directions until it's clear.
    private void TelegraphCharge()
    {
        isCharging = false;
        animator.SetBool("Charging", false);
        enemyFlip.FlipEnemy();
        SetHitboxActive(false);
        SetPlayerCollisionEnabled(false);
        StopMovement();

        lockedChargeDir = CalculateJitteredDirection();

        float wallDist = RaycastWallDistance(lockedChargeDir);
        Vector2 endpoint = (Vector2)transform.position + lockedChargeDir * wallDist;
        endpoint = NudgeEndpointIfOverlapping(endpoint);

        // Recompute direction from the (possibly nudged) endpoint
        lockedChargeDir = (endpoint - (Vector2)transform.position).normalized;

        claimedEndpoints.Add(endpoint);

        ShowTelegraphVisual(lockedChargeDir);

        state = State.Telegraph;
    }

    /// Beats 2 and 4. Launches the charge in the locked direction.
    /// The charge continues until hitting a wall — there is no beat-based stop like the boar.
    private void ExecuteCharge()
    {
        HideTelegraphVisual();

        // Clear the shared list on charge beats so the next telegraph round starts fresh.
        // Every treant calls this but Clear() on an already-empty list is harmless.
        claimedEndpoints.Clear();

        isCharging = true;
        stuckFrames = 0;
        lastChargePosition = rb.position;
        animator.SetBool("Charging", true);
        attackSoundEffect();
        SetHitboxActive(true);
        SetPlayerCollisionEnabled(true);
        rb.linearVelocity = lockedChargeDir * chargeSpeed;

        state = State.Chase;
    }

    private void EnterWallStun()
    {
        isCharging = false;
        animator.SetBool("Charging", false);
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

    // Cardinal nudge offsets: up, right, down, left
    private static readonly Vector2[] nudgeDirections = new Vector2[]
    {
        Vector2.up, Vector2.right, Vector2.down, Vector2.left
    };

    /// Checks the endpoint against all claimed spots. If it's too close to any,
    /// tries nudging it up/right/down/left by increasing multiples of nudgeDistance.
    /// Returns the original endpoint if nothing is claimed or it's already clear.
    private Vector2 NudgeEndpointIfOverlapping(Vector2 endpoint)
    {
        if (IsEndpointClear(endpoint))
        {
            return endpoint;
        }

        for (int ring = 1; ring <= maxNudgeAttempts; ring++)
        {
            float offset = nudgeDistance * ring;
            foreach (Vector2 dir in nudgeDirections)
            {
                Vector2 nudged = endpoint + dir * offset;
                if (IsEndpointClear(nudged))
                {
                    return nudged;
                }
            }
        }

        // if Everything overlaps then just use the original
        return endpoint;
    }

    private bool IsEndpointClear(Vector2 candidate)
    {
        foreach (Vector2 claimed in claimedEndpoints)
        {
            if (Vector2.Distance(candidate, claimed) < minEndpointSeparation)
            {
                return false;
            }
        }
        return true;
    }

    private Vector2 CalculateJitteredDirection()
    {
        Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;
        float baseAngle = Mathf.Atan2(toTarget.y, toTarget.x);
        float jitterRad = Random.Range(-telegraphJitterDegrees, telegraphJitterDegrees) * Mathf.Deg2Rad;
        float finalAngle = baseAngle + jitterRad;

        return new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));
    }

    /// Raycasts along a direction and returns the distance to the nearest wall,
    /// or maxRayDist if no wall is found. Used by both the spread logic and the
    /// telegraph visual scaling.
    private float RaycastWallDistance(Vector2 direction)
    {
        const float maxRayDist = 50.0f;
        float chargeDistance = maxRayDist;
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction, maxRayDist);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;  // ignore self
            // Ignore walls we're already touching to prevent it from thinking we're right up against a wall and shrinking the telegraph to nothing
            if (hit.distance < 0.6f) continue;
            if (hit.collider.CompareTag("Walls") && hit.distance < chargeDistance)
            {
                chargeDistance = hit.distance;
            }
        }

        return chargeDistance;
    }

    private void ShowTelegraphVisual(Vector2 direction)
    {
        if (telegraphVisual == null)
        {
            return;
        }

        float chargeDistance = RaycastWallDistance(direction);

        telegraphVisual.Init((Vector2)transform.position - 0.25f * Vector2.up, (Vector2)transform.position + direction * chargeDistance);

        telegraphVisual.gameObject.SetActive(true);
    }

    private void HideTelegraphVisual()
    {
        if (telegraphVisual == null)
        {
            return;
        }

        telegraphVisual.ReleaseIndicator();

        telegraphVisual.gameObject.SetActive(false);
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

    protected override void attackSoundEffect()
    {
        if (!attackSound.IsNull)
        {
            //Apply attack sound for the treant
            var dashingInstance = RuntimeManager.CreateInstance(attackSound);

            dashingInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
            dashingInstance.start();
            dashingInstance.release();
        }
    }
}
