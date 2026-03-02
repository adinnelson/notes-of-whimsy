using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class SpellEditBar : MonoBehaviour
{
    List<SpellBox> spellBoxes = new List<SpellBox>();
    PlayerActiveSpellsHandler playerActiveSpellsHandler;
    PlayerInventory playerInventory;
    private InputSystem_Actions inputActions;



    enum Setting
    {
        Swap,
        Equip
    }

    Setting setting = Setting.Equip;

    SpellDataSO spellToBePlaced = null;
    GameObject associatedSpellPickup = null;

    int? initialSlotId = null;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Escape.performed += OnMinimizeInventory;
    }

    private void OnDisable()
    {
        inputActions.Player.Escape.performed -= OnMinimizeInventory;
        inputActions.Player.Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.SetActive(false);

        playerActiveSpellsHandler = FindObjectOfType<PlayerActiveSpellsHandler>();
        playerInventory = FindObjectOfType<PlayerInventory>();

        // save spell boxes references
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform childTransform = transform.GetChild(i);
            GameObject childGameObject = childTransform.gameObject;

            SpellBox spellBox = childGameObject.GetComponent<SpellBox>();

            if(spellBox != null)
            {
                spellBox.Init(i + 1);
                spellBoxes.Add(spellBox);
            }
        }

    }

    // Opens spell bar
    // if a spell is passed in setting is equip
    // if not passed in in swap state
    public void Open(SpellDataSO newSpell = null, GameObject pickupable = null)
    {
        gameObject.SetActive(true);

        if(newSpell != null)
        {
            setting = Setting.Equip;
            spellToBePlaced = newSpell;
            associatedSpellPickup = pickupable;
        }
        else
        {
            setting = Setting.Swap;
        }

        UpdateIcons();
    }

    // close
    public void Close()
    {
        gameObject.SetActive(false);
        spellToBePlaced = null;
        associatedSpellPickup = null;
        initialSlotId = null;
    }

    public void UpdateIcons()
    {
        for(int i = 1;i <= PlayerActiveSpellsHandler.SPELL_SLOT_NUM;i++)
        {
            if(!playerActiveSpellsHandler.GetSpellUnlockedFromSlotId(i)) continue;
            if(playerActiveSpellsHandler.GetSpellEffectHandlerFromSlotId(i) == null) continue;

            spellBoxes[i - 1].SetSpellIcon(playerActiveSpellsHandler.GetSpellEffectHandlerFromSlotId(i).SpellIcon);
        }
    }

    // places spell
    // Or saves swap ids
    public void SpellBoxClicked(int slotId)
    {
        if (!playerActiveSpellsHandler.GetSpellUnlockedFromSlotId(slotId)) return;

        if(setting == Setting.Equip)
        {
            playerActiveSpellsHandler.EquipSpell(slotId, spellToBePlaced.spellId);

            Destroy(associatedSpellPickup);
            associatedSpellPickup = null;

            Close();
            return;
        }

        if(initialSlotId == null)
        {
            initialSlotId = slotId;
            return;
        }

        Swap((int)initialSlotId, slotId);
    
    }

    // swap 2 spells
    private void Swap(int slot1, int slot2)
    {
        Debug.Log("swapping");

        int spell1 = playerActiveSpellsHandler.GetSpellIdFromSlotId(slot1);
        int spell2 = playerActiveSpellsHandler.GetSpellIdFromSlotId(slot2);

        playerActiveSpellsHandler.ClearSlot(slot1);
        playerActiveSpellsHandler.ClearSlot(slot2);

        playerActiveSpellsHandler.EquipSpell(slot1, spell2);
        playerActiveSpellsHandler.EquipSpell(slot2, spell1);

        initialSlotId = null;

        UpdateIcons();
    }

    private void OnMinimizeInventory(InputAction.CallbackContext context)
    {
        Close();
    }
}
