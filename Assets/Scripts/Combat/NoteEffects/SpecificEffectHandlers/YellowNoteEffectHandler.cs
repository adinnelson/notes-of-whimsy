using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class YellowNoteEffectHandler : NoteEffectHandler
{
    float detectionRadius = 2.5f;

    float timeElapsed = 0.0f;

    float damage = 25f;

    float cooldown = 5;

    bool onCooldown = false;

    bool lightingEffectInProgress = false;


    void FixedUpdate() 
    {
        if (!lightingEffectInProgress) 
        {
            if(!onCooldown) return;

            if(timeElapsed >= cooldown)
            {
                onCooldown = false;
                timeElapsed = 0.0f;
                return;
            }

            timeElapsed += Time.fixedDeltaTime;
            return;
        }

        if(enemies.Count <= 0)
        {
            lightingEffectInProgress = false;
            return;
        }


        if(timeElapsed >= 1.0f)
        {
            enemies[0].GetComponent<Health>().TakeDamage(damage);
            enemies.RemoveAt(0);
            timeElapsed = 0;
        }

        timeElapsed +=  Time.fixedDeltaTime;
    }

    public override void Fire()
    {
        if (onCooldown) return;

        playerAttack.FireProjectile(note, this);
        onCooldown = true;
    }

    List<EnemyBase> enemies = new List<EnemyBase>();
    public override void HitEnemy(IDamageable damageable)
    {
        enemies.Clear();
        
        Health enemyHealth = (Health)damageable;

        
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(enemyHealth.transform.position, detectionRadius);

        Debug.Log(hitColliders.Length);

        foreach(Collider2D collider in hitColliders)
        {
            if(collider.gameObject == this) continue;

            EnemyBase enemy = collider.gameObject.GetComponent<EnemyBase>();

            if(enemy == null) continue;

            enemies.Add(enemy);

            lightingEffectInProgress = true;
        }

        
    }
}
