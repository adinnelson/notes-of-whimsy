using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class BlueProjectileController : BlueNoteEffectHandler
{
    [Tooltip("Damage dealt by the projectile on hit per physics frame")]
    [SerializeField] private float projectileDamage = 0.1f;
    [SerializeField] private float gravityScale = 1.0f;

    private Transform player;
    private Rigidbody2D rb => GetComponent<Rigidbody2D>();

    private Coroutine idleCoroutine;
    private Coroutine moveCoroutine;

    // Manually calculate velocity since the projectile is moved with SmoothDamp
    private float velocity = 0.0f;
    private float travelDistance = 4.5f;
    private float damageRadius = 0.5f;
    private float gravityRadius = 1.5f;

    private bool canActivate = true;

    private const float COOLDOWN_TIME = 4.0f;
    private const float MIN_DAMAGE_VELOCITY = 2.0f;
    private const float CIRCLING_RADIUS = 1.0f;
    private const float CIRCLING_SPEED = 1.0f;
    private const float RETURN_MAX_DELTA = 0.02f;
    // Maximum velocity of objects dragged by the projectile as a multiplier of the projectile's velocity
    private const float MAX_GRAVITY_VELOCITY_MULTIPLIER = 2.0f;

    private const float MOVE_TO_TARGET_SMOOTH_TIME = 0.3f;

    /// <summary>
    /// Initialize projectile
    /// </summary>
    public void Initialize(PlayerAttack playerAttack)
    {
        player = playerAttack.transform;
    }

    // Detach from player and move in the aim direction
    public void Activate()
    {
        if (!canActivate)
        {
            Debug.LogWarning("Blue projectile cannot be activated yet. Still in cooldown or moving.");
            return;
        }

        Vector2 aimDirection = GetMouseAimDirection();

        Vector2 startPosition = transform.position;
        Vector2 targetPosition = startPosition + aimDirection * travelDistance;

        // Move towards the target position over time
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
        }
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        canActivate = false;
        moveCoroutine = StartCoroutine(MoveTowardsTarget(targetPosition));
    }

    /// <summary>
    /// During idle, circle around the player
    /// </summary>
    private System.Collections.IEnumerator Idle()
    {
        while (true)
        {
            Vector2 newPosition = Vector2.MoveTowards(transform.position, PointCirclingPlayer(), RETURN_MAX_DELTA);
            transform.position = newPosition;

            if (!canActivate && Vector2.Distance(transform.position, PointCirclingPlayer()) < 0.5f)
            {
                canActivate = true;
            }

            yield return null;
        }
    }

    /// <summary>
    /// Coroutine to move the projectile towards the target position smoothly then call idle
    /// </summary>
    private System.Collections.IEnumerator MoveTowardsTarget(Vector2 targetPosition)
    {
        Vector2 currentVelocity = rb.linearVelocity;
        while (Vector2.Distance(transform.position, targetPosition) > 0.1f)
        {
            Vector2 newPosition = Vector2.SmoothDamp(transform.position, targetPosition, ref currentVelocity, MOVE_TO_TARGET_SMOOTH_TIME);
            velocity = Vector2.Distance(transform.position, newPosition) / Time.deltaTime;
            transform.position = newPosition;
            ApplyDamage();
            ApplyGravity();
            yield return null;
        }

        yield return new WaitForSeconds(COOLDOWN_TIME);
        idleCoroutine = StartCoroutine(Idle());
    }

    private void ApplyDamage()
    {
        // Do not apply damage if moving slowly
        if (velocity < MIN_DAMAGE_VELOCITY)
        {
            return;
        }

        // Get all colliders in the damage radius
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, damageRadius);
        foreach (Collider2D collision in hitColliders)
        {
            IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(projectileDamage);
            }
        }
    }

    /// <summary>
    /// Drag damagables towards the projectile
    /// TODO: Ensure enemies are stunned while being dragged to avoid jittering
    /// </summary>
    private void ApplyGravity()
    {
        // Get all colliders in the damage radius
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, gravityRadius);
        foreach (Collider2D collision in hitColliders)
        {
            // Do not affect the player
            if (collision.transform == player)
            {
                continue;
            }

            IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Vector2 directionToProjectile = (transform.position - collision.transform.position).normalized;
                float attractionMultiplier = (collision.transform.position - transform.position).magnitude;

                // Apply a force towards the projectile
                Rigidbody2D damageableRb = collision.gameObject.GetComponent<Rigidbody2D>();
                if (damageableRb != null)
                {
                    damageableRb.AddForce(directionToProjectile * gravityScale * attractionMultiplier, ForceMode2D.Force);

                    // Cap the velocity to prevent extreme speeds
                    Vector2 damagableVelocity = damageableRb.linearVelocity;
                    if (damagableVelocity.magnitude > velocity * MAX_GRAVITY_VELOCITY_MULTIPLIER)
                    {
                        damageableRb.linearVelocity = damagableVelocity.normalized * velocity * MAX_GRAVITY_VELOCITY_MULTIPLIER;
                    }
                }
                else
                {
                    Debug.LogWarning("Damageable object does not have a Rigidbody2D component. Cannot apply gravity effect.");
                }
            }
        }
    }

    /// <summary>
    /// Temporary function to get the direction of the mouse relative to the projectile
    /// Function written with AI assistance
    /// </summary>
    private Vector2 GetMouseAimDirection()
    {
        Vector2 projectilePosition = transform.position;
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        return (mouseWorldPos - projectilePosition).normalized;
    }

    /// <summary>
    /// Calculate a point circling around the player
    /// </summary>
    private Vector2 PointCirclingPlayer()
    {
        float speed = CIRCLING_SPEED;
        float radius = CIRCLING_RADIUS;

        float angle = Time.time * speed;
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        return (Vector2)player.position + offset;
    }
}
