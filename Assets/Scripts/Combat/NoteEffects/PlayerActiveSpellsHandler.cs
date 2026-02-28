using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerActiveSpellsHandler : MonoBehaviour
{
    private struct SlotSpell
    {
        public bool unlocked;
        public NoteEffectHandler noteEffectHandler;
        public Color colour;
    }

    Dictionary<int, SlotSpell> slotSpells = new Dictionary<int, SlotSpell>();

    private void Awake() {
        for(int i = 1;i <= 8;i++)
        {
            SlotSpell slotSpell = new SlotSpell();
            slotSpell.unlocked = false;
            slotSpell.noteEffectHandler = null;

            slotSpells.Add(i, slotSpell);
        }
    }

    public void EquipSpell(int slotId, int spellId)
    {
        SlotSpell slotSpell = slotSpells[slotId];
        
        if (!slotSpell.unlocked)
        {
            return;
        }
        
        if (slotSpell.noteEffectHandler != null)
        {
            // drop current note
            
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
                slotSpell.colour = Color.yellow;
                slotSpell.noteEffectHandler = yellowNoteEffectHandler;

                break;
            case 3:
                PurpleNoteEffectHandler purpleNoteEffectHandler = this.gameObject.AddComponent<PurpleNoteEffectHandler>();
                slotSpell.colour = Color.purple;
                slotSpell.noteEffectHandler = purpleNoteEffectHandler;
                
                break;
            case 4:
                BlueNoteEffectHandler blueNoteEffectHandler = this.gameObject.AddComponent<BlueNoteEffectHandler>();
                slotSpell.colour = Color.blue;
                slotSpell.noteEffectHandler = blueNoteEffectHandler;
                
                break;
        }

        slotSpells[slotId] = slotSpell;
    }

    public void UnlockSlot(int slotId)
    {
        SlotSpell slotSpell = slotSpells[slotId];
        slotSpell.unlocked = true;
    }

}
