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



}

public enum RewardType
{
    MaxHealth,
    Damage,
    Speed,
    HealthPotion,
    BeatTick
}
