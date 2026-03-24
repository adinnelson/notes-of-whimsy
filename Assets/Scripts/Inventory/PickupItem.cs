using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PickupItem : MonoBehaviour
{
    public SpellDataSO spell;
    public int beatId;

    [SerializeField] private float pickupRange = 2.0f;
    public float PickupRange => pickupRange;

    [Header("Item Pickup Feedback")]
    [SerializeField] private GameObject glowChild;
    [SerializeField] private TextMeshProUGUI labelChild;

    private Transform player;
    private PlayerInventory playerInventory;
    private SpriteRenderer glowRenderer;
    private bool inRange = false;

    private void Start()
    {
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

        //also find & build & assign label text for additional boostables feedback
        if (labelChild == null)
        {
            GameObject labelObj = GameObject.Find("FeedbackText");
            if (labelObj != null)
            {
                labelChild = labelObj.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                Debug.LogWarning("PickupItem: Could not find FeedbackText (TMP) in scene");
            }
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

            if (labelChild != null)
            {
                if (inRange)
                {
                    string label = BuildLabel();
                    labelChild.text = label;
                    labelChild.gameObject.SetActive(!string.IsNullOrEmpty(label));
                }
                else
                {
                    labelChild.gameObject.SetActive(false);
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
        Debug.Log($"BuildLabel - spell: {spell}, beatId: {beatId}, boost: {GetComponent<BoostableItem>()}");
        
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
}
