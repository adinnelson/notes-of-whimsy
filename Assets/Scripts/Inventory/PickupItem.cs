using UnityEngine;
using UnityEngine.InputSystem;

public class PickupItem : MonoBehaviour
{
    public SpellDataSO spell;
    public int beatId;

    [SerializeField] private float pickupRange = 2.0f;
    public float PickupRange => pickupRange;

    [SerializeField] private GameObject glowChild;

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

        //pickup beat tick -> must be checked before the other boostables
        if (beatId != 0)
        {
            playerInventory.PickupInventorySlot(gameObject);
        }

        //pickup a boostable
        BoostableItem boost = GetComponent<BoostableItem>();
        if (boost != null)
        {
            playerInventory.PickupBoost(boost);
            return;
        }
    }
}
