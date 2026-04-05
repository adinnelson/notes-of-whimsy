using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float maxSpeed = 1.0f;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int maxDamage = 1;

    //note that the maxSpeedBonus does NOT affect the acceleration -> only how fast the player is able to move as an upper limit
    private float maxSpeedBonus = 0.0f;
    private int maxHealthBonus = 0;
    private int maxDamageBonus = 0;
    private static PlayerStats _instance;

    private Health health;

    public static PlayerStats Instance
    {
        get
        {
            return _instance;
        }
    }
    public float Speed => maxSpeed + maxSpeedBonus;
    public int MaxHealth => maxHealth + maxHealthBonus;
    public int Damage => maxDamage + maxDamageBonus;
    public float BaseSpeed => maxSpeed; //added for PlayerMovement to scale the acceleration to the speed boostable item -> CURRENT: not using a scaling acceleration

    public event Action<int> OnMaxHealthIncreased;
    public event Action<float> OnSpeedChanged;
    public event Action<int> OnDamageChanged;
    public event Action OnStatsReset;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        health = GetComponent<Health>();
        if (health == null)
        {
            Debug.LogError($"{name}: No Health component found on player.");
        }

        ProgressFloor.OnEndSceneReached += ResetStats;
    }

    void OnDestroy()
    {
        ProgressFloor.OnEndSceneReached -= ResetStats;
    }

    public void AddMovementSpeed(float amount)
    {
        maxSpeedBonus += amount;
        OnSpeedChanged?.Invoke(Speed);
    }

    public void AddHealthBonus(int amount)
    {
        maxHealthBonus += amount;
        //Health script needs to update the current health as well: a +25maxHealth increase looks like this:
        //(currHealth)/(maxHealth) -> 50/75 +25healthBonus => 50+25/75+25 => 75/100
        OnMaxHealthIncreased?.Invoke(amount);
    }

    public void AddDamageBonus(int amount)
    {
        maxDamageBonus += amount;
        OnDamageChanged?.Invoke(Damage);
    }

    public void ResetStats()
    {
        maxSpeedBonus = 0.0f;
        maxHealthBonus = 0;
        maxDamageBonus = 0;
        health.CurrentHealth = MaxHealth;

        OnSpeedChanged?.Invoke(Speed);
        OnDamageChanged?. Invoke(Damage);
        OnStatsReset?.Invoke();
    }
}
