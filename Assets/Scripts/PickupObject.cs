using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PickupObject : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField] private InventoryItems inventory; //InventoryItems.cs should be assigned, either to player or an Inventory Manager obj, and then that gets used as the ref for this field
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
        var pickup = objToPickup.GetComponent<PickupItem>();
        if (pickup == null)
        {
            return;
        }
        inventory.AddItem(pickup.itemType); //add the note to the data
        /*
         * TEMPORARY IF STATEMENT: updates for future sprints when behaviour of the notes is better known. 
         * this is to prevent duplicate icons from spawning when multiples of the same note are picked up.
         * UI icon creation and updates should be handled by the inventory system and NOT by this PickupObject script.
         * 
         * Should duplicates show in inventory?
         * Should icons have counters attached, when there are more than 1 of a type?
         */
        if (inventory.GetCount(pickup.itemType) == 1)
        {
            SpawnIcon(pickup.itemType); //add the note to the UI
        }
        Destroy(objToPickup.gameObject);
    }

    void SpawnIcon(ItemType type)
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
            return;
        }
        GameObject newIconFromTemplate = Instantiate(prefab, contentsParent);
        newIconFromTemplate.SetActive(true);
    }
}
