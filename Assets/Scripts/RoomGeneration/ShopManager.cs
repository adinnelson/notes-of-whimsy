using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RoomInfo))]
public class ShopManager : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> rewardPrefabs;

    private List<GameObject> rewardLocations = new();

    private RoomInfo roomInfo;
    private ShopItem shopItem;

    private GameManager gameManager;
    private RewardManager rewardManager;
    private GoldManager goldManager;

    private void Awake()
    {
        roomInfo = GetComponent<RoomInfo>();
        rewardLocations = roomInfo.GetItemSpawnLocations();

        GameObject gameManagerObject = GameObject.FindGameObjectWithTag("GameManager");
        if (gameManagerObject == null)
        {
            Debug.LogWarning("ShopManager: GameManager reference missing from scene");
            return;
        }

        gameManager = gameManagerObject.GetComponent<GameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning("ShopManager: GameManager component missing from tagged GameObject");
            return;
        }

        rewardManager = gameManager.GetComponent<RewardManager>();
        if (rewardManager == null)
        {
            Debug.LogWarning("ShopManager: RewardManager reference missing from GameManager");
        }

        goldManager = gameManager.GetComponent<GoldManager>();
        if (goldManager == null)
        {
            Debug.LogWarning("ShopManager: GoldManager reference missing from GameManager");
        }

        if (rewardManager != null)
        {
            rewardPrefabs = rewardManager.GetRewardPrefabs();
        }
    }

    /// <summary>
    /// Attempts to purchase a shop item and spawn it at the provided location.
    /// </summary>
    /// <param name="item">The reward prefab being purchased.</param>
    /// <param name="location">The transform where the purchased reward will spawn.</param>
    /// <returns>True if the purchase succeeds; otherwise, false.</returns>
    public bool BuyItem(GameObject item, Transform location)
    {
        if (rewardManager == null)
        {
            Debug.LogWarning("ShopManager: Cannot buy item because RewardManager is missing");
            return false;
        }

        if (GoldManager.Instance == null)
        {
            Debug.LogWarning("ShopManager: Cannot buy item because GoldManager instance is missing");
            return false;
        }

        int itemCost = rewardManager.GetGoldCost(item);
        Debug.Log("Attempting to buy item for " + itemCost + " gold.");
        bool canAfford = GoldManager.Instance.MakePurchase(itemCost);
        if (!canAfford)
        {
            Debug.Log("Not enough gold to buy this item.");
            return canAfford;
        }

        rewardManager.SpawnReward(item, location);

        return canAfford;
    }

    /// <summary>
    /// Populates each configured shop location with a randomly generated reward.
    /// </summary>
    public void GenerateShopRewards()
    {
        if (rewardManager == null)
        {
            Debug.LogWarning("ShopManager: Cannot generate shop rewards because RewardManager is missing");
            return;
        }

        foreach (GameObject location in rewardLocations)
        {
            // Debug.Log(location.name);
            shopItem = location.GetComponent<ShopItem>();
            RewardType rewardType = rewardManager.SpawnReward(location.transform, true);
            GameObject rewardPrefab = rewardManager.GetRewardPrefab(rewardType);
            // Debug.Log(rewardPrefab.name);
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
            return;
        }

        shopText.text = customText ?? $"{rewardManager.GetGoldCost(rewardType)} Gold";
    }
}
