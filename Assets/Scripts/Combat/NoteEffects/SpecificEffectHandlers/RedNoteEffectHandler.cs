using UnityEngine;
using System.Collections.Generic;
using System.Linq;

//red note attack mapped to <e>
public class RedNoteEffectHandler : NoteEffectHandler
{
    [Header("Fireball AOE specs")]
    [SerializeField] private float knockbackRadius = 10.0f;
    [SerializeField] private float knockbackForce = 500.0f;
    [SerializeField] private float damage = 20.0f;
    [SerializeField] private LayerMask enemyLayer; 

    List<GameObject> enemiesInRange = new List<GameObject>();
    public override void HitEnemy(IDamageable damageable)
    {
        enemiesInRange.Clear();

        MonoBehaviour enemyComponent = damageable as MonoBehaviour;

        if (enemyComponent != null)
        {
            Vector3 impactPosition = enemyComponent.transform.position;
            Vector3 playerPosition = playerAttack.transform.position;
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(impactPosition, knockbackRadius, enemyLayer);

            foreach (Collider2D hit in hitColliders)
            {
                enemiesInRange.Add(hit.gameObject);
                Debug.Log($"AOE caught: {hit.gameObject.name}");
            }
            Debug.Log($"Explosion hit {enemiesInRange.Count} enemies on the Enemy layer.");

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

            if (rb != null)
            {
                Vector2 direction = (target.transform.position - moveAwayFrom).normalized;
                Vector3 offset = target.transform.position - impactPoint;

                if (offset.sqrMagnitude < 0.0001f)
                {
                    direction = (target.transform.position - moveAwayFrom).normalized;
                    Debug.Log($"Primary Target {target.name} hit! Knocking back from player.");
                }
                else
                {
                    direction = offset.normalized;
                }

                rb.velocity = Vector2.zero;
                rb.AddForce(direction * knockbackForce);
                Debug.Log($"Knocking back {target.name} with {knockbackForce} force.");
            }
        }
    }

    private void DoDamage(List<GameObject> targets)
    {
        if (targets == null || !targets.Any())
        {
            return;
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

        playerAttack.FireProjectile(note, this);
    }

    public void FixedUpdate()
    {

    }
}
