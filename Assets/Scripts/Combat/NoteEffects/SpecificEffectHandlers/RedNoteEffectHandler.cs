using UnityEngine;
using System.Collections.Generic;
using System.Linq;

//red note attack mapped to <e>
public class RedNoteEffectHandler : NoteEffectHandler
{
    [Header("Fireball AOE specs")]
    [SerializeField] private float knockbackRadius = 1.0f;
    [SerializeField] private float knockbackForce = 500.0f;
    [SerializeField] private float damage = 20.0f;

    List<GameObject> enemiesInRange = new List<GameObject>();

    public override void HitEnemy(IDamageable damageable)
    {
        enemiesInRange.Clear();
        MonoBehaviour enemyComponent = damageable as MonoBehaviour;

        if (enemyComponent != null)
        {
            Vector3 impactPosition = enemyComponent.transform.position;
            Vector3 playerPosition = playerAttack.transform.position;

            enemiesInRange.Add(enemyComponent.gameObject);
            DebugDrawCircle(impactPosition, knockbackRadius, Color.cyan, 2.0f);

            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(impactPosition, knockbackRadius);

            foreach (Collider2D hit in hitColliders)
            {
                if (hit.TryGetComponent<IDamageable>(out IDamageable targetDamageable))
                {
                    if (hit.CompareTag("Player"))
                    {
                        continue;
                    }

                    if (!enemiesInRange.Contains(hit.gameObject))
                    {
                        enemiesInRange.Add(hit.gameObject);
                    }
                }
            }
            DoDamage(enemiesInRange);
            Knockback(enemiesInRange, impactPosition, playerPosition);
        }
    }

    private void Knockback(List<GameObject> targets, Vector3 impactPoint, Vector3 moveAwayFrom)
    {
        if (targets == null || !targets.Any())
        {
            return;
        }

        foreach (GameObject target in targets)
        {
            Rigidbody2D rb = target.GetComponent<Rigidbody2D>();

            if (rb == null)
            {
                continue;
            }

            Vector2 direction;
            Vector3 targetPosition = target.transform.position;
            Vector3 offset = targetPosition - impactPoint;

            Debug.DrawLine(impactPoint, targetPosition, Color.yellow, 1.0f);

            if (offset.sqrMagnitude < 0.0001f)
            {
                direction = (targetPosition - moveAwayFrom).normalized;
                Debug.DrawRay(targetPosition, direction * 2, Color.red, 1.0f);
            }
            else
            {
                direction = offset.normalized;
                Debug.DrawRay(targetPosition, direction * 2, Color.blue, 1.0f);
            }

            rb.linearVelocity = Vector2.zero;
            rb.AddForce(direction * knockbackForce);
        }
    }

    private void DoDamage(List<GameObject> targets)
    {
        if (targets == null || !targets.Any())
        {
            return;
        }

        foreach (GameObject target in targets)
        {
            if (target.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
            }
        }
    }

    public override void Fire()
    {
        if (playerAttack == null)
        {
            Debug.LogError("RedNoteEffectHandler: 'playerAttack' is not assigned in the Inspector!");
            return;
        }

        if (note == null)
        {
            Debug.LogError("RedNoteEffectHandler: 'note' (the prefab) is not assigned in the Inspector!");
            return;
        }

        playerAttack.FireProjectile(note, this, this.gameObject);
    }

    private void DebugDrawCircle(Vector3 center, float radius, Color color, float duration)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);

            Debug.DrawLine(prevPoint, nextPoint, color, duration);
            prevPoint = nextPoint;
        }
    }
}
