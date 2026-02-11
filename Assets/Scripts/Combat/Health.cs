using UnityEngine;
using System;

public class Health : MonoBehaviour, IDamageable
{
    public event Action OnDeath;

    [SerializeField] private float maxHealth = 100.0f;
    [SerializeField] private GameObject healthPrefab; // HealthDisplay prefab

    private float currentHealth;
    private HealthUI UI;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    void Awake()
    {
        currentHealth = maxHealth;
        SpawnHealthUI();
    }

    void SpawnHealthUI()
    {
        if (healthPrefab != null)
        {
            GameObject objectToDisplayHealth = Instantiate(healthPrefab, transform);
            objectToDisplayHealth.transform.localPosition = new Vector3(0, 1.0f, 0);

            UI = objectToDisplayHealth.GetComponent<HealthUI>();

            if (UI != null)
            {
                UI.Initialize(this);
            }
        }
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;

        // TODO: REMOVE LOG once integrated with UI and effects so we can see health changes
        Debug.Log($"{name} took {damageAmount} damage. HP now: {currentHealth}");

        if (UI != null )
        {
            UI.UpdateText();
        }

        if (currentHealth <= 0)
        {
            // TODO: REMOVE LOG once integrated with UI and effects so we can see health changes
            Debug.Log($"{name} died!");
            Die();
        }
    }

    void Die()
    {
        if (UI != null)
        {
            Destroy(UI.gameObject);
        }

        OnDeath?.Invoke();
    }
}
