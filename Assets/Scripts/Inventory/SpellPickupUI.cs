using UnityEngine;

public class SpellPickupUI : MonoBehaviour
{
    public static SpellPickupUI Instance;

    [SerializeField] private GameObject panel;

    private PickupItem currentPickup;
    private PlayerInventory playerInventory;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
        playerInventory = FindObjectOfType<PlayerInventory>();
    }

    public void Show(PickupItem pickup)
    {
        currentPickup = pickup;

        //set the pop-up menu to appear by the object we are picking up
        Vector2 screenPos = Camera.main.WorldToScreenPoint(pickup.transform.position);
        panel.transform.position = screenPos;

        panel.SetActive(true);
    }

    public void Pickup()
    {
        if (currentPickup != null)
        {
            playerInventory.TryToPickup(currentPickup.spell, currentPickup.gameObject);
        }
        else
        {
            Debug.LogWarning("Pickup called but currentPickup is null!");
        }
        panel.SetActive(false);
        currentPickup = null;
    }

    public void Discard()
    {
        if (currentPickup != null)
        {
            Destroy(currentPickup.gameObject);
        }
        else
        {
            Debug.LogWarning("Discard called but currentPickup is null!");
        }
        panel.SetActive(false);
        currentPickup = null;
    }
}
