using UnityEngine;
using System.Collections.Generic;

//all inventory data and logic stored here
public class InventoryManager
{
    private Dictionary<ItemType, int> items = new();

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
