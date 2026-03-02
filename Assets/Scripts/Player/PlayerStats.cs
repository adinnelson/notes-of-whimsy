using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float maxSpeed = 3.0f;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int maxDamage = 1;

    private float maxSpeedBonus = 0.0f;
    private int maxHealthBonus = 0;
    private int maxDamageBonus = 0;

    public float Speed => maxSpeed + maxSpeedBonus;
    public int MaxHealth => maxHealth + maxHealthBonus;
    public int Damage => maxDamage + maxDamageBonus;

    public event Action<int> OnMaxHealthIncreased;

    public void AddMovementSpeed(float amount)
    {
        maxSpeedBonus += amount;
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
    }
}
