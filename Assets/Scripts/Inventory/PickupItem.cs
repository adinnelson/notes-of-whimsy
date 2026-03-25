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

    //to manage feedback display when in range of multiple boosts
    private static List<PickupItem> activePickups = new List<PickupItem>();
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

        //copy the parent sprite into the glow child at runtime
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

            if (glowChild != null)
            {
                glowChild.SetActive(inRange);
            }

            //when an object is in range, make sure the container & TMP are assigned in the inspector of the object
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

                if (labelContainer == null)
                    Debug.LogWarning("PickupItem: Could not find FeedbackTextContainer");
            }

            if (labelChild != null)
            {
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
                    thisLabel = string.Empty;
                }

                if (activePickups.Count > 0)
                {
                    labelChild.text = string.Join("\n", activePickups.ConvertAll(p => p.thisLabel));

                    if (labelContainer != null)
                    {
                        labelContainer.SetActive(true);
                    }
                }
                else
                {
                    if (labelContainer != null)
                    {
                        labelContainer.SetActive(false);
                    }
                }
            }
        }

        if (inRange && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPickup();
        }
    }

    private void TryPickup()
    {
        if (playerInventory == null)
        {
            return;
        }

        //pickup a spell
        if (spell != null)
        {
            SpellPickupUI.Instance.Show(this);
            return;
        }

        //pickup beat tick slot unlock -> must be checked before the other boostables
        if (beatId != 0)
        {
            playerInventory.PickupInventorySlot(gameObject);
            return;
        }

        //pickup a boostable
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
            return string.Empty;
        }

        //beat tick item
        if (beatId != 0)
        {
            return ("Unlock a Beat Slot!");
        }

        //boostable item: health potion, max health, damage, speed
        BoostableItem boost = GetComponent<BoostableItem>();
        if (boost != null)
        {
            switch (boost.Type)
            { 
                case BoostType.HealthPotion:
                    return $"+{(int)boost.Amount} Health Potion";
                case BoostType.MaxHealth:
                    return $"+{(int)boost.Amount} Max Health";
                case BoostType.Speed:
                    return $"+{boost.Amount:F1} Speed";
                case BoostType.Damage:
                    return $"+{(int)boost.Amount} Damage";
            }
        }
        return string.Empty;
    }

    //to keep text list up to date when an item is picked up
    private void OnDestroy()
    {
        activePickups.Remove(this);

        if (labelChild != null)
        {
            if (activePickups.Count > 0)
            {
                labelChild.text = string.Join("\n", activePickups.ConvertAll(p => p.thisLabel));
                if (labelContainer != null)
                {
                    labelContainer.SetActive(true);
                }
            }
            else
            {
                if (labelContainer != null)
                {
                    labelContainer.SetActive(false);
                }
            }
        }
    }
}
