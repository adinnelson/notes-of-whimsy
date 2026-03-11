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


        //if not assigned, find the display panel -> PickupPopup/Panel should be a child object of SpellPickupUIManager
        if (panel == null )
        {
            Transform panelTransform = transform.Find("PickupPopup/Panel");
            if (panelTransform != null )
            {
                panel = panelTransform.gameObject;
            }
            else
            {
                Debug.LogError("SpellPickupUI: Could not find 'PickupPop/Panel' as a child!");
            }
        }
        if (panel != null)
        {
            panel.SetActive(false);
        }
        
        playerInventory = FindObjectOfType<PlayerInventory>();
    }

    public void Show(PickupItem pickup)
    {
        currentPickup = pickup;

        if (panel == null)
        {
            Debug.LogWarning("Spell Pickup Popup called but panel is NULL");
            return;
        }

        //set the pop-up menu to appear by the object we are picking up
        Vector2 screenPos = Camera.main.WorldToScreenPoint(pickup.transform.position);
        panel.transform.position = screenPos;

        if (panel != null)
        {
            panel.SetActive(true);
        }
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
        if (panel != null)
        {
            panel.SetActive(false);
        }
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
        if (panel != null)
        {
            panel.SetActive(false);
        }
        currentPickup = null;
    }
}
