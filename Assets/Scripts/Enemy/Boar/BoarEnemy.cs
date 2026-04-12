using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
/// Fodder enemy that telegraphs a short charge on beats 1 and 3, then executes it on beats 2 and 4.
/// The damage hitbox is only active while charging, so the player can walk through the boar freely if it's not charging

public class BoarEnemy : EnemyBase
{
    [Header("Boar - Telegraph")]
    [Tooltip("Max random angular offset (degrees) applied to the charge direction. " + "Prevents a pack of boars from stacking their charges when kited.")]
    [SerializeField] private float telegraphJitterDegrees = 20.0f;

    [Tooltip("Child GameObject shown during the telegraph beat pointing in the charge direction. " + "The sprite must point RIGHT (+X) by default.")]
    [SerializeField] private ChargeIndicator telegraphVisual;

    [Header("Boar - Charge")]
    [SerializeField] private float chargeSpeed = 30.0f;
    [SerializeField] private float chargeTime = 0.1f;

    [Header("Boar - Chase")]
    [SerializeField] private float chaseSpeed = 2.5f;

    [Header("Boar - Spread")]
    [Tooltip("Minimum distance between any two boar charge endpoints.")]
    [SerializeField] private float minEndpointSeparation = 2.0f;
    [Tooltip("How far to nudge the endpoint each attempt when it overlaps another boar.")]
    [SerializeField] private float nudgeDistance = 1.5f;
    [Tooltip("Max number of nudge rings to try before giving up.")]
    [SerializeField] private int maxNudgeAttempts = 3;

    // Shared across all boar instances, each boar registers its endpoint here
    // so subsequent boars can pick a different spot
    private static readonly List<Vector2> claimedEndpoints = new List<Vector2>();

    private BoarHitbox hitbox;
    private Collider2D bodyCollider;
    private Vector2 lockedChargeDir;
    private float lockedChargeDistance;
    private bool isCharging = false;
    private bool shouldTelegraphNext = true;
    private Vector3 telegraphBaseScale;

    private Animator animator;

    private EnemyFlip enemyFlip;

    float chargeTimer = 0f;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        enemyFlip = GetComponent<EnemyFlip>();
        bodyCollider = GetComponent<Collider2D>();

        // Prevent fast charges from tunneling through walls
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        hitbox = GetComponentInChildren<BoarHitbox>(includeInactive: true);
        if (hitbox == null)
        {
            Debug.LogWarning($"{name}: No BoarHitbox found in children. " + "Add a child GameObject with a trigger Collider2D and BoarHitbox component.");
        }
        else
        {
            hitbox.Initialize(config.damage);
        }
        if (telegraphVisual != null)
        {
            telegraphBaseScale = telegraphVisual.transform.localScale;
        }

