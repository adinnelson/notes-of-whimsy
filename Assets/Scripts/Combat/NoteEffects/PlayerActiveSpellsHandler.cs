using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerActiveSpellsHandler : MonoBehaviour
{
    public const int SPELL_SLOT_NUM = 8;

    PlayerAttack playerAttack;

    [SerializeField] private List<SpellDataSO> spellData = new List<SpellDataSO>();

    // Yellow Note needed prefabs
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private TempLightingEffectLogic lightningEffect;

    [SerializeField] private LaserBeam laserPrefab;

    private struct SlotSpell
    {
        public bool unlocked;
        public NoteEffectHandler noteEffectHandler;
        public Color colour;
    }

    // spells equipt
    Dictionary<int, SlotSpell> slotSpells = new Dictionary<int, SlotSpell>();

    // all spell data
    Dictionary<int, SpellDataSO> idToSpellData = new Dictionary<int, SpellDataSO>();

    private void Awake() {
        
        // sets up slot spell with slot id
        for(int i = 1;i <= 8;i++)
        {
            SlotSpell slotSpell = new SlotSpell();
            slotSpell.unlocked = false;
            slotSpell.noteEffectHandler = null;

            slotSpells.Add(i, slotSpell);
        }

        // sets up spell data with spell id
        for (int i = 0;i < spellData.Count;i++)
        {
            idToSpellData.Add(i + 1, spellData[i]);    
        }

        playerAttack = GetComponent<PlayerAttack>();
    }

    // equipes spell
    public void EquipSpell(int slotId, int spellId)
    {
        SlotSpell slotSpell = slotSpells[slotId];
        
        if (!slotSpell.unlocked)
        {
            return;
        }
        
        if (slotSpell.noteEffectHandler != null)
        {
            Instantiate(slotSpell.noteEffectHandler.SpellData.NoteGameObj, transform.position, transform.rotation);
            ClearSlot(slotId);
            
        }

        switch(spellId)
        {
            case 1:
                RedNoteEffectHandler redNoteEffectHandler = this.gameObject.AddComponent<RedNoteEffectHandler>(); 
                slotSpell.colour = Color.red;
                slotSpell.noteEffectHandler = redNoteEffectHandler;

                break;
            case 2:
                YellowNoteEffectHandler yellowNoteEffectHandler = this.gameObject.AddComponent<YellowNoteEffectHandler>();
                yellowNoteEffectHandler.CustomYellowInit(lightningEffect, enemyMask);
                slotSpell.colour = Color.yellow;
                slotSpell.noteEffectHandler = yellowNoteEffectHandler;

                break;
            case 3:
                PurpleNoteEffectHandler purpleNoteEffectHandler = this.gameObject.AddComponent<PurpleNoteEffectHandler>();
                purpleNoteEffectHandler.SetLaser(laserPrefab);
                slotSpell.colour = Color.purple;
                slotSpell.noteEffectHandler = purpleNoteEffectHandler;
                
                break;
            case 4:

                // DOESN'T WORK AWAITING REDESIGN IMPLEMENTATION
                BlueNoteEffectHandler blueNoteEffectHandler = this.gameObject.AddComponent<BlueNoteEffectHandler>();
                slotSpell.colour = Color.blue;
                slotSpell.noteEffectHandler = blueNoteEffectHandler;
                
                break;
        }

        slotSpell.noteEffectHandler.Init(idToSpellData[spellId], playerAttack);
        slotSpells[slotId] = slotSpell;
    }

    // Clear slot at slot Id
    public void ClearSlot(int slotId)
    {
        Destroy(slotSpells[slotId].noteEffectHandler);

        SlotSpell slotSpell = slotSpells[slotId];
        slotSpell.noteEffectHandler = null;

        slotSpells[slotId] = slotSpell;
    }

    // unlock slot
    public void UnlockSlot(int slotId)
    {
        SlotSpell slotSpell = slotSpells[slotId];
        slotSpell.unlocked = true;
        slotSpells[slotId] = slotSpell;
    }

    // returns if slot is unlocked
    public bool GetSpellUnlockedFromSlotId(int slotId)
    {
        if(!slotSpells.ContainsKey(slotId)) return false;

        return slotSpells[slotId].unlocked;
    }

    // returns note effect handler from slot
    public NoteEffectHandler GetSpellEffectHandlerFromSlotId(int slotId)
    {
        if(!slotSpells.ContainsKey(slotId)) return null;

        return slotSpells[slotId].noteEffectHandler;
    }

    // gets spell id from slot id
    public int GetSpellIdFromSlotId(int slotId)
    {
        if (slotSpells[slotId].noteEffectHandler == null)
        {
            return -1;
        }

        return slotSpells[slotId].noteEffectHandler.SpellData.spellId;
    }
}
