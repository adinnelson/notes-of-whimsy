using UnityEngine;

//shifted PickupObject into PlayerInventory for clarity
public class PlayerInventory : MonoBehaviour
{
    private InventoryManager inventory = new InventoryManager();

    [Header("InventoryUI")]
    [SerializeField] private Transform contentsParent;
    /*
     * these "Icons for Note Type" private GameObjects are stand-ins for the actual note prefabs. 
     * if we swap these later, other updates must also occur: 
     *      1. prefab reference in Inspector needs updating
     *      2. below, SpawnIcon() switch-mapping (ex. ItemType.Green => greenIcon); 
     *      3. in ItemType.cs enum if new note types are added or changed (ex. Green)
     */
    [Header("Icons for Note Type")]
    [SerializeField] private GameObject greenIcon;
    [SerializeField] private GameObject blueIcon;
    [SerializeField] private GameObject yellowIcon;
    [SerializeField] private GameObject pinkIcon;

    private void OnTriggerEnter2D(Collider2D objToPickup)
    {
        PickupItem pickup = objToPickup.GetComponent<PickupItem>();

        if (pickup == null)
        {
            Debug.Log($"Object to pickup was empty! Collider: {objToPickup.name}");
            return;
        }

        inventory.AddItem(pickup.itemType); //add to inventory Data
    
        // temporary to prevent duplicate icons
        if (inventory.GetCount(pickup.itemType) == 1)
        {
            SpawnIcon(pickup.itemType); //add to inventory UI
        }
        Destroy(objToPickup.gameObject);
    }

    private void SpawnIcon(ItemType type)
    {
        GameObject prefab = type switch
        {
            ItemType.Green => greenIcon,
            ItemType.Blue => blueIcon,
            ItemType.Yellow => yellowIcon,
            ItemType.Pink => pinkIcon,
            _ => null
        };

        if (prefab == null)
        {
            Debug.Log($"No prefab assigned for ItemType {type}");
            return;
        }
        Instantiate(prefab, contentsParent).SetActive(true);
    }
}
