using UnityEngine;

[CreateAssetMenu(fileName = "NewSpell", menuName = "Scriptable Objects/Spells")]
public class SpellDataSO : ScriptableObject
{
    public ItemType spellType;

    //these match info in Note.cs -- can we change these in Note.cs to inherite from here?
    public string spellName;
    public string description;
    public string inputKey;

    //these match the stats found in Projectile.cs and Note.cs and can be overriden here by updating them in inspector
    [Header("Combat Stats")]
    public float damage;
    public float speed;
    public float lifetimeSeconds;

    [Header("Visuals")]
    public GameObject noteProjectilePrefab; //prefab with the Note.cs script on it **visual effects in the world
    public GameObject uiIconPrefab; //visual for icon in the UI
}
