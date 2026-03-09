using UnityEngine;

/// Fodder enemy that telegraphs a short charge on beats 1 and 3, then executes it on beats 2 and 4.
/// The damage hitbox is only active while charging, so the player can walk through the boar freely if it's not charging

public class BoarEnemy : EnemyBase
{
    [Header("Boar - Telegraph")]
    [Tooltip("Max random angular offset (degrees) applied to the charge direction. " + "Prevents a pack of boars from stacking their charges when kited.")]
    [SerializeField] private float telegraphJitterDegrees = 20.0f;

    [Tooltip("Child GameObject shown during the telegraph beat pointing in the charge direction. " + "The sprite must point RIGHT (+X) by default.")]
    [SerializeField] private GameObject telegraphVisual;

    [Header("Boar - Charge")]
    [SerializeField] private float chargeSpeed = 14.0f;

    [Header("Boar - Chase")]
    [SerializeField] private float chaseSpeed = 2.5f;

    private BoarHitbox hitbox;
    private Collider2D bodyCollider;
    private Vector2 lockedChargeDir;
    private bool isCharging = false;
    private bool shouldTelegraphNext = true;

    protected override void Awake()
    {
        base.Awake();

        bodyCollider = GetComponent<Collider2D>();

        hitbox = GetComponentInChildren<BoarHitbox>(includeInactive: true);
        if (hitbox == null)
        {
            Debug.LogWarning($"{name}: No BoarHitbox found in children. " + "Add a child GameObject with a trigger Collider2D and BoarHitbox component.");
        }
        else
        {
            hitbox.Initialize(config.damage);
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
        if (isCharging) return;
        if (state == State.Telegraph) return;
        if (stunEffects.Count > 0) return;

        MoveTowardsTarget(chaseSpeed);
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
    /// and shows the telegraph visual. Jitter prevents boar packs from stacking charges.
    private void TelegraphCharge()
    {
        isCharging = false;
        SetHitboxActive(false);
        SetPlayerCollisionEnabled(false);  // player can walk through while telegraphing/idle
        StopMovement();

        lockedChargeDir = CalculateJitteredDirection();
        ShowTelegraphVisual(lockedChargeDir);

        state = State.Telegraph;
    }

    /// Beats 2 and 4. Launches the boar in the locked direction and enables the damage hitbox.
    /// The charge lasts exactly one beat — TelegraphCharge on the following beat cleans it up.
    private void ExecuteCharge()
    {
        HideTelegraphVisual();

        isCharging = true;
        SetHitboxActive(true);
        SetPlayerCollisionEnabled(true);  // body collider on — boar carries the player

        rb.linearVelocity = lockedChargeDir * chargeSpeed;

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
}
