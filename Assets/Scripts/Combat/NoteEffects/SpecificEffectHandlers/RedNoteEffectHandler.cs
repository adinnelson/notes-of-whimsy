using UnityEngine;

public class RedNoteEffectHandler : NoteEffectHandler
{
    public override void Fire()
    {
        playerAttack.FireProjectile(note, this);
    }

    public override void HitEnemy(IDamageable damageable)
    {

    }
}
