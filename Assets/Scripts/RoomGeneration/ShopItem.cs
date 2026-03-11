using System;
using System.ComponentModel.Design;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
public class ShopItem : MonoBehaviour
{

    [SerializeField]
    private SpriteRenderer itemSpriteRenderer;
    private Sprite itemSprite = null;

    [SerializeField]
    private SpriteRenderer interactionIndicator;


    [SerializeField]
    private GameObject rewardPrefab;

    [SerializeField]
    private GameObject player;
    static public float distanceToPlayer = float.MaxValue;

    private float myDistanceToPlayer = float.MaxValue;

    [SerializeField] 
    private float interactionDistance = 1.5f;

    [SerializeField]
    private ShopManager shopManager;

    [SerializeField]
    private Color canInteractColor = Color.yellow;

    [SerializeField]
    private Color cannotInteractColor = Color.grey;

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


    void Update()
    {
        myDistanceToPlayer = GetDistanceToPlayer();
        distanceToPlayer = Math.Min(myDistanceToPlayer, distanceToPlayer);
        CanPurchase();
    }

    void LateUpdate()
    {
        distanceToPlayer = float.MaxValue;
        

    }

    public void HideShopItem()
    {
        gameObject.SetActive(false);
    }
    public void SetUpShopItem(GameObject prefab)
    {
        SetItemSprite(prefab.GetComponent<SpriteRenderer>());
        rewardPrefab = prefab;
    }

    private float GetDistanceToPlayer()
    {
        if (player != null)        {
            
            return (player.transform.position - transform.position).magnitude;
        }
        else
        {
            return float.MaxValue;
        }
    }

    private void SetItemSprite(SpriteRenderer itemSpriteRenderer)
    {
        itemSprite = itemSpriteRenderer.sprite;
        this.itemSpriteRenderer.sprite = itemSprite;
        this.itemSpriteRenderer.color = itemSpriteRenderer.color;
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
        return FloatEqual(myDistanceToPlayer, distanceToPlayer) && myDistanceToPlayer <= interactionDistance;
    }

    private bool FloatEqual(float a, float b, float epsilon = 0.01f)
    {
        return Mathf.Abs(a - b) < epsilon;
    }

}
