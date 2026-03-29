using UnityEngine;

/// <summary>
/// Child hitbox for the DeerBoss slam attack (end of CHARGE sequence).
/// Positioned in front of the boss facing the player on the telegraph beat,
/// then enabled on the slam beat and disabled after.
///
/// Setup: child GameObject under DeerBoss with a trigger BoxCollider2D
/// and a SpriteRenderer for the warning/impact visual.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DeerBossSlamHitBox : MonoBehaviour
{
    [SerializeField] private LayerMask damageableLayers;

    private float damage;
    private Collider2D triggerCollider;
    private SpriteRenderer sprite;
    private bool hasDealtDamage = false;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();

        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"{name}: DeerBossSlamHitbox collider must have IsTrigger enabled.");
        }

        triggerCollider.enabled = false;
        if (sprite != null) sprite.enabled = false;
    }

    public void Initialize(float configDamage)
    {
        damage = configDamage;
    }

    /// <summary>
    /// Show the slam warning visual and position the hitbox.
    /// Called on the telegraph beat. Does not enable the collider yet.
    /// </summary>
    public void Telegraph(Vector2 direction, Vector2 size, float offsetDistance)
    {
        // Position in front of boss (local space)
        transform.localPosition = (Vector3)(direction * offsetDistance);

        // Rotate to face the direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        // Scale to match slam size
        transform.localScale = new Vector3(size.x, size.y, 1f);

        // Show warning visual but don't enable damage yet
        if (sprite != null) sprite.enabled = true;
        triggerCollider.enabled = false;
    }

    /// <summary>
    /// Activate the slam damage. Called on the slam beat.
    /// </summary>
    public void Activate()
    {
        hasDealtDamage = false;
        triggerCollider.enabled = true;
    }

    /// <summary>
    /// Disable everything. Called after the slam or on cleanup.
    /// </summary>
    public void Deactivate()
    {
        triggerCollider.enabled = false;
        if (sprite != null) sprite.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasDealtDamage) return;

        if ((damageableLayers.value & (1 << other.gameObject.layer)) == 0) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            hasDealtDamage = true;
        }
    }
}