        SetHitboxActive(false);
        SetPlayerCollisionEnabled(false);
    }

    // Boar has no range gate — it always enters the charge cycle once aggroed
    protected override bool CanStartAttack() => true;

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
        if (isCharging)
        {
            chargeTimer -= Time.fixedDeltaTime;
            if (chargeTimer < 0)
            {
                CancelChargeForKnockback();
                StopMovement();
            }
        }
        if (state == State.Telegraph) return;
        if (stunEffects.Count > 0) return;

        //MoveTowardsTarget(chaseSpeed);
    }

    protected override void HandleDeath()
    {
        isCharging = false;
        SetHitboxActive(false);
        SetPlayerCollisionEnabled(false);
        HideTelegraphVisual();

        base.HandleDeath();
    }

    /// Beats 1 and 3. Stops the boar, locks the charge direction with a small random jitter,
    /// and shows the telegraph visual. If the endpoint would overlap another boar's,
    /// nudges it in cardinal directions until it's clear.
    private void TelegraphCharge()
    {
        isCharging = false;
        animator.SetBool("Charging", false);
        enemyFlip.FlipEnemy();
        SetHitboxActive(false);
        SetPlayerCollisionEnabled(false);  // player can walk through while telegraphing/idle
        StopMovement();

        lockedChargeDir = CalculateJitteredDirection();
        lockedChargeDistance = CalculateWallCappedDistance(lockedChargeDir);

        Vector2 endpoint = (Vector2)transform.position + lockedChargeDir * lockedChargeDistance;
        endpoint = NudgeEndpointIfOverlapping(endpoint);

        // Recompute direction and distance from the (possibly nudged) endpoint
        Vector2 toEndpoint = endpoint - (Vector2)transform.position;
        lockedChargeDir = toEndpoint.normalized;
        lockedChargeDistance = CalculateWallCappedDistance(lockedChargeDir);

        claimedEndpoints.Add(endpoint);

        ShowTelegraphVisual(lockedChargeDir, lockedChargeDistance);

        state = State.Telegraph;
    }

    /// Beats 2 and 4. Launches the boar in the locked direction and enables the damage hitbox.
    /// The charge lasts exactly one beat — TelegraphCharge on the following beat cleans it up.
    private void ExecuteCharge()
    {
        HideTelegraphVisual();

        // Clear the shared list on charge beats so the next telegraph round starts fresh.
        // Every boar calls this but Clear() on an already-empty list is harmless.
        claimedEndpoints.Clear();

        isCharging = true;
        animator.SetBool("Charging", true);
        attackSoundEffect();
        SetHitboxActive(true);
        SetPlayerCollisionEnabled(true);  // body collider on — boar carries the player

        // Cap charge duration so the boar can't overshoot a nearby wall
        float maxTime = lockedChargeDistance / chargeSpeed;
        chargeTimer = Mathf.Min(chargeTime, maxTime);

        rb.linearVelocity = lockedChargeDir * chargeSpeed;

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

        // Everything overlaps, just use the original
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

    /// Raycasts along the charge direction and returns the distance to the nearest wall,
    /// or the full charge distance if no wall is in the way.
    private float CalculateWallCappedDistance(Vector2 direction)
    {
        float maxDistance = chargeSpeed * chargeTime;

        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction, maxDistance);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            // Ignore walls we're already touching to prevent shrinking the telegraph to nothing
            if (hit.distance < 0.3f) continue;
            if (hit.collider.CompareTag("Walls") && hit.distance < maxDistance)
            {
                maxDistance = hit.distance;
            }
        }

        return maxDistance;
    }

    private void ShowTelegraphVisual(Vector2 direction, float chargeDistance)
    {
        if (telegraphVisual == null)
        {
            return;
        }
        //float degrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        //telegraphVisual.transform.rotation = Quaternion.Euler(0.0f, 0.0f, degrees);

        //telegraphVisual.transform.localScale = new Vector3(chargeDistance, telegraphBaseScale.y, telegraphBaseScale.z);
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

    /// Toggles whether the boar's body collider physically blocks/pushes the player.
    /// Enabled during charge so the boar carries the player with it; disabled otherwise
    /// so the player can freely walk through the boar body.
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

    public void CancelChargeForKnockback()
    {
        if (state == State.Dead) return;

        isCharging = false;
        animator.SetBool("Charging", false);

        SetHitboxActive(false);
        SetPlayerCollisionEnabled(false);
        HideTelegraphVisual();

        shouldTelegraphNext = true;
        state = State.Chase;

        // Stun briefly so FixedUpdate doesn't overwrite the knockback velocity
        // with MoveTowardsTarget on the very next physics frame
        //KnockbackStunRoutine();
    }

    protected override void attackSoundEffect()
    {
        if (!attackSound.IsNull)
        {
            //Apply attack sound for the boar
            var dashingInstance = RuntimeManager.CreateInstance(attackSound);

            dashingInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
            dashingInstance.start();
            dashingInstance.release();
        }
    }

    // This doesn't seem to be required and I believe is causing the boars to disapper
    //private void KnockbackStunRoutine()
    //{
       // const string knockbackStunKey = "knockback";
        //AddStunEffect(knockbackStunKey);
        
       // SimpleTimer timer = new SimpleTimer();

        //timer.StartTimer(0.35f, onFinish: () => RemoveStunEffect(knockbackStunKey));
        //RemoveStunEffect(knockbackStunKey);
   // }
}
