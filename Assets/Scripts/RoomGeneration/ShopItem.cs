using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
public class ShopItem : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer itemSpriteRenderer;

    [SerializeField]
    private SpriteRenderer interactionIndicator;

    [SerializeField]
    private GameObject rewardPrefab;

    [SerializeField]
    private GameObject player;

    [SerializeField]
    private float interactionDistance = 1.5f;

    [SerializeField]
    private ShopManager shopManager;

    [SerializeField]
    private Color canInteractColor = Color.yellow;

    [SerializeField]
    private Color cannotInteractColor = Color.grey;

    private Sprite itemSprite;
    private float myDistanceToPlayer = float.MaxValue;

    public static float DistanceToPlayer = float.MaxValue;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("ShopItem: Player reference missing from scene");
        }

        SetItemSprite(itemSpriteRenderer);
        shopManager = transform.parent.GetComponent<ShopManager>();
    }

    private void Update()
    {
        myDistanceToPlayer = GetDistanceToPlayer();
        DistanceToPlayer = Math.Min(myDistanceToPlayer, DistanceToPlayer);
        CanPurchase();
    }

    private void LateUpdate()
    {
        DistanceToPlayer = float.MaxValue;
    }

    private void HideShopItem()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Configures this shop item to represent the provided reward prefab.
    /// </summary>
    /// <param name="prefab">The reward prefab associated with this shop item.</param>
    public void SetUpShopItem(GameObject prefab)
    {
        SetItemSprite(prefab.GetComponent<SpriteRenderer>());
        rewardPrefab = prefab;
    }

    private float GetDistanceToPlayer()
    {
        if (player != null)
        {
            return (player.transform.position - transform.position).magnitude;
        }

        return float.MaxValue;
    }

    private void SetItemSprite(SpriteRenderer sourceSpriteRenderer)
    {
        itemSprite = sourceSpriteRenderer.sprite;
        this.itemSpriteRenderer.sprite = itemSprite;
        this.itemSpriteRenderer.color = sourceSpriteRenderer.color;
    }

    private void CanPurchase()
    {
        if (!PlayerWithinRange())
        {
            UpdateColour(cannotInteractColor);
            return;
        }
        UpdateColour(canInteractColor);
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        bool canAfford = shopManager.BuyItem(rewardPrefab, transform);
        if (canAfford) HideShopItem();
    }

    private void UpdateColour(Color colour)
    {
        interactionIndicator.color = colour;
    }

    private bool PlayerWithinRange()
    {
        return FloatEqual(myDistanceToPlayer, DistanceToPlayer) && myDistanceToPlayer <= interactionDistance;
    }

    private bool FloatEqual(float a, float b, float epsilon = 0.01f)
    {
        return Mathf.Abs(a - b) < epsilon;
    }
}
