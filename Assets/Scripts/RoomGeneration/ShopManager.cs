using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(RoomInfo))]
public class ShopManager : MonoBehaviour
{

    // rewards list
    [SerializeField] 
    private List<GameObject> rewardPrefabs;
    private List<GameObject> rewardLocations = new();

    private RoomInfo roomInfo;
    private ShopItem shopItem;

    private GameManager gameManager;
    private RewardManager rewardManager;
    private GoldManager goldManager;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        roomInfo = GetComponent<RoomInfo>();
        rewardLocations = roomInfo.GetItemSpawnLocations();

        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if(gameManager == null)
        {
            Debug.LogWarning("ShopManager: GameManager reference missing from scene");
        }
        rewardManager = gameManager.GetComponent<RewardManager>();
        if(rewardManager == null)
        {
            Debug.LogWarning("ShopManager: RewardManager reference missing from GameManager");
        }
        goldManager = gameManager.GetComponent<GoldManager>();
        if(goldManager == null)
        {
            Debug.LogWarning("ShopManager: GoldManager reference missing from GameManager");
        }

        rewardPrefabs = rewardManager.GetRewardPrefabs();
        
    }

    void Start()
    {
        GenerateShopRewards();
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    public bool BuyItem(GameObject item, Transform location)
    {
        int itemCost = rewardManager.GetGoldCost(item);
        print("Attempting to buy item for " + itemCost + " gold.");
        bool canAfford = GoldManager.Instance.MakePurchase(itemCost);
         if (!canAfford)
        {
            print("Not enough gold to buy this item.");
            return canAfford;
        }
        
        rewardManager.SpawnReward(item, location);

        return canAfford;
    }



    private void GenerateShopRewards()
    {
        // current code is perfect for general room logic
        // TODO: fix
        foreach (GameObject location in rewardLocations)
        {
            print(location.name);
            shopItem = location.GetComponent<ShopItem>();
            RewardType rewardType = rewardManager.SpawnReward(location.transform, true);
            GameObject rewardPrefab = rewardManager.GetRewardPrefab(rewardType);
            // Instantiate(rewardPrefab, location.transform.position, Quaternion.identity, location.transform);
            print(rewardPrefab.name);
            UpdateShopText(location, rewardType); 
            shopItem.SetUpShopItem(rewardPrefab);

        }
    }

    private void UpdateShopText(GameObject location, RewardType rewardType, string customText = null)
    {
        TMPro.TextMeshPro shopText = location.GetComponentInChildren<TMPro.TextMeshPro>(true);
        if (shopText == null)
        {
            Debug.LogWarning($"No TextMeshProUGUI found in children of {location.name}");
        }

        shopText.text = customText ?? $"{rewardManager.rewardCosts[rewardType]} Gold";
    }

    private void UpdateItemSprite(){

    }


}
