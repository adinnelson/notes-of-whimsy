using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    //now allows multiple same type spells 
    private List<SpellDataSO> activeSpells = new List<SpellDataSO>(); 
    private List<GameObject> activeIcon = new List<GameObject>();

    [SerializeField] private BeatHandler beatHandler;

    [Header("InventoryUI")]
    [SerializeField] private Transform contentsParent;
    [SerializeField] private SpellEditBar spellEditBar;
    [SerializeField] private PlayerActiveSpellsHandler playerActiveSpellsHandler;

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.SpellSlots.performed += OnOpenInventory;
    }

    private void OnDisable()
    {
        inputActions.Player.SpellSlots.performed -= OnOpenInventory;
        inputActions.Player.Disable();
    }

    public void TryToPickup(SpellDataSO newSpell, GameObject worldObject)
    {
        if (newSpell == null)
        {
            Debug.LogWarning("tried to add a null spell");
            return;
        }

        activeSpells.Add(newSpell);

        spellEditBar.Open(newSpell, worldObject);
    }

    private void OnTriggerEnter2D(Collider2D objToPickup)
    {
        PickupItem pickup = objToPickup.GetComponent<PickupItem>();

        if (pickup == null)
        {
            //Debug.LogWarning($"Not A Valid PickUp Item! Collider: {objToPickup.name}");
            return;
        }

        if (pickup.tickId == 0)
        {
            return;
        }

        beatHandler.TickUnlocked(pickup.tickId);
        
        playerActiveSpellsHandler.UnlockSlot(pickup.tickId);

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
        activeIcon.Add(iconInstance);
    }

    private void OnOpenInventory(InputAction.CallbackContext context)
    {
        spellEditBar.Open();
    }
}
