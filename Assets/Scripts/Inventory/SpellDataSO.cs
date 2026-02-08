using UnityEngine;

[CreateAssetMenu(fileName = "NewSpell", menuName = "Scriptable Objects/Spells")]
public class SpellDataSO : ScriptableObject
{
    public ItemType spellType;

    [Header("Visuals")]
    public GameObject noteProjectilePrefab; //prefab with the Note.cs script on it **visual effects in the world
    public GameObject uiIconPrefab; //visual for icon in the UI
}
