using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100.0f;
    
    private float currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        // TODO: REMOVE LOG once integrated with UI and effects so we can see health changes
        Debug.Log($"{name} took {damageAmount} damage. HP now: {currentHealth}");

        if (currentHealth <= 0)
        {
            // TODO: REMOVE LOG once integrated with UI and effects so we can see health changes
            Debug.Log($"{name} died!");
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
