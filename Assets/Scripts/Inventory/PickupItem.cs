using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class PickupItem : MonoBehaviour
{
    public SpellDataSO spell;
    public int beatId;

    [SerializeField] private float pickupRange = 2.0f;
    public float PickupRange => pickupRange;

    [Header("Item Pickup Feedback")]
    [SerializeField] private GameObject glowChild;
    [SerializeField] private TextMeshProUGUI labelChild;
    [SerializeField] private GameObject labelContainer;

    private static List<PickupItem> activePickups = new List<PickupItem>();
    private static PickupItem currentActive;

    private string thisLabel = string.Empty;

    private Transform player;
    private PlayerInventory playerInventory;
    private SpriteRenderer glowRenderer;
    private bool inRange = false;

    public void SetLabelReferences(TextMeshProUGUI label, GameObject container)
    {
        labelChild = label;
        labelContainer = container;
    }

    private void Start()
    {
        activePickups.RemoveAll(p => p == null);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerInventory = playerObj.GetComponent<PlayerInventory>();
        }

        if (glowChild != null)
        {
            glowRenderer = glowChild.GetComponent<SpriteRenderer>();
            SpriteRenderer parentRenderer = GetComponent<SpriteRenderer>();

            if (glowRenderer != null && parentRenderer != null)
            {
                glowRenderer.sprite = parentRenderer.sprite;
            }

            glowChild.SetActive(false);
        }
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        bool nowInRange = distance <= pickupRange;

        if (nowInRange != inRange)
        {
            inRange = nowInRange;

            //find refs if needed
            if (inRange && (labelChild == null || labelContainer == null))
            {
                var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var go in allObjects)
                {
                    if (go.name == "FeedbackTextContainer")
                    {
                        labelContainer = go;
                        labelChild = go.GetComponentInChildren<TextMeshProUGUI>(true);
                        break;
                    }
                }
            }

            if (inRange)
            {
                thisLabel = BuildLabel();

                if (!string.IsNullOrEmpty(thisLabel) && !activePickups.Contains(this))
                {
                    activePickups.Add(this);
                }
            }
            else
            {
                activePickups.Remove(this);

                if (currentActive == this)
                {
                    SetVisual(false);
                    currentActive = null;
                }
            }

            UpdateActivePickup();
        }

        if (inRange && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPickup();
        }
    }

    private static void UpdateActivePickup()
    {
        //cleanup nulls
        activePickups.RemoveAll(p => p == null);

        if (activePickups.Count == 0)
        {
            if (currentActive != null)
            {
                currentActive.SetVisual(false);
                currentActive = null;
            }
            return;
        }

        Transform player = null;
        foreach (var item in activePickups)
        {
            if (item != null && item.player != null)
            {
                player = item.player;
                break;
            }
        }

        if (player == null) return;

        PickupItem closest = null;
        float minDist = Mathf.Infinity;

        foreach (var item in activePickups)
        {
            if (item == null) continue;

            float dist = Vector2.Distance(player.position, item.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = item;
            }
        }

        if (closest != currentActive)
        {
            if (currentActive != null)
                currentActive.SetVisual(false);

            currentActive = closest;

            if (currentActive != null)
                currentActive.SetVisual(true);
        }
    }

    public void SetVisual(bool state)
    {
        if (glowChild != null)
            glowChild.SetActive(state);

        if (labelChild != null && labelContainer != null)
        {
            if (state)
            {
                labelChild.text = thisLabel;
                labelContainer.SetActive(true);
            }
            else
            {
                //hide if this item was previously the active one
                if (currentActive == this || currentActive == null)
                {
                    labelContainer.SetActive(false);
                    labelChild.text = "";
                }
            }
        }
    }

    private void TryPickup()
    {
        if (playerInventory == null)
        {
            return;
        }

        if (spell != null)
        {
            SpellPickupUI.Instance.Show(this);
            return;
        }

        if (beatId != 0)
        {
            playerInventory.PickupInventorySlot(gameObject);
            return;
        }

        BoostableItem boost = GetComponent<BoostableItem>();
        if (boost != null)
        {
            playerInventory.PickupBoost(boost);
            return;
        }
    }

    private string BuildLabel()
    {
        if (spell != null)
        {
            return GetSpellLabel();
        }

        if (beatId != 0)
        {
            return "Unlock a Beat Slot!";
        }

        BoostableItem boost = GetComponent<BoostableItem>();
        if (boost != null)
        {
            switch (boost.Type)
            {
                case BoostType.HealthPotion:
                    return $"Press <e> to apply +{(int)boost.Amount} Health Potion";
                case BoostType.MaxHealth:
                    return $"Press <e> to apply +{(int)boost.Amount} to your Max Health";
                case BoostType.Speed:
                    return $"Press <e> to apply +{boost.Amount:F1} Speed";
                case BoostType.Damage:
                    return $"Press <e> to apply +{(int)boost.Amount} Damage";
            }
        }

        return string.Empty;
    }

    //used to set the spell label
    private string GetSpellLabel()
    {
        switch (spell.SpellType)
        {
            case ItemType.Purple:
                return "Press <e> to pick up Laserbeam Spell";
            case ItemType.Blue:
                return "Press <e> to pick up Whirlpool Spell";
            case ItemType.Yellow:
                return "Press <e> to pick up Stun Spell";
            case ItemType.Pink:
                return "Press <e> to pick up Fireball Spell";
        }
        return "Press <e> to pick up Spell";
    }

    private void OnDestroy()
    {
        activePickups.Remove(this);

        if (currentActive == this)
        {
            currentActive = null;
        }

        UpdateActivePickup();
    }
}
