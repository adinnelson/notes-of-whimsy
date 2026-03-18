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
        bool actionPerfomed = false;
        //check for gold pickup
        GoldCoin gold = objToPickup.GetComponent<GoldCoin>();
        if (gold != null)
        {
            GoldManager.Instance.AddMoreGold(gold.Amount);
            Destroy(gold.gameObject);
            return;
        }

        //check for boostables pickup
        BoostableItem boost = objToPickup.GetComponent<BoostableItem>();
        if (boost != null)
        {
            var stats = GetComponent<PlayerStats>();
            var health = GetComponent<Health>();

            switch (boost.Type)
            {
                case BoostType.HealthPotion:
                    if (health != null)
                    {
                        health.CurrentHealth += (int)boost.Amount;
                    }
                    break;
                case BoostType.MaxHealth:
                    stats.AddHealthBonus((int)boost.Amount);
                    break;
                case BoostType.Speed:
                    stats.AddMovementSpeed(boost.Amount);
                    break;
                case BoostType.Damage:
                    stats.AddDamageBonus((int)boost.Amount);
                    break;
            }
            actionPerfomed = true;

        }


        PickupItem pickup = objToPickup.GetComponent<PickupItem>();
        if (pickup == null)
        {
            //Debug.LogWarning($"Not A Valid PickUp Item! Collider: {objToPickup.name}");
            if (actionPerfomed)
            {
                Destroy(objToPickup.gameObject);
            }
            return;
        }

        if (pickup.beatId == 0)
        {
            return;
        }

        int slotId = playerActiveSpellsHandler.UnlockSlot();

        beatHandler.BeatUnlocked(slotId);

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
