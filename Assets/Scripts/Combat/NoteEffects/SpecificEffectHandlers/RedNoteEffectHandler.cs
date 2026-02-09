using UnityEngine;
using System.Collections.Generic;

//red attack mapped to <e>
public class RedNoteEffectHandler : NoteEffectHandler
{
    [Header("Fireball AOE specs")]
    [SerializeField] private float knockbackRadius = 10.0f;
    [SerializeField] private float knockbackForce = 500.0f;
    [SerializeField] private float damage = 20.0f;

    public override void HitEnemy(IDamageable damageable)
    {
        Health enemyHealth = (Health)damageable;
        Debug.Log("Enemy Health: {enemyHealth}");
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

        Debug.Log("RedNoteEffectHandler: All references good, calling FireProjectile...");
        playerAttack.FireProjectile(note, this);
    }

    public void FixedUpdate()
    {

    }
}
