using UnityEngine;

/// Attached to the DamageHitbox child of the BoarEnemy prefab.
/// Only deals contact damage while enabled — BoarEnemy toggles this around charge beats
/// so the player can freely walk through the boar body when it is not attacking.
[RequireComponent(typeof(Collider2D))]
public class BoarHitbox : MonoBehaviour
{
    private float damage;
    private Collider2D triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();

        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"{name}: BoarHitbox collider must have IsTrigger enabled.");
        }
    }

    /// Called by BoarEnemy in Awake to pass the damage value from EnemyConfig,
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
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
    }
}
