using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RewardManager : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> rewardPrefabs;
    [SerializeField]
    private List<RewardType> rewardPool = new();

    public readonly Dictionary<RewardType, int> rewardCosts = new()
    {
        { RewardType.MaxHealth, 5},
        { RewardType.Damage, 5},
        { RewardType.Speed, 5},
        { RewardType.HealthPotion, 2 },
        { RewardType.BeatTick, 10 }
    };

    // Higher number means more common
    public readonly Dictionary<RewardType, int> rewardRarities = new()
    {
        { RewardType.MaxHealth, 5 },
        { RewardType.Damage, 5 },
        { RewardType.Speed, 3 },
        { RewardType.HealthPotion, 10 },
        { RewardType.BeatTick, 1 }
    };

    private void Awake()
    {
        foreach(RewardType rewardType in System.Enum.GetValues(typeof(RewardType)))
        {
            if (!rewardCosts.ContainsKey(rewardType))
            {
                Debug.LogError($"Reward type {rewardType} does not have a cost defined.");
            }
            if (!rewardRarities.ContainsKey(rewardType))
            {
                Debug.LogError($"Reward type {rewardType} does not have a rarity defined.");
            }

            for (int i = 0; i < rewardRarities[rewardType]; i++)
            {
                rewardPool.Add(rewardType);
            }

        }
    }

    public List<GameObject> GetRewardPrefabs()
    {
        return rewardPrefabs;
    }

    public RewardType GetRandomReward()
    {
        if (rewardPool.Count == 0)
        {
            Debug.LogError("Reward pool is empty!");
            return RewardType.MaxHealth; // Default fallback
        }
        int index = Random.Range(0, rewardPool.Count);
        return rewardPool[index];
    }

    public GameObject GetRewardPrefab(RewardType rewardType)
    {
        GameObject prefab = rewardPrefabs.FirstOrDefault(p => p.GetComponent<BoostableItem>()?.RewardType == rewardType);
        if (prefab == null)
        {
            Debug.LogError($"No prefab found for reward type {rewardType}");
        }
        return prefab;
    }

    public RewardType SpawnReward(Transform location, bool isShopReward = false)
    {
        RewardType rewardType = GetRandomReward();
        GameObject rewardPrefab = GetRewardPrefab(rewardType);
        if (rewardPrefab != null && !isShopReward)
        {
            Instantiate(rewardPrefab, location.transform.position, Quaternion.identity);
        }

        return rewardType;
    }

    /// <summary>
    /// Overload of SpawnReward that takes in a specific prefab to spawn, used for shop rewards where we want to control the reward type
     /// and just need to spawn the prefab at the correct location
    /// </summary>
    /// <param name="rewardPrefab"></param>
    /// <param name="location"></param>
    public void SpawnReward(GameObject rewardPrefab, Transform location)
    {
        if (rewardPrefab != null)
        {
            Instantiate(rewardPrefab, location.transform.position, Quaternion.identity);
        }
    }

    public int GetGoldCost(RewardType rewardType)
    {
        if (rewardCosts.TryGetValue(rewardType, out int cost))
        {
            return cost;
        }
        else
        {
            Debug.LogError($"No cost defined for reward type {rewardType}");
            return int.MaxValue; // Default to very expensive if not defined
        }
    }

    public int GetGoldCost(GameObject rewardPrefab)
    {
        BoostableItem boost = rewardPrefab.GetComponent<BoostableItem>();
        if (boost != null)
        {
            return GetGoldCost(boost.RewardType);
        }
        else
        {
            Debug.LogError($"Prefab {rewardPrefab.name} does not have a BoostableItem component");
            return int.MaxValue;
        }
    }





}

public enum RewardType
{
    MaxHealth,
    Damage,
    Speed,
    HealthPotion,
    BeatTick
}
