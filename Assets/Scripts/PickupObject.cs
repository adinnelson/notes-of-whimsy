using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class pickupObject : MonoBehaviour
{
    [Header("Inventory UI")]
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

        SpawnIcon(pickup.itemType);
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
