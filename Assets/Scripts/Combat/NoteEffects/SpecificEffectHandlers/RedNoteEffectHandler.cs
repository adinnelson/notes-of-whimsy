using UnityEngine;
using System.Collections.Generic;

public class RedNoteEffectHandler : NoteEffectHandler
{
    [Header("Fireball AOE specs")]
    [SerializeField] private int directDamage = 4;
    [SerializeField] private int splashDamage = 1;
    [SerializeField] private float knockbackRadius = 6.0f;
    [SerializeField] private float knockbackForce = 50.0f;
    [SerializeField] private float cooldown = 1.0f; //cooldown in beats; not enforcing cd here; cd should be pulled from here by input logic

    public override void Fire()
    {
        playerAttack.FireProjectile(note, this);
    }

    public List<Collider2D> KnockbackRadius(Vector2 centre)
    {
        return new List<Collider2D>(Physics2D.OverlapCircleAll(centre, knockbackRadius));
    }

    public void Knockback(Rigidbody2D rb, Vector2 centre)
    {
        Vector2 knockbackDirection = (rb.position - centre).normalized;
        rb.AddForce(knockbackDirection *  knockbackForce);
    }

    private void ApplyDirectDamage(IDamageable target)
    {
        target.TakeDamage(directDamage);
    }

    private void ApplySplashDamage(List<Collider2D> enemies, IDamageable directHit)
    {
        foreach (var collider in enemies)
        {
            if (collider.TryGetComponent<IDamageable>(out var damageable))
            {
                if (damageable != directHit)
                {
                    damageable.TakeDamage(splashDamage);
                }
            }
        }
    }

    public override void HitEnemy(IDamageable damageable)
    {
        Vector2 hitPosition = damageable is MonoBehaviour mb ? mb.transform.position : (Vector2)transform.position;

        //direct damage to primary enemy hit
        ApplyDirectDamage(damageable);

        //retrieve all enemies in knockback radius
        List<Collider2D> enemiesInRange = KnockbackRadius(hitPosition);

        //apply splash damage
        ApplySplashDamage(enemiesInRange, damageable);

        //apply knockback 
        foreach (var collider in enemiesInRange)
        {
            if (collider.TryGetComponent<Rigidbody2D>(out var rb))
            {
                Knockback(rb, hitPosition); // use your existing function
            }
        }
    }
}
