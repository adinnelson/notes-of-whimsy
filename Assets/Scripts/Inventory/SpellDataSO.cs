using UnityEngine;

[CreateAssetMenu(fileName = "NewSpell", menuName = "Scriptable Objects/Spells")]
public class SpellDataSO : ScriptableObject
{
    public string spellName; //should matche 'name' in Note.cs
    public string description; //should matche 'desc' in Note.cs
    public string inputKey; //should matches 'key' in Note.cs
    public ItemType spellType;


    [Header("Combat Stats")]
    public float damage;
    public float speed;
    public float lifetime;

    [Header("Visuals")]
    public GameObject noteProjectilePrefab; //prefab with the Note.cs script on it **visual effects in the world
    public GameObject uiIconPrefab; //visual for icon in the UI
}
