using UnityEngine;

public class BlueNoteEffectHandler : NoteEffectHandler
{
    public override void Fire()
    {
        playerAttack.FireProjectile(note, this);
    }

    public override void HitEnemy(IDamageable damageable)
    {

    }
}
