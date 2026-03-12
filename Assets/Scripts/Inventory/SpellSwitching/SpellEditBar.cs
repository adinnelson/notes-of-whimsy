using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class SpellEditBar : MonoBehaviour
{
    private List<SpellBox> spellBoxes = new List<SpellBox>();
    private PlayerActiveSpellsHandler playerActiveSpellsHandler;
    private PlayerInventory playerInventory;
    private InputSystem_Actions inputActions;



    enum Setting
    {
        Swap,
        Equip
    }

    private Setting setting = Setting.Equip;

    private SpellDataSO spellToBePlaced = null;
    private GameObject associatedSpellPickup = null;

    private int? initialSlotId = null;

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
        UpdateSpellBox();
    }

    // close
    public void Close()
    {
        gameObject.SetActive(false);
        spellToBePlaced = null;
        associatedSpellPickup = null;
        initialSlotId = null;
    }

    // reloads icons
    public void UpdateIcons()
    {
        for(int i = 1;i <= PlayerActiveSpellsHandler.SPELL_SLOT_NUM;i++)
        {
            int childCount = spellBoxes[i - 1].transform.childCount;
            if(childCount > 0) Destroy(spellBoxes[i - 1].transform.GetChild(0).gameObject);
            if(!playerActiveSpellsHandler.GetSpellUnlockedFromSlotId(i)) continue;
            if(playerActiveSpellsHandler.GetSpellEffectHandlerFromSlotId(i) == null) continue;
            GameObject spellIconPrefab = playerActiveSpellsHandler.GetSpellEffectHandlerFromSlotId(i).SpellIcon;
            print("there should be 4 of these");
            spellBoxes[i - 1].SetSpellIcon(playerActiveSpellsHandler.GetSpellEffectHandlerFromSlotId(i).SpellIcon);
        }
    }

    public void UpdateSpellBox()
    {
        for(int i = 1;i <= PlayerActiveSpellsHandler.SPELL_SLOT_NUM;i++)
        {
            if(playerActiveSpellsHandler.GetSpellUnlockedFromSlotId(i))
            {
                spellBoxes[i-1].SetColour(Color.darkGray);   
            } 
            else
            {
                spellBoxes[i-1].SetColour(Color.grey);
            }
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
            spellBoxes[slotId - 1].SetColour(Color.green);
            return;
        }

        Swap((int)initialSlotId, slotId);
    
    }

    // swap 2 spells
    private void Swap(int slot1, int slot2)
    {
        if(slot1 == slot2) return;

        int spell1 = playerActiveSpellsHandler.GetSpellIdFromSlotId(slot1);
        int spell2 = playerActiveSpellsHandler.GetSpellIdFromSlotId(slot2);
        // print(spell1 + " " + spell2);
        // if(spell1 == -1 || spell2 == -1) return;
        playerActiveSpellsHandler.ClearSlot(slot1);
        playerActiveSpellsHandler.ClearSlot(slot2);

        if(spell1 != -1)
        {
            playerActiveSpellsHandler.EquipSpell(slot2, spell1);
        }
        if(spell2 != -1)
        {
            playerActiveSpellsHandler.EquipSpell(slot1, spell2);
        }
        // playerActiveSpellsHandler.EquipSpell(slot1, spell2);
        // playerActiveSpellsHandler.EquipSpell(slot2, spell1);

        // spellBoxes[(int)initialSlotId - 1].SetColour(Color.grey);
        initialSlotId = null;
        UpdateSpellBox();
        UpdateIcons();
    }

    private void OnMinimizeInventory(InputAction.CallbackContext context)
    {
        if(gameObject.activeSelf)
        Close();
    }
}
