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
        { RewardType.MaxHealth, 5 },
        { RewardType.Damage, 5 },
        { RewardType.Speed, 5 },
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
        foreach (RewardType rewardType in System.Enum.GetValues(typeof(RewardType)))
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

    /// <summary>
    /// Gets the list of reward prefabs available for spawning.
    /// </summary>
    public List<GameObject> GetRewardPrefabs()
    {
        return rewardPrefabs;
    }

    /// <summary>
    /// Selects a reward type from the weighted reward pool.
    /// </summary>
    /// <returns>A randomly selected reward type.</returns>
    public RewardType GetRandomReward()
    {
        if (rewardPool.Count == 0)
        {
            Debug.LogError("Reward pool is empty!");
            return RewardType.MaxHealth;
        }

        int index = Random.Range(0, rewardPool.Count);
        return rewardPool[index];
    }

    /// <summary>
    /// Gets the reward prefab associated with the specified reward type.
    /// </summary>
    /// <param name="rewardType">The reward type to resolve.</param>
    /// <returns>The matching reward prefab, or null if none exists.</returns>
    public GameObject GetRewardPrefab(RewardType rewardType)
    {
        GameObject prefab = rewardPrefabs.FirstOrDefault(p => p.GetComponent<BoostableItem>()?.RewardType == rewardType);
        if (prefab == null)
        {
            Debug.LogError($"No prefab found for reward type {rewardType}");
        }

        return prefab;
    }

    /// <summary>
    /// Selects and optionally spawns a random reward at the given location.
    /// </summary>
    /// <param name="location">The transform used as the spawn location.</param>
    /// <param name="isShopReward">When true, only selects the reward type and does not spawn the prefab.</param>
    /// <returns>The selected reward type.</returns>
    public RewardType SpawnReward(Transform location, bool isShopReward = false)
    {
        RewardType rewardType = GetRandomReward();
        GameObject selectedRewardPrefab = GetRewardPrefab(rewardType);
        if (selectedRewardPrefab != null && !isShopReward)
        {
            Instantiate(selectedRewardPrefab, location.transform.position, Quaternion.identity);
        }

        return rewardType;
    }

    /// <summary>
    /// Spawns the specified reward prefab at the provided location.
    /// </summary>
    /// <param name="rewardPrefab">The reward prefab to instantiate.</param>
    /// <param name="location">The transform used as the spawn location.</param>
    public void SpawnReward(GameObject rewardPrefab, Transform location)
    {
        if (rewardPrefab != null)
        {
            Instantiate(rewardPrefab, location.transform.position, Quaternion.identity);
        }
    }

    /// <summary>
    /// Gets the gold cost associated with the specified reward type.
    /// </summary>
    /// <param name="rewardType">The reward type to price.</param>
    /// <returns>The gold cost for the reward type, or <see cref="int.MaxValue"/> if undefined.</returns>
    public int GetGoldCost(RewardType rewardType)
    {
        if (rewardCosts.TryGetValue(rewardType, out int cost))
        {
            return cost;
        }

        Debug.LogError($"No cost defined for reward type {rewardType}");
        return int.MaxValue;
    }

    /// <summary>
    /// Gets the gold cost associated with the reward type on the specified prefab.
    /// </summary>
    /// <param name="rewardPrefab">The reward prefab containing a <see cref="BoostableItem"/>.</param>
    /// <returns>The gold cost for the prefab's reward type, or <see cref="int.MaxValue"/> if unavailable.</returns>
    public int GetGoldCost(GameObject rewardPrefab)
    {
        if (rewardPrefab == null)
        {
            Debug.LogError("Reward prefab is null");
            return int.MaxValue;
        }

        BoostableItem boost = rewardPrefab.GetComponent<BoostableItem>();
        if (boost != null)
        {
            return GetGoldCost(boost.RewardType);
        }

        Debug.LogError($"Prefab {rewardPrefab.name} does not have a BoostableItem component");
        return int.MaxValue;
    }
}

/// <summary>
/// The available reward categories for room and shop rewards.
/// </summary>
public enum RewardType
{
    MaxHealth,
    Damage,
    Speed,
    HealthPotion,
    BeatTick
}
