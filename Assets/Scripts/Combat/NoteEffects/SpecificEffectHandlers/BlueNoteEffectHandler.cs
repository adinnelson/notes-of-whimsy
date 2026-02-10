using UnityEngine;

public class BlueNoteEffectHandler : NoteEffectHandler
{
    // Bool tracking if the projectile is currently instanced (either idle or active)
    private bool isProjectileInstanced;
    private BlueProjectileController projectileInstance;
    private Transform playerTransform => playerAttack.transform;

    public override void Fire()
    {
        // playerAttack.FireProjectile(note, this);

        if (!isProjectileInstanced)
        {
            // Instantiate and initialize the projectile
            projectileInstance = Instantiate(note, playerTransform.position, Quaternion.identity).GetComponent<BlueProjectileController>();
            projectileInstance.Initialize(playerAttack);
            isProjectileInstanced = true;
        }

        projectileInstance.Activate();
    }

    // Handled by BlueProjectileController
    public override void HitEnemy(IDamageable damageable)
    {

    }
}
