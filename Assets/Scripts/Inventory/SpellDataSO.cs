using UnityEngine;

[CreateAssetMenu(fileName = "NewSpell", menuName = "Scriptable Objects/Spells")]
public class SpellDataSO : ScriptableObject
{
    [Header("Type")]
    public ItemType SpellType;

    [Header("Visuals")]
    public Note NoteProjectilePrefab; //prefab for note effects
    public GameObject UIIconPrefab; //prefab for UI Icon
}
