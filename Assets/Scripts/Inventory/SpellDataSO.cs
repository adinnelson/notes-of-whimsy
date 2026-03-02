using UnityEngine;

[CreateAssetMenu(fileName = "NewSpell", menuName = "Scriptable Objects/Spells")]
public class SpellDataSO : ScriptableObject
{
    public ItemType SpellType;
    public Note NoteProjectilePrefab; //prefab for note effects -- could be made more general by swapping Note type for GameObject type
    public GameObject UIIconPrefab; //prefab for UI Icon
    public GameObject NoteGameObj;
    public int spellId;
}
