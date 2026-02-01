using UnityEngine;

public abstract class NoteEffectHandler : MonoBehaviour
{
    [SerializeField] protected Note note;

    [SerializeField] protected PlayerAttack playerAttack;

    public abstract void Fire();

    public abstract void HitEnemy(IDamageable damageable);
}
