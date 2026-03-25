using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class Health : MonoBehaviour, IDamageable
{
    public event Action OnDeath;

    [SerializeField] private float enemyMaxHealth = 100.0f;
    [SerializeField] private GameObject healthPrefab; // HealthDisplay prefab
    [SerializeField] private HealthBarUpdater healthBarUpdater;
    [SerializeField] private HealthText healthText; // assign this in the HealthDisplay prefab

    [Header("Hit Flash")]
    [SerializeField] private Material whiteMaterial;
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private float flashTime = 0.1f;

    private float currentHealth;
    private HealthUI UI;
    private bool isPlayer = false;
    private PlayerStats stats;

    private float hitFlashTimer;
    private bool inHitFlash = false;
    private SpriteRenderer sprite;

    public float MaxHealth => stats != null ? stats.MaxHealth : enemyMaxHealth;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.OnMaxHealthIncreased += OnMaxHealthIncreased;
        }


        currentHealth = MaxHealth;
        SpawnHealthUI();

        isPlayer = GetComponent<PlayerAttack>() != null;
    }

    void Start()
    {
        if (healthText != null)
        {
            healthText.UpdateHPText(currentHealth, MaxHealth);
        }
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

    private void FixedUpdate()
    {
        UpdateHitFlash();
    }

    private void UpdateHitFlash()
    {
        hitFlashTimer -= Time.fixedDeltaTime;

        if (hitFlashTimer <= 0 && inHitFlash)
        {
            //sprite.material = defaultMaterial;
            inHitFlash = false;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;

        hitFlashTimer = flashTime;
        //sprite.material = whiteMaterial;
        inHitFlash = true;

        if (healthBarUpdater != null)
        {
            healthBarUpdater.UpdateHealthBar(currentHealth, MaxHealth);
        }

        if (healthText != null)
        {
            healthText.UpdateHPText(currentHealth, MaxHealth);
        }

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
        if (UI != null && !isPlayer)
        {
            Destroy(UI.gameObject);
        }

        OnDeath?.Invoke();

        if(isPlayer)
        {
            GoldManager.Instance.ResetGold();
            PlayerStats.Instance.ResetStats();
            GetComponent<PlayerActiveSpellsHandler>()?.RemoveAllSpells();

            // TODO: load death scene
            SceneManager.LoadScene("Main", LoadSceneMode.Single);

            currentHealth = MaxHealth;
            UI.UpdateText();
        }
    }

    private void OnDestroy()
    {
        if (stats != null)
        {
            stats.OnMaxHealthIncreased -= OnMaxHealthIncreased;
        }
    }

    private void OnMaxHealthIncreased(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, MaxHealth);
        UI?.UpdateText();
        if (healthBarUpdater != null)
        {
            healthBarUpdater.UpdateHealthBar(currentHealth, MaxHealth);
        }
        if (healthText != null)
        {
            healthText.UpdateHPText(currentHealth, MaxHealth);
        }
    }

    public float CurrentHealth
    {
        get => currentHealth;
        set
        {
            currentHealth = Mathf.Clamp(value, 0.0f, MaxHealth);
            UI?.UpdateText();
            if (healthBarUpdater != null)
            {
                healthBarUpdater.UpdateHealthBar(currentHealth, MaxHealth);
            }

        }
    }
}
