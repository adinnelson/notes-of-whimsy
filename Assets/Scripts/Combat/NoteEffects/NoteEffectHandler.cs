using UnityEngine;

// Generic NoteEffectHandler class to be inherited by specific note effect handlers
public abstract class NoteEffectHandler : MonoBehaviour
{
    [SerializeField] protected Note note;

    [SerializeField] protected PlayerAttack playerAttack;

    [SerializeField] protected SpellDataSO spellData;

    public SpellDataSO SpellData
    {
        get { return spellData; }
    }

    public GameObject SpellIcon
    {
        get {return spellData.UIIconPrefab; }
    }

    public abstract void Fire();

    public abstract void HitEnemy(IDamageable damageable);

    public abstract void Init(SpellDataSO spellData, PlayerAttack playerAttack);
}
