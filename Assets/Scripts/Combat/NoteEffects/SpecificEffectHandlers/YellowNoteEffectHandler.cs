using UnityEngine;

public class YellowNoteEffectHandler : NoteEffectHandler
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
        playerAttack.FireProjectile(note, this);
    }

    public override void HitEnemy(IDamageable damageable)
    {

    }
}
