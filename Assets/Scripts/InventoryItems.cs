using System.Collections.Generic;
using UnityEngine;

/*
 * Inventory data currently stored as a Dictionary<ItemType, int> 
 * tracks the inventory as item counts, for now. 
 * 
 * Inventory data structure can evolve (ex. create ItemData ==> class ItemData) without altering the pickup logic.
 */
public class InventoryItems : MonoBehaviour
{
    private Dictionary<ItemType, int> items = new Dictionary<ItemType, int>(); //keeping the count of each item type for now -- obviously we can store more data associated with each item we pickup

    public void AddItem(ItemType type, int amount = 1)
    {
        if (items.ContainsKey(type))
        {
            items[type] += amount;
        }
        else
        {
            items[type] = amount;
        }
        Debug.Log($"Added {type}. Count now at: {items[type]}");
    }

    public int GetCount(ItemType type)
    {
        return items.TryGetValue(type, out int count) ? count : 0;
    }

    public bool HasItem(ItemType type, int amount = 1)
    {
        return GetCount(type) >= amount;
    }
}
