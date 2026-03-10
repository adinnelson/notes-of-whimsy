using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(RoomInfo))]
public class ShopManager : MonoBehaviour
{

    // rewards list
    [SerializeField] 
    private GameObject[] rewardPrefabs;
    private RoomInfo roomInfo;
    private List<GameObject> rewardLocations = new();

    private RewardManager rewardManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        roomInfo = GetComponent<RoomInfo>();
        rewardLocations = roomInfo.GetItemSpawnLocations();
        rewardManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<RewardManager>();
        GenerateShopRewards();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void GenerateShopRewards()
    {
        // TODO: fix
        foreach (GameObject location in rewardLocations)
        {
            RewardType rewardType = rewardManager.GetRandomReward();
            GameObject rewardPrefab = rewardManager.GetRewardPrefab(rewardType);
            Instantiate(rewardPrefab, location.transform.position, Quaternion.identity, location.transform);
        }

    }
}
