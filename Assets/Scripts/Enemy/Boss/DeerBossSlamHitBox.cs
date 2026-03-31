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
    private const int OVERLAP_BUFFER_SIZE = 8;

    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] private Color telegraphColor = new Color(1.0f, 0.45f, 0.2f, 0.45f);
    [SerializeField] private Color impactColor = new Color(1.0f, 0.9f, 0.35f, 0.9f);

    private float damage;
    private Collider2D triggerCollider;
    private SpriteRenderer sprite;
    private bool hasDealtDamage = false;
    private readonly Collider2D[] overlapResults = new Collider2D[OVERLAP_BUFFER_SIZE];
    private Vector2 lastTelegraphedSize = Vector2.one;
    private float lastTelegraphedAngle = 0.0f;

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

    /// <summary>
    /// Sets slam damage.
    /// </summary>
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
        transform.localRotation = Quaternion.Euler(0.0f, 0.0f, angle);
        lastTelegraphedAngle = angle;
        lastTelegraphedSize = size;

        // Scale to match slam size
        transform.localScale = new Vector3(size.x, size.y, 1.0f);

        // Show warning visual but don't enable damage yet
        if (sprite != null)
        {
            sprite.color = telegraphColor;
            sprite.enabled = true;
        }
        triggerCollider.enabled = false;
    }

    /// <summary>
    /// Activate the slam damage. Called on the slam beat.
    /// </summary>
    public void Activate(Transform playerTarget = null)
    {
        hasDealtDamage = false;

        if (sprite != null)
        {
            sprite.color = impactColor;
            sprite.enabled = true;
        }

        triggerCollider.enabled = true;
        TryDamagePlayerTarget(playerTarget);
        ApplyDamageToOverlaps();
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
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void ApplyDamageToOverlaps()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.useTriggers = true;
        filter.SetLayerMask(damageableLayers);

        int count = Physics2D.OverlapBox(
            transform.position,
            lastTelegraphedSize,
            lastTelegraphedAngle,
            filter,
            overlapResults);

        for (int i = 0; i < count; i++)
        {
            TryDamage(overlapResults[i]);
            if (hasDealtDamage)
            {
                return;
            }
        }
    }

    private void TryDamage(Collider2D other)
    {
        if (hasDealtDamage || other == null) return;

        // Only hit the player.
        if (!other.CompareTag("Player")) return;

        if ((damageableLayers.value & (1 << other.gameObject.layer)) == 0) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            hasDealtDamage = true;
        }
    }

    private void TryDamagePlayerTarget(Transform playerTarget)
    {
        if (hasDealtDamage || playerTarget == null)
        {
            return;
        }

        Collider2D playerCollider = playerTarget.GetComponent<Collider2D>();
        if (playerCollider == null)
        {
            return;
        }

        if ((damageableLayers.value & (1 << playerCollider.gameObject.layer)) == 0)
        {
            return;
        }

        ColliderDistance2D distanceInfo = playerCollider.Distance(triggerCollider);
        if (!distanceInfo.isOverlapped)
        {
            return;
        }

        IDamageable damageable = playerTarget.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            hasDealtDamage = true;
        }
    }
}
