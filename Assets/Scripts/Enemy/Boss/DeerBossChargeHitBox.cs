using UnityEngine;

/// <summary>
/// Child hitbox for the DeerBoss charge attacks. Same pattern as BoarHitbox —
/// the parent script toggles SetEnabled(true/false) around charge beats.
/// Deals one hit per charge activation to prevent repeated damage while
/// the boss carries the player.
///
/// Setup: child GameObject under DeerBoss with a trigger Collider2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DeerBossChargeHitBox : MonoBehaviour
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
            Debug.LogWarning($"{name}: DeerBossChargeHitbox collider must have IsTrigger enabled.");
        }

        // Start disabled
        triggerCollider.enabled = false;
    }

    public void Initialize(float configDamage)
    {
        damage = configDamage;
    }

    public void SetEnabled(bool active)
    {
        if (triggerCollider == null) return;

        triggerCollider.enabled = active;

        if (active)
        {
            hasDealtDamageThisCharge = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasDealtDamageThisCharge) return;

        // Only ever damage the player — guards against the boss hurting other enemies
        // even if damageableLayers is misconfigured in the Inspector.
        if (!other.CompareTag("Player")) return;

        if ((damageableLayers.value & (1 << other.gameObject.layer)) == 0) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            hasDealtDamageThisCharge = true;
        }
    }
}
