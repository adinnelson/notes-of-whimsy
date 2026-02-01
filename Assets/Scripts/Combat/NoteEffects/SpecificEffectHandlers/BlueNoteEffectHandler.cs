using UnityEngine;

public class BlueNoteEffectHandler : NoteEffectHandler
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void FixedUpdate()
    {
        
    }

    public override void Fire()
    {
        playerAttack.FireProjectile(note);
    }

    public override void HitEnemy(IDamageable damageable)
    {

    }
}
