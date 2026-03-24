using UnityEngine;

public enum BoostType
{
    Speed,
    MaxHealth,
    Damage,
    HealthPotion,
    None
}
[RequireComponent(typeof(SpriteRenderer))]
public class BoostableItem : MonoBehaviour
{
    [SerializeField] private RewardType rewardType;
    [SerializeField] private BoostType boostType;
    [SerializeField] private float amount = 1.0f;

    public RewardType RewardType => rewardType;
    public BoostType Type => boostType;
    public float Amount => amount;

    //added this for testing and debugging -- if this interferes with the actual game running we can probably delete
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
