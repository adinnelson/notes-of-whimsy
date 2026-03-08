using UnityEngine;

/// Attached to the DamageHitbox child of the TreantEnemy prefab.
/// Only deals contact damage while charging — TreantEnemy toggles this around charge beats.
/// Damage fires once per charge activation to prevent repeated hits while the treant carries the player.
[RequireComponent(typeof(Collider2D))]
public class TreantHitbox : MonoBehaviour
{
    [SerializeField] private LayerMask damageableLayers;

    private float damage;
    private Collider2D triggerCollider;
    private bool hasDealtDamageThisCharge = false;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();

        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"{name}: TreantHitbox collider must have IsTrigger enabled.");
        }
    }

    /// Called by TreantEnemy in Awake to pass the damage value from EnemyConfig.
    public void Initialize(float configDamage)
    {
        damage = configDamage;
    }

    public void SetEnabled(bool active)
    {
        if (triggerCollider == null)
        {
            return;
        }

        triggerCollider.enabled = active;

        // Reset the one-shot flag each time a new charge begins
        if (active)
        {
            hasDealtDamageThisCharge = false;
        }
    }

    // Fires once when the hitbox trigger overlaps another collider.
    // Skips if damage was already dealt this charge, or if the hit object isn't on a damageable layer.
    // Walks up the hit object's hierarchy to find an IDamageable and deals damage once, then locks
    // further damage until the next charge begins.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasDealtDamageThisCharge)
        {
            return;
        }

        if ((damageableLayers.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            hasDealtDamageThisCharge = true;
        }
    }
}
