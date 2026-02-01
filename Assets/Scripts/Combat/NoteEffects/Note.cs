using UnityEngine;
using System;

public class Note : Projectile
{
    [SerializeField] private string key;
    [SerializeField] private string name;
    [SerializeField] private string desc;
 
    private NoteEffectHandler noteEffectHandler;

    public void Activate(Vector3 spawnPosition, Vector2 travelDirection, NoteEffectHandler noteEffectHandler = null)
    {
        base.Activate(spawnPosition, travelDirection);

        this.noteEffectHandler = noteEffectHandler;
    }

    // overrides generic projectile as we want to activate effect
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) 
        {
            return;
        }

        // Don't hit yourself
        if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == gameObject)
        {
            return;
        }

        // If the thing we hit can take damage, damage it
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            noteEffectHandler?.HitEnemy(damageable);
            Despawn();
        }
    }
}
