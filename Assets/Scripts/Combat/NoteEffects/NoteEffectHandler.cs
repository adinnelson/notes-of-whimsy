using UnityEngine;

// Generic NoteEffectHandler class to be inherited by specific note effect hand;ers
public abstract class NoteEffectHandler : MonoBehaviour
{
    [SerializeField] protected Note note;

    [SerializeField] protected PlayerAttack playerAttack;

    public abstract void Fire();

    public abstract void HitEnemy(IDamageable damageable);
}
