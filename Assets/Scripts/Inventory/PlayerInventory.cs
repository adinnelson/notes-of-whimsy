using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    //now allows multiple same type spells 
    private List<SpellDataSO> activeSpells = new List<SpellDataSO>(); 
    private List<GameObject> activeIcon = new List<GameObject>();

    [SerializeField] private BeatHandler beatHandler;

    [Header("InventoryUI")]
    [SerializeField] private Transform contentsParent;

    public void TryToPickup(SpellDataSO newSpell, GameObject worldObject)
    {


        if (newSpell == null)
        {
            Debug.LogWarning("tried to add a null spell");
            return;
        }

        activeSpells.Add(newSpell);
        SpawnIcon(newSpell);
        Destroy(worldObject);
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
}
