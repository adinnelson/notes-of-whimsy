using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class SpellEditBar : MonoBehaviour
{
    public const string SPELL_BAR_ATTACK_LOCK_KEY = "spellbar";

    private List<SpellBox> spellBoxes = new List<SpellBox>();
    private PlayerActiveSpellsHandler playerActiveSpellsHandler;
    private PlayerInventory playerInventory;
    private PlayerAttack playerAttack;
    private BeatHandler beatHandler;
    private InputSystem_Actions inputActions;

    enum Setting
    {
        Swap,
        Equip, 
        Cinematic
    }

    private Setting setting = Setting.Equip;
    public bool InCinematic
    {
        get { return setting == Setting.Cinematic; }
    }

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
        playerAttack = FindObjectOfType<PlayerAttack>();

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
    public void Open(SpellDataSO newSpell = null, GameObject pickupable = null, bool isCinematic = false)
    {
        gameObject.SetActive(true);

        if(beatHandler == null)
        {
            beatHandler = FindObjectOfType<BeatHandler>();
        }

        beatHandler.gameObject.SetActive(false);

        if(!isCinematic)
        {
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
        }
        else
        {
            setting = Setting.Cinematic;
            Time.timeScale = 0f;
            StartCoroutine(BossSpellBarCinematic());
        }

        UpdateIcons();
        UpdateSpellBox();

        playerAttack.AddAttackLock(SPELL_BAR_ATTACK_LOCK_KEY);
    }

    // close
    public void Close()
    {
        gameObject.SetActive(false);
        beatHandler.gameObject.SetActive(true);
        spellToBePlaced = null;
        associatedSpellPickup = null;
        initialSlotId = null;

        playerAttack.RemoveAttackLock(SPELL_BAR_ATTACK_LOCK_KEY);
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
        if(InCinematic) return;

        if(gameObject.activeSelf)
        Close();
    }

    private IEnumerator BossSpellBarCinematic()
    {
        // get all spells equipped
        List<int> spellsEquipped = playerActiveSpellsHandler.GetSpells();

        // get number of unlocked slots
        int slotsUnlocked = playerActiveSpellsHandler.GetNumberOfUnlockedSlots();

        // looping randomize
        for(int i = 0;i < 5;i++)
        {
            playerActiveSpellsHandler.RemoveAllSpells();

            playerActiveSpellsHandler.RandomizeSpells(playerActiveSpellsHandler.UnlockRandomSlots(slotsUnlocked), new List<int>(spellsEquipped));

            UpdateIcons();

            UpdateSpellBox();

            // play sound for click like slots

            yield return new WaitForSecondsRealtime(0.5f);
        }

        yield return new WaitForSecondsRealtime(1.5f);

        Time.timeScale = 1f;
        Close();
    }
}
