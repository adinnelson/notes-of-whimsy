using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    private Dictionary<ItemType, SpellDataSO> activeSpells = new Dictionary<ItemType, SpellDataSO>(); //keep track of spells -- only allowed 1 of each type
    private Dictionary<ItemType, GameObject> activeIcon = new Dictionary<ItemType, GameObject>(); //keep track of the spell Icons -- swap them out when new spell is learned

    [SerializeField] private BeatHandler beatHandler;

    [Header("InventoryUI")]
    [SerializeField] private Transform contentsParent;

    private void OnTriggerEnter2D(Collider2D objToPickup)
    {
        PickupItem pickup = objToPickup.GetComponent<PickupItem>();

        if (pickup == null || (pickup.spell == null && pickup.tickId == 0))
        {
            Debug.LogWarning($"Object to pickup was empty! Collider: {objToPickup.name}");
            return;
        }

        if (pickup.tickId != 0)
        {
            beatHandler.BeatUnlocked(pickup.tickId);
            Destroy(objToPickup.gameObject);
            return;
        }

        SpellDataSO newSpell = pickup.spell;
        ItemType type = newSpell.SpellType;

        /*
         * only 1 spell of each ItemType allowed at a time. 
         * if we find a new spell but already have a spell of that ItemType known, we automatically swap for the new one.
         * future task: no auto swap new spell -> will have a pop up of stats for the new spell and player can choose to replace active spell with the new spell
         */
        if (activeSpells.ContainsKey(type))
        {
            Debug.Log($"Swapping {activeSpells[type].name} for {newSpell.name}");
            if (activeIcon.ContainsKey(type))
            {
                Destroy(activeIcon[type]);
                activeIcon.Remove(type);
            }
        }

        activeSpells[type] = newSpell;
        SpawnIcon(newSpell);
        Destroy(objToPickup.gameObject);
    }

    private void SpawnIcon(SpellDataSO spell)
    {
        if (spell.UIIconPrefab == null)
        {
            Debug.LogWarning($"No UI Icon Prefab assigned for {spell.name}");
            return;
        }

        GameObject iconInstance = Instantiate(spell.UIIconPrefab, contentsParent);
        iconInstance.SetActive(true);
        activeIcon[spell.SpellType] = iconInstance;
    }

    public SpellDataSO GetSpellByType(ItemType type)
    {
        return activeSpells.TryGetValue(type, out SpellDataSO spell) ? spell : null;
    }

    /*
     * FUTURE TODO:
     * public void SaveInventory()
     * public void LoadInventory()
     * either use a spell library with all spells accessible in a specific spell folder 
     *              or 
     * can use SO SpellDatabase with: public List<SpellDataSO> allSpellsDatabase;
     */
}
