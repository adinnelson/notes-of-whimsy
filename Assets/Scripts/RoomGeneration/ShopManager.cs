using System.Collections.Generic;
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
    private RewardManager rewardManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        roomInfo = GetComponent<RoomInfo>();
        rewardLocations = roomInfo.GetItemSpawnLocations();

        rewardManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<RewardManager>();
        if(rewardManager == null)
        {
            Debug.LogWarning("ShopManager: RewardManager reference missing from GameManager");
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


    private void GenerateShopRewards()
    {
        // current code is perfect for general room logic
        // TODO: fix
        foreach (GameObject location in rewardLocations)
        {
            RewardType rewardType = rewardManager.SpawnReward(location);
            GameObject rewardPrefab = rewardManager.GetRewardPrefab(rewardType);
            // Instantiate(rewardPrefab, location.transform.position, Quaternion.identity, location.transform);
            UpdateShopText(location, rewardType); 

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
