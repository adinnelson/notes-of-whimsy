using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
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

    private void Update()
    {
        bool spellEditBarNull = spellEditBar == null;
        
        if(contentsParent == null || spellEditBarNull)
        {

            if(spellEditBarNull)
            {
                spellEditBar = FindFirstObjectByType<SpellEditBar>(FindObjectsInactive.Include);

            }
            contentsParent = spellEditBar?.gameObject.transform;

        }

        if(beatHandler == null)
        {
            beatHandler = GameObject.Find("BeatBar")?.GetComponent<BeatHandler>();
        }
        
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

    //called by PickupItem on <e> for Boostable item
    public void PickupBoost(BoostableItem boost)
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
        Destroy(boost.gameObject);
    }

    //called by PickupItem on <e> for beat bar tick unlock when beatId != 0
    public void PickupInventorySlot(GameObject itemObject)
    {
        int slotId = playerActiveSpellsHandler.UnlockSlot();
        beatHandler.BeatUnlocked(slotId);
        Destroy(itemObject);
    }

    //gold pickups remains on collide
    private void OnTriggerEnter2D(Collider2D objToPickup)
    {
        GoldCoin gold = objToPickup.GetComponent<GoldCoin>();
        if (gold != null)
        {
            GoldManager.Instance.AddMoreGold(gold.Amount);
            Destroy(gold.gameObject);
        }
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
